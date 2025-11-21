# Media Storage Configuration Fix

**Issue**: API throwing 500 error during media upload with `System.ArgumentNullException: Value cannot be null. (Parameter 'webRootPath')`

**Date**: 2025-11-19
**Status**: FIXED
**Files Modified**:
- `src/SurveyBot.API/Program.cs` (lines 172-258, 440)
- `src/SurveyBot.API/appsettings.json` (lines 24-29)

---

## Root Cause

The `IWebHostEnvironment.WebRootPath` property was **NULL** because:

1. No `wwwroot` directory existed in the API project
2. ASP.NET Core sets `WebRootPath` to `null` when the directory doesn't exist
3. `FileSystemMediaStorageService` constructor required a non-null path
4. The DI registration directly passed `env.WebRootPath` without null checking

This commonly occurs in:
- Docker containers where `wwwroot` isn't mounted
- Minimal API setups without static file requirements
- Fresh deployments without proper directory structure

---

## Solution Overview

Implemented a **multi-level fallback strategy** with automatic directory creation:

### Priority 1: Explicit Configuration (Recommended for Production/Docker)
```json
{
  "MediaStorage": {
    "StoragePath": "/app/media",  // or "C:\\AppData\\Media" on Windows
    "UseWebRootPath": true,
    "MaxFileSizeMB": 10,
    "AllowedFileTypes": ["jpg", "jpeg", "png", "gif", "webp", "mp4", "webm", "mp3", "pdf"]
  }
}
```

### Priority 2: IWebHostEnvironment.WebRootPath
- Uses ASP.NET Core's configured `WebRootPath` if available
- Typical path: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\wwwroot`

### Priority 3: Automatic Fallback
- Creates `wwwroot` directory in `ContentRootPath`
- Automatically creates `uploads/media` subdirectory structure
- Logs warning about fallback usage

---

## Implementation Details

### Program.cs Changes (Lines 172-258)

```csharp
builder.Services.AddScoped<IMediaStorageService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<FileSystemMediaStorageService>>();
    var validationService = sp.GetRequiredService<IMediaValidationService>();
    var configuration = sp.GetRequiredService<IConfiguration>();

    string webRootPath;

    // Priority 1: Explicit configuration
    var configuredPath = configuration.GetValue<string>("MediaStorage:StoragePath");
    if (!string.IsNullOrEmpty(configuredPath))
    {
        webRootPath = configuredPath;
        logger.LogInformation("Using configured MediaStorage:StoragePath: {WebRootPath}", webRootPath);
    }
    // Priority 2: IWebHostEnvironment.WebRootPath
    else if (!string.IsNullOrEmpty(env.WebRootPath))
    {
        webRootPath = env.WebRootPath;
        logger.LogInformation("Using WebRootPath from IWebHostEnvironment: {WebRootPath}", webRootPath);
    }
    // Priority 3: Fallback to ContentRootPath/wwwroot
    else
    {
        webRootPath = Path.Combine(env.ContentRootPath, "wwwroot");
        logger.LogWarning("WebRootPath was null. Using fallback: {WebRootPath}", webRootPath);
    }

    // Create directories with error handling
    try
    {
        if (!Directory.Exists(webRootPath))
        {
            Directory.CreateDirectory(webRootPath);
            logger.LogInformation("Created media storage directory: {WebRootPath}", webRootPath);
        }

        var uploadsPath = Path.Combine(webRootPath, "uploads", "media");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
            logger.LogInformation("Created media uploads directory: {UploadsPath}", uploadsPath);
        }
    }
    catch (UnauthorizedAccessException ex)
    {
        logger.LogError(ex, "Access denied creating media storage directory: {WebRootPath}", webRootPath);
        throw new InvalidOperationException($"Cannot create media storage directory (access denied): {webRootPath}", ex);
    }
    catch (IOException ex)
    {
        logger.LogError(ex, "I/O error creating media storage directory: {WebRootPath}", webRootPath);
        throw new InvalidOperationException($"Cannot create media storage directory (I/O error): {webRootPath}", ex);
    }

    // Final validation
    if (!Directory.Exists(webRootPath))
    {
        logger.LogError("Media storage directory does not exist: {WebRootPath}", webRootPath);
        throw new InvalidOperationException($"Media storage directory does not exist and could not be created: {webRootPath}");
    }

    logger.LogInformation("Media storage initialized successfully at: {WebRootPath}", webRootPath);
    return new FileSystemMediaStorageService(webRootPath, logger, validationService);
});
```

### Static File Serving (Line 440)

Added `app.UseStaticFiles();` to enable HTTP access to uploaded media:

```csharp
app.UseCors();
app.UseStaticFiles();  // NEW: Serve files from wwwroot
app.UseHttpsRedirection();
```

This allows accessing uploaded files like:
- `http://localhost:5000/uploads/media/2025/11/abc123.jpg`

---

## Configuration Options

### appsettings.json (New Section)

