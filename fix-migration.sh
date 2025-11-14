#!/bin/bash
# Script to fix migration snapshot issue

echo "=== SurveyBot Migration Fix Script ==="
echo ""
echo "This script will create a new migration to fix the model snapshot."
echo ""

# Navigate to API project
cd src/SurveyBot.API

# Check if dotnet ef is installed
if ! command -v dotnet-ef &> /dev/null
then
    echo "dotnet-ef not found. Installing..."
    dotnet tool install --global dotnet-ef
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

echo "Step 1: Creating new migration to fix snapshot..."
dotnet ef migrations add FixSurveyCodeSnapshot

echo ""
echo "Step 2: Reviewing the migration..."
echo "The migration should show the Code column for surveys."
echo ""

read -p "Does the migration look correct? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]
then
    echo "Migration created successfully!"
    echo ""
    echo "Next steps:"
    echo "1. Review the generated migration in src/SurveyBot.Infrastructure/Migrations/"
    echo "2. Build the Docker image: docker compose build api"
    echo "3. Start the application: docker compose up -d"
    echo ""
    echo "The migration will be applied automatically when the API starts."
else
    echo "Migration not confirmed. You can manually review it in:"
    echo "src/SurveyBot.Infrastructure/Migrations/"
fi
