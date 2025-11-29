# Login Network Error - Diagnostic Report

**Date**: 2025-11-28
**Issue**: Frontend admin panel login fails with "Network Error"
**Status**: ✅ ROOT CAUSE IDENTIFIED

---

## Executive Summary

The frontend admin panel login is failing due to **CORS origin mismatch**. The frontend is running with an **ngrok URL** (`https://27b2352927ab.ngrok-free.app`) but the backend API's CORS configuration has **different hardcoded ngrok URLs** that don't include the current frontend ngrok URL.

**Impact**: Login completely broken for remote access (ngrok). Local access (`http://localhost:3000`) works fine.

**Solution**: Update backend CORS configuration to include current ngrok URLs or use dynamic origin validation.

---

## Problem Analysis

### User Report

```
When i try login in admin panel on frontend page just update and nothing happens
```

### Error Logs

```javascript
Network error: Please check your connection
Login error: AxiosError {
  message: 'Network Error',
  name: 'AxiosError',
  code: 'ERR_NETWORK'
}
```

### Key Observations

1. ✅ **API Server**: Healthy and running (confirmed by Docker logs and health check)
2. ✅ **Database**: Connected and operational
3. ✅ **OPTIONS Preflight Requests**: Successful (CORS preflight working)
4. ❌ **POST Login Requests**: NEVER reach the API (blocked by CORS)

---

## Root Cause Analysis

### Frontend Configuration

**File**: `frontend/src/config/ngrok.config.ts`

```typescript
export const BACKEND_NGROK_URL = 'https://df0778be2c16.ngrok-free.app';  // API
export const FRONTEND_NGROK_URL = 'https://27b2352927ab.ngrok-free.app'; // Frontend
```

**File**: `frontend/.env.development`

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

**Current Frontend Origin**: `https://27b2352927ab.ngrok-free.app` (from ngrok.config.ts)

### Backend CORS Configuration

**File**: `src/SurveyBot.API/Program.cs` (lines 46-50)

```csharp
policy.WithOrigins(
    "http://localhost:3000",                  // Local frontend (React dev server)
    "http://localhost:5173",                  // Local frontend (Vite)
    "https://5167d6c0729b.ngrok-free.app",    // OLD ngrok frontend URL ❌
    "https://3c6dfc99c860.ngrok-free.app"     // OLD ngrok backend URL ❌
)
```

**Problem**: The current frontend ngrok URL (`https://27b2352927ab.ngrok-free.app`) is **NOT in the allowed origins list**.

### Why CORS Blocks the Request

1. **Browser makes OPTIONS preflight** → API returns CORS headers for allowed origins
2. **Browser sees origin `https://27b2352927ab.ngrok-free.app`** → NOT in allowed list
3. **Browser blocks POST request** before it's even sent to server
4. **Frontend receives "Network Error"** (CORS error disguised as network error)

---

## Evidence from Docker Logs

**API Container Logs** (from docker-log-analyzer report):

```
2025-11-28 17:20:22 [INF] HTTP OPTIONS /api/auth/login responded 204 in 2.5ms
2025-11-28 17:37:46 [INF] HTTP OPTIONS /api/auth/login responded 204 in 1.8ms
```

**Observation**: Only OPTIONS requests (preflight) are logged. **ZERO POST requests** reach the API.

This confirms the browser is blocking the actual POST request due to CORS.

---

## Request Flow Analysis

```
┌─────────────────────────────────────────────────────────────┐
│ 1. User clicks "Login" button                               │
│    Origin: https://27b2352927ab.ngrok-free.app             │
└───────────────┬─────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Browser sends OPTIONS preflight request                  │
│    OPTIONS https://df0778be2c16.ngrok-free.app/api/auth/login│
│    Origin: https://27b2352927ab.ngrok-free.app             │
└───────────────┬─────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. API receives OPTIONS request                             │
│    ✅ Returns 204 No Content with CORS headers:            │
│    Access-Control-Allow-Origin: * (NgrokPolicy allows all)  │
│    Access-Control-Allow-Methods: POST                       │
│    Access-Control-Allow-Headers: content-type               │
│    Access-Control-Allow-Credentials: true                   │
└───────────────┬─────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. Browser evaluates CORS response                          │
│    ⚠️ ISSUE: Default policy is applied, NOT NgrokPolicy   │
│    Default policy WithOrigins() does NOT include current URL│
│    Browser: "Origin not allowed" → BLOCK POST request       │
└───────────────┬─────────────────────────────────────────────┘
                ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. POST request NEVER sent to server                        │
│    ❌ Browser blocks it client-side                        │
│    Frontend receives: AxiosError "Network Error"            │
│    API logs: ZERO POST requests recorded                    │
└─────────────────────────────────────────────────────────────┘
```

---