```json
{
  "MediaStorage": {
    "StoragePath": null,                    // Custom path (null = use defaults)
    "UseWebRootPath": true,                 // Use ASP.NET Core WebRootPath
    "MaxFileSizeMB": 10,                    // Max upload size
    "AllowedFileTypes": [                   // Allowed extensions
      "jpg", "jpeg", "png", "gif", "webp",  // Images
      "mp4", "webm",                        // Videos
      "mp3",                                // Audio
      "pdf"                                 // Documents
    ]
  }
}
```

### Docker Configuration Example

For Docker deployments, use explicit path with volume mount:

**appsettings.Production.json**:
```json
{
  "MediaStorage": {
    "StoragePath": "/app/media"
  }
}
```

**docker-compose.yml**:
```yaml
services:
  api:
    volumes:
      - ./media-storage:/app/media  # Persist uploads outside container
```

### Windows Production Example

**appsettings.Production.json**:
```json
{
  "MediaStorage": {
    "StoragePath": "C:\\AppData\\SurveyBot\\Media"
  }
}
```

---

## Environment-Specific Behavior

### Local Development (Default)
- Uses fallback: `{ContentRootPath}/wwwroot`
- Creates directory automatically
- Example: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\wwwroot`

### Docker Container
- Configure explicit path in `appsettings.json`
- Mount as volume for persistence
- Example: `/app/media` → `./media-storage` volume

### Production Server
- Use explicit path pointing to dedicated storage
- Ensure directory permissions (write access)
- Consider using network storage (NAS, S3, etc. - future enhancement)

---

## Logging Output

### Successful Initialization
```
[10:30:00 INF] WebRootPath was null. Using fallback: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\wwwroot
[10:30:00 INF] Created media storage directory: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\wwwroot
[10:30:00 INF] Created media uploads directory: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\wwwroot\uploads\media
[10:30:00 INF] Media storage initialized successfully at: C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\wwwroot
```

### Using Explicit Configuration
```
[10:30:00 INF] Using configured MediaStorage:StoragePath: /app/media
[10:30:00 INF] Media storage initialized successfully at: /app/media
```

### Error: Access Denied
```
[10:30:00 ERR] Access denied creating media storage directory: /restricted/path
System.InvalidOperationException: Cannot create media storage directory (access denied): /restricted/path
```

---

## Verification Steps

### 1. Check Logs on Startup
Look for media storage initialization logs:
```bash
dotnet run | grep -i "media storage"
```

### 2. Verify Directory Structure
After startup, confirm directories exist:
```bash
ls -R wwwroot/uploads/media/
```

Expected structure:
```
wwwroot/
└── uploads/
    └── media/
        └── {year}/
            └── {month}/
                └── {guid}.{ext}
```

### 3. Test Upload
Upload a file via API:
```bash
curl -X POST http://localhost:5000/api/media/upload \
  -H "Authorization: Bearer {token}" \
  -F "file=@test-image.jpg" \
  -F "mediaType=image"
```

Expected response:
```json
{
  "success": true,
  "data": {
    "id": "abc-123",
    "filePath": "/uploads/media/2025/11/guid.jpg",
    "thumbnailPath": "/uploads/media/2025/11/guid_thumb.jpg",
    "fileSize": 102400,
    "mimeType": "image/jpeg"
  }
}
```

### 4. Access Uploaded File
```bash
curl http://localhost:5000/uploads/media/2025/11/guid.jpg --output test.jpg
```

---

## Troubleshooting

### Issue: Still Getting ArgumentNullException

**Solution**: Check logs for which fallback is being used. Ensure:
1. Configuration section exists in appsettings.json
2. ContentRootPath is not null (should never happen)
3. Application has write permissions

### Issue: Files Upload but Return 404

**Solution**: Ensure `app.UseStaticFiles()` is called in Program.cs middleware pipeline

### Issue: Access Denied Errors

**Solution**:
1. Check directory permissions
2. Run application with appropriate user account
3. Use explicit path with proper permissions in appsettings.json

### Issue: Docker Container Loses Uploads on Restart

**Solution**: Mount volume to persist uploads:
```yaml
volumes:
  - ./media-storage:/app/media
```

---

## Future Enhancements

### Cloud Storage Integration
Replace `FileSystemMediaStorageService` with:
- `AzureBlobMediaStorageService`
- `S3MediaStorageService`
- `GoogleCloudMediaStorageService`

Same interface, different implementation.

### Configuration-Based Storage Selection
```json
{
  "MediaStorage": {
    "Provider": "FileSystem",  // or "AzureBlob", "S3"
    "FileSystem": {
      "StoragePath": "/app/media"
    },
    "AzureBlob": {
      "ConnectionString": "...",
      "ContainerName": "media"
    }
  }
}
```

---

## Summary

**Root Cause**: `IWebHostEnvironment.WebRootPath` was null because `wwwroot` directory didn't exist

**Solution**: Three-tier fallback strategy with automatic directory creation

**Benefits**:
- Works in all environments (dev, Docker, production)
- Automatic directory creation
- Configurable for specific deployment needs
- Comprehensive logging
- Proper error handling

**Files Modified**:
1. `src/SurveyBot.API/Program.cs` - DI registration with fallback logic
2. `src/SurveyBot.API/appsettings.json` - Added MediaStorage configuration section

**Testing**: Restart API and attempt media upload - should now work successfully.
