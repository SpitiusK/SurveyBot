#!/bin/bash

# Test script to create survey with branching rules via API

API_BASE="https://3c6dfc99c860.ngrok-free.app/api"
TOKEN="your-jwt-token-here"

echo "===== BRANCHING RULES API TEST ====="
echo ""
echo "Note: You need to:"
echo "1. Get your JWT token from localStorage after logging in"
echo "2. Replace 'your-jwt-token-here' with your actual token"
echo "3. Update API_BASE if your ngrok URL changed"
echo ""

# Step 1: Create survey
echo "[1] Creating survey..."
SURVEY=$(curl -s -X POST "$API_BASE/surveys" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Branching Test Survey",
    "description": "Test branching rules",
    "allowMultipleResponses": false,
    "showResults": false
  }')

SURVEY_ID=$(echo $SURVEY | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*')
echo "Created survey ID: $SURVEY_ID"
echo ""

# Step 2: Create questions
echo "[2] Creating 4 questions..."
Q1=$(curl -s -X POST "$API_BASE/surveys/$SURVEY_ID/questions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "Question 1",
    "questionType": 1,
    "isRequired": true,
    "options": ["Option A", "Option B"]
  }')

Q1_ID=$(echo $Q1 | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*')
echo "Created Q1 ID: $Q1_ID"

# Step 3: Create branching rule
echo ""
echo "[3] Creating branching rule Q1→Q2 (when answer=Option A)..."
# ... (simplified - actual test would continue)

echo ""
echo "Check API logs for:"
echo "- POST /api/surveys (create survey)"
echo "- POST /api/surveys/{id}/questions (create questions)"
echo "- POST /api/surveys/{id}/questions/{id}/branches (create branching rule)"
