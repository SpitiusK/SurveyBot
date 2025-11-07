# Troubleshooting Guide
## Telegram Survey Bot MVP

This guide helps you diagnose and fix common issues you might encounter during development.

---

## Table of Contents

1. [Quick Diagnostics](#quick-diagnostics)
2. [Database Issues](#database-issues)
3. [Build and Compilation Issues](#build-and-compilation-issues)
4. [Runtime Issues](#runtime-issues)
5. [Docker Issues](#docker-issues)
6. [Entity Framework Issues](#entity-framework-issues)
7. [API Issues](#api-issues)
8. [Telegram Bot Issues](#telegram-bot-issues)
9. [Performance Issues](#performance-issues)
10. [Development Environment Issues](#development-environment-issues)

---

## Quick Diagnostics

### First Steps for Any Issue

1. **Check application is running**
   ```bash
   curl http://localhost:5000/api/health
   ```
   Should return: `{"status":"Healthy","database":"Connected"}`

2. **Check Docker containers**
   ```bash
   docker-compose ps
   ```
   Both postgres and pgadmin should show "Up"

3. **Check logs**
   ```bash
   # API logs (if running)
   # Check console output where app is running

   # Docker logs
   docker-compose logs -f postgres
   ```

4. **Verify database connection**
   - Open pgAdmin: http://localhost:5050
   - Login: admin@surveybot.local / admin123
   - Try connecting to postgres server

---

## Database Issues

### Issue: "Cannot connect to database"

**Symptoms**:
- Error: "Connection refused" or "Timeout expired"
- Health check returns "Unhealthy"
- API fails to start

**Solutions**:

1. **Verify PostgreSQL is running**
   ```bash
   docker-compose ps
   # Look for surveybot-postgres container
   ```

2. **Start PostgreSQL if stopped**
   ```bash
   docker-compose up -d postgres
   ```

3. **Check connection string**
   - File: `src/SurveyBot.API/appsettings.json`
   - Should be: `Host=localhost;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password`

4. **Verify PostgreSQL is accepting connections**
   ```bash
   docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db
   # If successful, type \q to quit
   ```

5. **Check firewall** (if using local PostgreSQL)
   - Port 5432 must be open
   - Windows: Check Windows Defender Firewall
   - Mac/Linux: Check iptables/firewalld

**Still not working?**
```bash
# Nuclear option: Reset everything
docker-compose down -v
docker-compose up -d
cd src/SurveyBot.API
dotnet ef database update
```

---

### Issue: "Database does not exist"

**Symptoms**:
- Error: "database 'surveybot_db' does not exist"
- Migrations fail

**Solutions**:

1. **Create database manually**
   ```bash
   docker exec -it surveybot-postgres psql -U postgres
   CREATE DATABASE surveybot_db;
   GRANT ALL PRIVILEGES ON DATABASE surveybot_db TO surveybot_user;
   \q
   ```

2. **Apply migrations**
   ```bash
   cd src/SurveyBot.API
   dotnet ef database update
   ```

3. **Verify database exists**
   ```bash
   docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db -c "\dt"
   ```

---

### Issue: "Password authentication failed"

**Symptoms**:
- Error: "password authentication failed for user"
- Cannot connect via pgAdmin

**Solutions**:

1. **Check credentials in docker-compose.yml**
   ```yaml
   POSTGRES_USER: surveybot_user
   POSTGRES_PASSWORD: surveybot_dev_password
   POSTGRES_DB: surveybot_db
   ```

2. **Check credentials in appsettings.json**
   Must match docker-compose.yml

3. **Reset password**
   ```bash
   docker-compose down -v
   docker-compose up -d
   # This creates fresh database with correct password
   ```

---

### Issue: "Tables not found"

**Symptoms**:
- Error: "relation 'users' does not exist"
- Database exists but empty

**Solutions**:

1. **Apply migrations**
   ```bash
   cd src/SurveyBot.API
   dotnet ef database update
   ```

2. **Check migrations exist**
   ```bash
   cd src/SurveyBot.Infrastructure
   ls Migrations/
   # Should see migration files
   ```

3. **Create initial migration if missing**
   ```bash
   cd src/SurveyBot.API
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Verify tables created**
   ```bash
   docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db -c "\dt"
   ```

---

## Build and Compilation Issues

### Issue: "Build failed" or compilation errors

**Symptoms**:
- Error: "The type or namespace name could not be found"
- Red squiggly lines in IDE

**Solutions**:

1. **Restore NuGet packages**
   ```bash
   dotnet restore
   dotnet clean
   dotnet build
   ```

2. **Check .NET version**
   ```bash
   dotnet --version
   # Must be 8.0.x or higher
   ```

3. **Clear NuGet cache**
   ```bash
   dotnet nuget locals all --clear
   dotnet restore
   ```

4. **Check for missing references**
   ```bash
   # From project directory
   dotnet list reference
   ```

5. **Rebuild solution**
   ```bash
   dotnet clean
   dotnet build --no-incremental
   ```

---

### Issue: "Project file is corrupt"

**Symptoms**:
- Error: "The project file is invalid"
- Cannot load project in IDE

**Solutions**:

1. **Check .csproj file syntax**
   - Look for unclosed tags
   - Verify XML is well-formed

2. **Compare with working project**
   - Copy structure from another working .csproj

3. **Restore from git**
   ```bash
   git checkout -- src/ProjectName/ProjectName.csproj
   ```

---

## Runtime Issues

### Issue: "Application crashes on startup"

**Symptoms**:
- App starts then immediately exits
- Error in logs

**Solutions**:

1. **Check logs for specific error**
   ```bash
   cd src/SurveyBot.API
   dotnet run
   # Read the error message carefully
   ```

2. **Common startup errors**:

   **"Missing appsettings.json"**
   ```bash
   # Verify file exists
   ls src/SurveyBot.API/appsettings.json
   ```

   **"Invalid connection string"**
   - Check appsettings.json format
   - Verify all required fields present

   **"Port already in use"**
   ```bash
   # Find process using port 5000
   # Windows:
   netstat -ano | findstr :5000
   # Mac/Linux:
   lsof -i :5000

   # Kill the process or change port in appsettings.json
   ```

3. **Run with detailed logging**
   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --verbosity detailed
   ```

---

### Issue: "NullReferenceException"

**Symptoms**:
- Error: "Object reference not set to an instance of an object"
- API returns 500 error

**Solutions**:

1. **Enable detailed error pages**
   - Already enabled in Development environment
   - Check console logs for stack trace

2. **Check dependency injection**
   - Verify service is registered in Program.cs
   - Check constructor parameters

3. **Add null checks**
   ```csharp
   if (myObject == null)
   {
       _logger.LogError("myObject is null");
       throw new ArgumentNullException(nameof(myObject));
   }
   ```

4. **Use debugger**
   - Set breakpoint before error
   - Inspect variable values
   - Step through code

---

## Docker Issues

### Issue: "Docker daemon is not running"

**Symptoms**:
- Error: "Cannot connect to Docker daemon"
- docker-compose commands fail

**Solutions**:

1. **Start Docker Desktop**
   - Windows: Check system tray for Docker icon
   - Mac: Check menu bar for Docker icon
   - Linux: `sudo systemctl start docker`

2. **Verify Docker is running**
   ```bash
   docker ps
   # Should show running containers or empty list
   ```

3. **Restart Docker Desktop**
   - Quit and restart the application

---

### Issue: "Port already in use"

**Symptoms**:
- Error: "bind: address already in use"
- docker-compose up fails

**Solutions**:

1. **Find what's using the port**
   ```bash
   # Port 5432 (PostgreSQL)
   # Windows:
   netstat -ano | findstr :5432
   # Mac/Linux:
   lsof -i :5432
   ```

2. **Stop conflicting container**
   ```bash
   docker ps
   docker stop container_name
   ```

3. **Change port in docker-compose.yml**
   ```yaml
   ports:
     - "5433:5432"  # Map to different host port
   ```

4. **Update connection string** if you changed port

---

### Issue: "Volume mount issues"

**Symptoms**:
- Error: "Error response from daemon: invalid mount config"
- Data not persisting

**Solutions**:

1. **Check volume exists**
   ```bash
   docker volume ls
   ```

2. **Remove and recreate volumes**
   ```bash
   docker-compose down -v
   docker-compose up -d
   ```

3. **Check Docker Desktop settings**
   - Windows: Check file sharing in Docker Desktop settings
   - Mac: Check file sharing in Docker Desktop settings

---

## Entity Framework Issues

### Issue: "No migrations found"

**Symptoms**:
- Error: "No migrations were applied"
- Database empty after update

**Solutions**:

1. **Check migrations folder**
   ```bash
   ls src/SurveyBot.Infrastructure/Migrations/
   ```

2. **Create initial migration**
   ```bash
   cd src/SurveyBot.API
   dotnet ef migrations add InitialCreate
   ```

3. **Verify DbContext is registered**
   - Check Program.cs
   - Should have: `builder.Services.AddDbContext<ApplicationDbContext>`

---

### Issue: "Migration already applied"

**Symptoms**:
- Error: "The migration has already been applied"
- Cannot apply migration

**Solutions**:

1. **Check migration history**
   ```bash
   cd src/SurveyBot.API
   dotnet ef migrations list
   ```

2. **Remove last migration** (if not applied to other environments)
   ```bash
   dotnet ef migrations remove
   ```

3. **Revert to specific migration**
   ```bash
   dotnet ef database update PreviousMigrationName
   ```

---

### Issue: "Pending model changes"

**Symptoms**:
- Warning: "Your model has changes not reflected in a migration"
- Error on database update

**Solutions**:

1. **Create new migration**
   ```bash
   cd src/SurveyBot.API
   dotnet ef migrations add DescriptiveNameForChanges
   ```

2. **Apply migration**
   ```bash
   dotnet ef database update
   ```

---

### Issue: "EF Core tools not installed"

**Symptoms**:
- Error: "Could not execute because the specified command or file was not found"
- dotnet ef commands not working

**Solutions**:

1. **Install EF Core tools globally**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Update EF Core tools**
   ```bash
   dotnet tool update --global dotnet-ef
   ```

3. **Verify installation**
   ```bash
   dotnet ef --version
   ```

---

## API Issues

### Issue: "404 Not Found on all endpoints"

**Symptoms**:
- All API calls return 404
- Even health check fails

**Solutions**:

1. **Check base URL**
   - Should be: http://localhost:5000/api/...
   - Not: http://localhost:5000/...

2. **Verify routing**
   - Controllers should have `[Route("api/[controller]")]`

3. **Check MapControllers is called**
   ```csharp
   // In Program.cs
   app.MapControllers();
   ```

---

### Issue: "CORS errors"

**Symptoms**:
- Error: "No 'Access-Control-Allow-Origin' header"
- Works in Swagger but fails from browser

**Solutions**:

1. **Add CORS policy** (when implementing admin panel)
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowAll", builder =>
       {
           builder.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
       });
   });

   app.UseCors("AllowAll");
   ```

2. **Note**: CORS not needed for MVP as no frontend yet

---

### Issue: "Swagger not loading"

**Symptoms**:
- http://localhost:5000/swagger shows blank page
- 404 on /swagger

**Solutions**:

1. **Check Swagger is configured**
   ```csharp
   // In Program.cs
   app.UseSwagger();
   app.UseSwaggerUI();
   ```

2. **Check environment**
   - Swagger only enabled in Development by default

3. **Check for JavaScript errors**
   - Open browser console (F12)
   - Look for errors

---

## Telegram Bot Issues

### Issue: "Bot not responding"

**Symptoms**:
- Send message to bot, no response
- Webhook not receiving updates

**Solutions**:

1. **Verify bot token is correct**
   - Check .env or appsettings.json
   - Token should be format: 123456789:ABC...

2. **Check webhook is set**
   ```bash
   curl https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo
   ```

3. **Check logs**
   - Look for webhook POST requests
   - Check for errors in message processing

**Note**: Full bot implementation is pending in current MVP state.

---

## Performance Issues

### Issue: "Slow database queries"

**Symptoms**:
- API responses take > 1 second
- Database queries timeout

**Solutions**:

1. **Enable EF Core query logging**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.EntityFrameworkCore.Database.Command": "Information"
       }
     }
   }
   ```

2. **Check indexes**
   - Review database/INDEX_OPTIMIZATION.md
   - Verify indexes exist: `\di` in psql

3. **Use async operations**
   - Always use `await` with async methods
   - Don't mix sync and async code

4. **Add pagination**
   - Limit results with Take/Skip
   - Don't load all records at once

---

### Issue: "Memory leaks"

**Symptoms**:
- Memory usage grows over time
- Application slows down

**Solutions**:

1. **Dispose DbContext properly**
   - Use dependency injection (automatic disposal)
   - Don't create DbContext manually

2. **Check for circular references**
   - Avoid circular navigation properties

3. **Use memory profiler**
   - dotMemory (JetBrains)
   - Visual Studio Diagnostic Tools

---

## Development Environment Issues

### Issue: "IDE not recognizing files"

**Symptoms**:
- Red squiggles everywhere
- IntelliSense not working

**Solutions**:

**Visual Studio**:
1. Close and reopen solution
2. Clean solution: Build > Clean Solution
3. Rebuild: Build > Rebuild Solution
4. Delete .vs folder and restart

**VS Code**:
1. Reload window: Ctrl+Shift+P > "Reload Window"
2. Reinstall C# extension
3. Delete .vscode folder and restart

---

### Issue: "Git conflicts"

**Symptoms**:
- Merge conflicts after pull
- Cannot commit changes

**Solutions**:

1. **View conflicts**
   ```bash
   git status
   ```

2. **Resolve manually**
   - Open conflicted files
   - Look for <<<<<<< markers
   - Choose correct version
   - Remove conflict markers

3. **Use merge tool**
   ```bash
   git mergetool
   ```

4. **After resolving**
   ```bash
   git add .
   git commit -m "Resolve merge conflicts"
   ```

---

## Still Having Issues?

### Getting More Help

1. **Check existing documentation**
   - README.md
   - Architecture documentation
   - Database documentation

2. **Enable detailed logging**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft": "Debug",
         "Microsoft.EntityFrameworkCore": "Information"
       }
     }
   }
   ```

3. **Search error messages**
   - Copy exact error message
   - Search on Google/Stack Overflow
   - Check .NET documentation

4. **Check application logs**
   - Console output
   - Log files (if configured)

5. **Use debugger**
   - Set breakpoints
   - Inspect variables
   - Step through code

### Reporting Bugs

When reporting an issue, include:

1. **What you were trying to do**
2. **What happened instead**
3. **Complete error message** (with stack trace)
4. **Steps to reproduce**
5. **Environment details**:
   - OS version
   - .NET version
   - Docker version
   - Branch/commit hash

### Emergency Reset

If everything is broken and you need to start fresh:

```bash
# 1. Stop everything
docker-compose down -v

# 2. Clean .NET build artifacts
dotnet clean
rm -rf */bin */obj  # or manually delete bin/obj folders

# 3. Reset git (if needed)
git stash  # Save your changes
git pull origin main

# 4. Start fresh
dotnet restore
dotnet build
docker-compose up -d
cd src/SurveyBot.API
dotnet ef database update
dotnet run
```

---

## Quick Reference Commands

### Database
```bash
# Start database
docker-compose up -d postgres

# Stop database
docker-compose down

# Reset database
docker-compose down -v
docker-compose up -d

# Connect to database
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db

# Apply migrations
cd src/SurveyBot.API && dotnet ef database update

# Create migration
cd src/SurveyBot.API && dotnet ef migrations add MigrationName
```

### Application
```bash
# Build
dotnet build

# Run
cd src/SurveyBot.API && dotnet run

# Test
dotnet test

# Clean
dotnet clean
```

### Docker
```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f

# Check status
docker-compose ps
```

---

**Document Status**: Complete
**Last Updated**: 2025-11-06

Remember: Most issues can be resolved by carefully reading error messages and checking logs. Take your time and debug systematically!
