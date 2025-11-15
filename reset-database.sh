#!/bin/bash
# Script to reset the database with corrected migrations

echo "=== SurveyBot Database Reset Script ==="
echo ""
echo "⚠️  WARNING: This will DELETE ALL DATA in the database!"
echo ""
read -p "Are you sure you want to continue? (yes/no): " -r
echo

if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]
then
    echo "Aborted."
    exit 1
fi

echo "Step 1: Stopping API container..."
docker compose stop api

echo ""
echo "Step 2: Dropping database..."
docker exec -it surveybot-postgres psql -U surveybot_user -d postgres -c "DROP DATABASE IF EXISTS surveybot_db;"

echo ""
echo "Step 3: Creating fresh database..."
docker exec -it surveybot-postgres psql -U surveybot_user -d postgres -c "CREATE DATABASE surveybot_db OWNER surveybot_user;"

echo ""
echo "Step 4: Starting API (migrations will apply automatically)..."
docker compose up -d api

echo ""
echo "Step 5: Watching logs..."
echo "Press Ctrl+C to exit log view"
echo ""
sleep 2
docker compose logs -f api
