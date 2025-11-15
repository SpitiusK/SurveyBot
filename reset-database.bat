@echo off
REM Script to reset the database with corrected migrations

echo === SurveyBot Database Reset Script ===
echo.
echo WARNING: This will DELETE ALL DATA in the database!
echo.
set /p CONFIRM="Are you sure you want to continue? (yes/no): "

if /i NOT "%CONFIRM%"=="yes" (
    echo Aborted.
    exit /b 1
)

echo.
echo Step 1: Stopping API container...
docker compose stop api

echo.
echo Step 2: Dropping database...
docker exec -it surveybot-postgres psql -U surveybot_user -d postgres -c "DROP DATABASE IF EXISTS surveybot_db;"

echo.
echo Step 3: Creating fresh database...
docker exec -it surveybot-postgres psql -U surveybot_user -d postgres -c "CREATE DATABASE surveybot_db OWNER surveybot_user;"

echo.
echo Step 4: Starting API (migrations will apply automatically)...
docker compose up -d api

echo.
echo Step 5: Watching logs...
echo Press Ctrl+C to exit log view
echo.
timeout /t 2 >nul
docker compose logs -f api
