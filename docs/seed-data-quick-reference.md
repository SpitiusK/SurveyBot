# Seed Data Quick Reference

## Quick Usage

```csharp
// Easiest way - in Program.cs
await app.Services.SeedDatabaseAsync();
```

## What You Get

| Item | Count | Details |
|------|-------|---------|
| Users | 3 | john_doe, jane_smith, test_user |
| Surveys | 3 | Customer Sat., Product Feedback, Conference |
| Questions | 12 | 3 of each type |
| Responses | 5 | 4 complete, 1 incomplete |
| Answers | 18 | All question types covered |

## Sample Users

```
john_doe    (TID: 123456789) - Survey creator
jane_smith  (TID: 987654321) - Active respondent
test_user   (TID: 555555555) - Mixed responses
```

## Sample Surveys

### 1. Customer Satisfaction (Active)
- 4 questions: Rating, Multiple Choice, Single Choice, Text
- 2 complete responses

### 2. Product Feedback (Active)
- 4 questions: Single Choice, Rating, Multiple Choice, Text
- 2 complete responses

### 3. Conference Registration (Inactive)
- 4 questions: Text, Single Choice, Multiple Choice, Rating
- 1 incomplete response

## Answer Formats

```json
// Text
{ "AnswerText": "Great service!", "AnswerJson": null }

// Single Choice
{ "AnswerText": null, "AnswerJson": "{\"SelectedOption\":\"Friend Referral\"}" }

// Multiple Choice
{ "AnswerText": null, "AnswerJson": "[\"Online Support\",\"Mobile App\"]" }

// Rating
{ "AnswerText": null, "AnswerJson": "{\"Value\":5}" }
```

## Usage Options

```csharp
// Option 1: Seed only
await app.Services.SeedDatabaseAsync();

// Option 2: Migrate + Seed
await app.Services.MigrateDatabaseAsync();
await app.Services.SeedDatabaseAsync();

// Option 3: Reset everything (CAUTION!)
await app.Services.ResetAndSeedDatabaseAsync();
```

```powershell
# PowerShell
.\scripts\seed-database.ps1

# With custom connection
.\scripts\seed-database.ps1 -ConnectionString "Host=localhost;..."

# Reset and seed
.\scripts\seed-database.ps1 -Reset
```

## Verification

```sql
-- Quick count
SELECT COUNT(*) FROM users;     -- 3
SELECT COUNT(*) FROM surveys;   -- 3
SELECT COUNT(*) FROM questions; -- 12
SELECT COUNT(*) FROM responses; -- 5
SELECT COUNT(*) FROM answers;   -- 18
```

## Files

- Implementation: `src/SurveyBot.Infrastructure/Data/DataSeeder.cs`
- Extensions: `src/SurveyBot.Infrastructure/Data/Extensions/DatabaseExtensions.cs`
- Tests: `tests/SurveyBot.Tests/DataSeederTests.cs`
- Script: `scripts/seed-database.ps1`
- Docs: `docs/database-seeding-guide.md`