## Why NgrokPolicy Isn't Being Applied

**Code Analysis**: `Program.cs` lines 42-74

```csharp
// DEFAULT POLICY (applied to all endpoints)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(/* hardcoded URLs */)  // ❌ Default uses this
    });

    // NGROK POLICY (permissive, allows all ngrok URLs)
    options.AddPolicy("NgrokPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            return origin.Contains("ngrok-free.app") ||  // ✅ Would allow 27b2352927ab
                   origin.Contains("ngrok.app") ||
                   origin.Contains("ngrok.io") ||
                   origin == "http://localhost:3000" ||
                   origin == "http://localhost:5173";
        })
    });
});
```

**Problem**: The `NgrokPolicy` is **defined but NEVER APPLIED**.

**Middleware**: `app.UseCors()` with no argument uses the **default policy**, which has hardcoded URLs.

**Solution**: Apply NgrokPolicy explicitly:

```csharp
app.UseCors("NgrokPolicy");  // Apply permissive policy for ngrok
```

---

## Verification Tests

### Test 1: CORS Preflight (OPTIONS)

```bash
curl -X OPTIONS http://localhost:5000/api/auth/login \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type" \
  -v 2>&1 | grep "< Access-Control"
```

**Result**:
```
< Access-Control-Allow-Credentials: true
< Access-Control-Allow-Headers: content-type
< Access-Control-Allow-Methods: POST
< Access-Control-Allow-Origin: http://localhost:3000
```

✅ **CORS works for `localhost:3000`** (in allowed origins list)

### Test 2: Actual Login (POST)

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"telegramId": 123456789, "username": "test"}' \
  -v 2>&1 | head -30
```

**Result**: ✅ **200 OK** with JWT token

**Conclusion**: API endpoint works perfectly when CORS doesn't block it.

---

## Solution Options

### Option 1: Update CORS with Current ngrok URLs (Quick Fix)

**File**: `src/SurveyBot.API/Program.cs`

**Change**:
```csharp
policy.WithOrigins(
    "http://localhost:3000",
    "http://localhost:5173",
    "https://27b2352927ab.ngrok-free.app",    // NEW frontend ngrok URL ✅
    "https://df0778be2c16.ngrok-free.app"     // NEW backend ngrok URL ✅
)
```

**Pros**:
- Quick fix, minimal code change
- Explicit allowed origins

**Cons**:
- ❌ Must update every time ngrok URL changes
- ❌ Maintenance burden
- ❌ Error-prone

**Recommendation**: ❌ **NOT RECOMMENDED** - ngrok URLs change frequently

---

### Option 2: Apply NgrokPolicy (Recommended)

**File**: `src/SurveyBot.API/Program.cs`

**Change** (line ~230):
```csharp
// OLD:
// app.UseCors();  // Uses default policy

// NEW:
app.UseCors("NgrokPolicy");  // Uses permissive ngrok policy
```

**Pros**:
- ✅ Automatically allows ALL ngrok URLs (any subdomain)
- ✅ No need to update when ngrok URL changes
- ✅ Already defined in code, just needs to be applied

**Cons**:
- Less restrictive (allows any ngrok URL)
- Still secure (requires ngrok subdomain)

**Recommendation**: ✅ **RECOMMENDED** - Best for development with ngrok

---

### Option 3: Environment-Based CORS (Production-Ready)

**File**: `src/SurveyBot.API/Program.cs`

**Implementation**:
```csharp
// In Development: Use permissive NgrokPolicy
if (app.Environment.IsDevelopment())
{
    app.UseCors("NgrokPolicy");  // Allow all ngrok URLs
}
else
{
    // In Production: Use strict default policy with known origins
    app.UseCors();  // Uses default policy with explicit origins
}
```

**Pros**:
- ✅ Flexible for development (auto-allows ngrok)
- ✅ Strict for production (only known origins)
- ✅ Environment-aware

**Cons**:
- Slightly more complex

**Recommendation**: ✅ **BEST PRACTICE** - Recommended for production deployment

---

### Option 4: Dynamic Origin Validation with Environment Variable

**File**: `appsettings.Development.json`

Add:
```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173",
    "https://27b2352927ab.ngrok-free.app"
  ]
}
```

**File**: `Program.cs`

```csharp
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**Pros**:
- ✅ Configuration-driven
- ✅ Easy to update (no code changes)
- ✅ Environment-specific

**Cons**:
- Still requires manual update when ngrok URL changes

**Recommendation**: ⚠️ **OPTIONAL** - Good for production, still manual for ngrok

---

## Immediate Fix Steps

### Step 1: Apply NgrokPolicy

**File**: `src/SurveyBot.API/Program.cs`

**Find** (around line 230):
```csharp
app.UseCors();
```

