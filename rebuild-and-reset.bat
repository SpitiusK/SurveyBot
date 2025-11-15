@echo off
REM Script to rebuild API with updated migrations and reset database

echo === SurveyBot - Rebuild and Reset Script ===
echo.
echo This will:
echo   1. Stop all containers
echo   2. Remove old API image
echo   3. Rebuild API with updated migrations
echo   4. Drop and recreate database
echo   5. Start all containers (migrations apply automatically)
echo.
echo WARNING: This will DELETE ALL DATA in the database!
echo.
set /p CONFIRM="Continue? (yes/no): "

if /i NOT "%CONFIRM%"=="yes" (
    echo Aborted.
    exit /b 1
)

echo.
echo Step 1: Stopping all containers...
docker compose down

echo.
echo Step 2: Removing old API image...
docker rmi surveybot-api 2>nul

echo.
echo Step 3: Rebuilding API with updated migrations...
docker compose build --no-cache api

echo.
echo Step 4: Starting PostgreSQL...
docker compose up -d postgres

echo Waiting for PostgreSQL to be ready...
timeout /t 10 >nul

echo.
echo Step 5: Dropping old database...
docker exec -it surveybot-postgres psql -U surveybot_user -d postgres -c "DROP DATABASE IF EXISTS surveybot_db;"

echo.
echo Step 6: Creating fresh database...
docker exec -it surveybot-postgres psql -U surveybot_user -d postgres -c "CREATE DATABASE surveybot_db OWNER surveybot_user;"

echo.
echo Step 7: Starting API (all 3 migrations will apply)...
docker compose up -d api

echo.
echo Step 8: Watching logs...
echo You should see all 3 migrations apply:
echo   - 20251105190107_InitialCreate
echo   - 20251106000001_AddLastLoginAtToUser
echo   - 20251109000001_AddSurveyCodeColumn
echo.
echo Press Ctrl+C to exit log view
echo.
timeout /t 3 >nul
docker compose logs -f api
