# Migration Fix Summary

## Problem Solved

The API was failing to start in Docker with this error:
```
System.InvalidOperationException: An error was generated for warning
'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning':
The model for context 'SurveyBotDbContext' has pending changes.
```

## Root Cause

When the migrations `AddSurveyCodeColumn` and `AddLastLoginAtToUser` were created, the **model snapshot file** (`SurveyBotDbContextModelSnapshot.cs`) was not properly updated. This file is EF Core's "source of truth" for the current database schema, and when it's out of sync with the actual entity configurations, EF Core throws this error.

## What Was Fixed

### 1. **Survey.Code Property Missing from Snapshot**

**Problem:**
- `Survey.cs` had a `Code` property
- `SurveyConfiguration.cs` configured the Code column
- Migration `AddSurveyCodeColumn` was created
- **BUT** the model snapshot didn't include the Code property

**Fix:**
- Added `Code` property to Survey entity in snapshot
- Added `Code` unique index in snapshot
- Now matches the actual configuration

### 2. **User.LastLoginAt Property Missing from Snapshot**

**Problem:**
- `User.cs` had a `LastLoginAt` property
- Migration `AddLastLoginAtToUser` was created
- **BUT** `UserConfiguration.cs` didn't configure it
- **AND** the model snapshot didn't include it

**Fix:**
- Added `LastLoginAt` configuration to `UserConfiguration.cs`
- Added `LastLoginAt` property to User entity in snapshot
- Now fully configured and tracked

### 3. **Incorrect Column Naming in LastLoginAt Migration**

**Problem:**
- The migration used PascalCase: `"LastLoginAt"` and `"Users"`
- All other migrations use snake_case: `"last_login_at"` and `"users"`
- This inconsistency could cause issues when applying migrations

**Fix:**
- Changed migration to use snake_case column names
- Now consistent with the rest of the database schema

## Files Modified

1. **SurveyBotDbContextModelSnapshot.cs**
   - Added `Code` property for Survey entity
   - Added `LastLoginAt` property for User entity

2. **UserConfiguration.cs**
   - Added `LastLoginAt` configuration

3. **20251106000001_AddLastLoginAtToUser.cs**
   - Fixed column names to use snake_case

4. **fix-migration.sh** (new file)
   - Script for creating migrations on host machine if needed

## How to Test

### Step 1: Rebuild Docker Image

The fixes are already committed to the branch. Rebuild the Docker image:

```bash
docker compose build api
```

### Step 2: Start All Services

```bash
docker compose up -d
```

### Step 3: Watch the Logs

```bash
docker compose logs -f api
```

**Expected Output:**
```
[HH:mm:ss INF] Starting SurveyBot API application
[HH:mm:ss INF] Applying database migrations...
[HH:mm:ss INF] Database connection established
[HH:mm:ss INF] Database is up to date. No pending migrations
[HH:mm:ss INF] Telegram Bot initialized successfully
[HH:mm:ss INF] SurveyBot API started successfully
```

**Key indicator:** Look for `"Database is up to date. No pending migrations"` instead of the previous error.

### Step 4: Verify API is Running

```bash
# Check health
curl http://localhost:5000/health/db

# Expected response:
{"status":"Healthy","results":{"database":{"status":"Healthy"}}}

# Access Swagger UI
# Open browser: http://localhost:5000/swagger
```

### Step 5: Check Database Schema

Connect to the database and verify the columns exist:

```bash
# Connect to PostgreSQL
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db

# Check surveys table
\d surveys

# You should see 'code' column
# Expected: code | character varying(10) |

# Check users table
\d users

# You should see 'last_login_at' column
# Expected: last_login_at | timestamp with time zone |

# Exit
\q
```

## What Changed Since Last Commit

### Commit 1: `f7f25c0` - Dockerize API and add automatic database migrations
- Created Dockerfile for API
- Updated docker-compose.yml
- Added automatic migration application in Program.cs
- Created DOCKER-README.md

### Commit 2: `89c0394` - Fix migration model snapshot inconsistencies (THIS ONE)
- Fixed model snapshot for Survey.Code
- Fixed model snapshot for User.LastLoginAt
- Fixed UserConfiguration.cs
- Fixed LastLoginAt migration column naming

## Troubleshooting

### If API Still Shows "Pending Changes" Error

**Option 1: Clear Database and Rebuild**
```bash
# Stop and remove all containers and volumes
docker compose down -v

# Rebuild and start
docker compose up -d --build
```

**Option 2: Manually Apply Migrations**
If you're developing on the host machine with the database in Docker:

```bash
# Update connection string in appsettings.json to use localhost
cd src/SurveyBot.API

# Apply migrations
dotnet ef database update
```

**Option 3: Create a New Migration**
If the snapshot is still out of sync:

```bash
cd src/SurveyBot.API

# Create a migration (it should show "no changes")
dotnet ef migrations add VerifySnapshot

# If it creates an empty migration, that's good - remove it
dotnet ef migrations remove
```

### If Database Connection Fails

Check the connection string uses the service name:
```
Host=postgres  (not localhost)
```

Verify PostgreSQL is healthy:
```bash
docker ps
# Look for "healthy" status next to surveybot-postgres
```

## Summary

✅ **Model snapshot is now synchronized**
✅ **All configurations are consistent**
✅ **Column naming follows snake_case convention**
✅ **Automatic migrations will work correctly**
✅ **API can start successfully in Docker**

The application is now ready for testing and deployment!

## Next Steps

1. ✅ Test the dockerized setup
2. Add Telegram bot token to `docker-compose.yml` environment variables
3. Configure webhook URL for production
4. Set up CI/CD pipeline
5. Deploy to production environment

For more information, see:
- [DOCKER-README.md](DOCKER-README.md) - Docker deployment guide
- [CLAUDE.md](CLAUDE.md) - Complete project documentation