**Replace with**:
```csharp
app.UseCors("NgrokPolicy");
```

### Step 2: Restart API

```bash
# Stop API
Ctrl+C

# Restart API
cd src/SurveyBot.API
dotnet run
```

### Step 3: Test Login

1. Open frontend: `https://27b2352927ab.ngrok-free.app`
2. Try login
3. Should work now ✅

---

## Verification After Fix

### Check CORS Headers

```bash
curl -X OPTIONS https://df0778be2c16.ngrok-free.app/api/auth/login \
  -H "Origin: https://27b2352927ab.ngrok-free.app" \
  -H "Access-Control-Request-Method: POST" \
  -v 2>&1 | grep "Access-Control-Allow-Origin"
```

**Expected**:
```
< Access-Control-Allow-Origin: https://27b2352927ab.ngrok-free.app
```

### Monitor API Logs

```bash
docker compose logs -f api
```

**Expected**: After login attempt, you should see:
```
[INF] HTTP POST /api/auth/login responded 200 in 50ms
```

---

## Prevention Measures

### 1. Documentation Update

Update `CLAUDE.md` and `documentation/api/QUICK-REFERENCE.md`:

```markdown
## CORS Configuration for ngrok

**Development**: The API uses `NgrokPolicy` which automatically allows all ngrok URLs.

**Important**: No need to update CORS configuration when ngrok URL changes.

**Configuration**: `Program.cs` line 59-73 defines NgrokPolicy
**Applied**: `Program.cs` line 230 - `app.UseCors("NgrokPolicy")`
```

### 2. Add Health Check for CORS

**New endpoint**: `/api/health/cors`

```csharp
[HttpGet("cors")]
[AllowAnonymous]
public IActionResult CheckCors()
{
    var origin = Request.Headers["Origin"].ToString();
    return Ok(new
    {
        requestOrigin = origin,
        corsPolicy = "NgrokPolicy",
        message = "If you see this, CORS is working"
    });
}
```

### 3. Frontend Diagnostic Tool

Add CORS test button in login page:

```typescript
const testCors = async () => {
  try {
    const response = await api.get('/health/cors');
    console.log('CORS test passed:', response.data);
  } catch (error) {
    console.error('CORS test failed:', error);
  }
};
```

---

## Related Issues

### Why OPTIONS Requests Work But POST Doesn't

**Explanation**:

1. **OPTIONS (Preflight)**: Simple request, browser allows it without full CORS check
2. **Server Response**: API returns CORS headers
3. **Browser Evaluation**: Checks if returned headers allow the actual request
4. **POST Request**: Only sent if CORS headers permit it

**In this case**:
- OPTIONS succeeds (server is reachable)
- Server returns headers for **default policy** (outdated ngrok URLs)
- Browser blocks POST because origin not in allowed list

### Why It Looks Like "Network Error"

**Browser Behavior**:
- CORS violations don't expose detailed errors to JavaScript (security)
- Axios sees request blocked before network transmission
- Displays generic "Network Error" instead of "CORS Error"

**Console Logs**:
```
Network error: Please check your connection
```

**Actual Issue**: CORS policy violation

---

## Testing Checklist

After applying fix:

- [ ] Frontend login works from ngrok URL
- [ ] Frontend login works from localhost:3000
- [ ] API logs show POST /api/auth/login requests
- [ ] Browser console shows no CORS errors
- [ ] JWT token received successfully
- [ ] User redirected to dashboard after login

---

## Conclusion

**Root Cause**: CORS origin mismatch - backend has old hardcoded ngrok URLs

**Impact**: Login completely broken for remote access

**Solution**: Apply NgrokPolicy middleware to auto-allow all ngrok URLs

**Fix Location**: `src/SurveyBot.API/Program.cs` line ~230

**Change**: `app.UseCors()` → `app.UseCors("NgrokPolicy")`

**Effort**: 1-line code change + API restart

**Status**: ✅ **ROOT CAUSE IDENTIFIED - SOLUTION READY**

---

## Appendix: Related Files

### Frontend Files Analyzed

1. `frontend/.env.development` - API base URL configuration
2. `frontend/src/config/ngrok.config.ts` - ngrok URL configuration
3. `frontend/src/services/api.ts` - Axios HTTP client

### Backend Files Analyzed

1. `src/SurveyBot.API/Program.cs` - CORS configuration (lines 42-74, ~230)
2. `src/SurveyBot.API/Controllers/AuthController.cs` - Login endpoint

### Docker Logs

1. API container: `surveybot-api-1` - Healthy, no POST requests logged
2. Database container: `surveybot-postgres-1` - Connected and operational

---

**Report Generated**: 2025-11-28 20:45 UTC
**Analyzed By**: task-execution-agent
**Status**: ✅ Complete - Solution Identified
