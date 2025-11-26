# NextQuestionDeterminant Value Object Migration Guide

**Version**: 1.4.1 | **Date**: 2025-11-23 | **Status**: Clean Slate Approach

---

## CRITICAL WARNING

**⚠️ DATA LOSS WARNING ⚠️**

This migration uses a **CLEAN SLATE approach** and will **PERMANENTLY DELETE ALL EXISTING SURVEY DATA**:
- **Users, Surveys, Questions, QuestionOptions, Responses, Answers** - ALL TRUNCATED
- **No data recovery possible** - TRUNCATE RESTART IDENTITY bypasses undo log
- **Production environments**: Do NOT apply without explicit backup and approval
- **Development only**: Safe for development/testing environments
- **Mock data loss**: All test surveys, questions, responses will be deleted

---

## Why This Migration?

### The Problem: Magic Values

**v1.4.0 Approach** (Before Migration):
```csharp
public int? DefaultNextQuestionId { get; set; }  // 0 = end, >0 = question ID
```

**Issues**:
1. **Semantic Ambiguity**: What does `0` mean? Not obvious to developers
2. **No Type Safety**: Any int can be assigned; compiler can't prevent mistakes
3. **Runtime Errors**: Bugs only discovered during testing or production
4. **Maintainability**: Code comments required to explain magic values

### The Solution: Value Object Pattern (DDD)

**v1.4.1 Approach** (After Migration):
```csharp
public NextQuestionDeterminant DefaultNext { get; set; }
// - NextQuestionDeterminant.ToQuestion(5) = Navigate to Q5
// - NextQuestionDeterminant.End() = End survey
```

**Benefits**:
1. **Type Safety**: Compiler enforces valid states
2. **Self-Documenting**: Code clearly shows intent
3. **Immutable**: Cannot be accidentally modified
4. **Value Semantics**: Equality based on content, not reference
5. **Database Constraints**: CHECK constraints prevent invalid states

### Why Clean Slate?

The migration **cannot meaningfully convert** old primitive data to new owned type structure:
1. **Format Incompatibility**: `int?` → `(NextStepType enum, int?)` tuple
2. **No Meaningful Mapping**: Old `0` value semantically ambiguous (what's the enum equivalent?)
3. **Development Stage**: Feature in active development, no production data exists
4. **Clean Restart**: Ensures all future flows use new pattern correctly

---

## Pre-Migration Checklist

Before applying this migration:

- [ ] **BACKUP DATABASE**: `docker exec surveybot-postgres pg_dump -U surveybot_user surveybot_db > backup-$(date +%s).sql`
- [ ] **Verify environment**: `ASPNETCORE_ENVIRONMENT=Development` (NOT Production)
- [ ] **Understand data loss**: You WILL lose all surveys, questions, responses, users
- [ ] **No production data**: Confirm no valuable data in database
- [ ] **Team agreement**: Notify team of clean slate reset
- [ ] **Test migration**: Run in development first, not production
- [ ] **Review migration file**: Read `20251123131359_CleanSlateNextQuestionDeterminant.cs`

---

## Step-by-Step Migration

### Step 1: Backup Database

**MANDATORY**: Always backup before running destructive migrations.

```bash
# Backup entire database
docker exec surveybot-postgres pg_dump \
  -U surveybot_user \
  -d surveybot_db \
  --format=custom \
  --file=/backups/surveybot_backup_$(date +%Y%m%d_%H%M%S).dump

# Or if using volume mounting
docker exec surveybot-postgres pg_dump \
  -U surveybot_user \
  surveybot_db > ~/surveybot_backup_$(date +%Y%m%d_%H%M%S).sql
```

**Verify backup**:
```bash
# List backup files
ls -lh ~/surveybot_backup_*.sql
```

### Step 2: Update NuGet Packages (if needed)

Ensure all project dependencies are restored:

```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet restore
dotnet build
```

### Step 3: Apply Migration

Navigate to API project and apply the migration:

```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API

# Apply migration to development database
dotnet ef database update --project ../SurveyBot.Infrastructure

# Or specific migration:
dotnet ef database update CleanSlateNextQuestionDeterminant --project ../SurveyBot.Infrastructure
```

**Expected Output**:
```
Build started...
Build succeeded.
Applying migration '20251123131359_CleanSlateNextQuestionDeterminant'...
Done.
```

**If migration fails**, see "Rollback Procedure" section below.

### Step 4: Verify Migration Applied

```bash
# Check applied migrations
dotnet ef migrations list --project ../SurveyBot.Infrastructure

# Should show:
# 20251123010631_RemoveNextQuestionFKConstraints (Applied)
# 20251123131359_CleanSlateNextQuestionDeterminant (Applied)

# Connect to database to verify schema
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "\d questions"
```

**Table structure should show**:
```
Column                    |            Type
--________________________|_____________________
default_next_step_type    | text
default_next_question_id  | integer
```

### Step 5: Verify Constraints

Check that CHECK constraints were created:

```bash
# List all constraints on questions table
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "SELECT constraint_name, constraint_type
      FROM information_schema.table_constraints
      WHERE table_name='questions' AND constraint_type='CHECK';"
```

**Should output**:
```
constraint_name                         | constraint_type
__________________________________________|________________
chk_question_default_next_invariant      | CHECK
chk_question_option_next_invariant       | CHECK
```

### Step 6: Verify Foreign Keys

Check that FK constraints were recreated:

```bash
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "SELECT constraint_name
      FROM information_schema.table_constraints
      WHERE table_name IN ('questions', 'question_options')
        AND constraint_type='FOREIGN KEY';"
```

**Should output**:
```
fk_questions_default_next_question
fk_question_options_next_question
```

### Step 7: Run Application

Start the application to verify no startup errors:

```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run
```

**Expected output**:
```
[HH:MM:SS INF] Starting SurveyBot API application
[HH:MM:SS INF] Entity Framework Core initialized successfully
[HH:MM:SS INF] SurveyBot API started successfully
Now listening on: http://localhost:5000
```

**In Swagger** (http://localhost:5000/swagger):
- Navigate to any endpoint
- Create a new survey (will work with empty questions)
- Verify no database errors in logs

---

## What Gets Deleted

This migration performs **TRUNCATE CASCADE** on all survey-related tables:

| Table | Records | Reason |
|-------|---------|--------|
| answers | ALL | Depends on responses |
| responses | ALL | Depends on surveys |
| question_options | ALL | Depends on questions |
| questions | ALL | Schema change, cascade delete |
| surveys | ALL | Schema change, cascade delete |
| users | ALL | Cascade delete via surveys |

**Recovery**: Use database backup from Step 1 if needed.

---

## Schema Changes Summary

### New Columns Added

**questions table**:
```sql
-- New columns for NextQuestionDeterminant value object
default_next_step_type TEXT          -- 'GoToQuestion' or 'EndSurvey'
default_next_question_id INT         -- Target question ID (null if EndSurvey)
```

**question_options table**:
```sql
-- New columns for NextQuestionDeterminant value object
next_step_type TEXT                  -- 'GoToQuestion' or 'EndSurvey'
next_question_id INT                 -- Target question ID (null if EndSurvey)
```

### Constraints Added

**CHECK Constraints** (Enforce Value Object Invariants):
```sql
-- Question.DefaultNext invariant
chk_question_default_next_invariant
  (default_next_step_type IS NULL AND default_next_question_id IS NULL) OR
  (default_next_step_type = 'GoToQuestion' AND default_next_question_id IS NOT NULL AND default_next_question_id > 0) OR
  (default_next_step_type = 'EndSurvey' AND default_next_question_id IS NULL)

-- QuestionOption.Next invariant
chk_question_option_next_invariant
  (next_step_type IS NULL AND next_question_id IS NULL) OR
  (next_step_type = 'GoToQuestion' AND next_question_id IS NOT NULL AND next_question_id > 0) OR
  (next_step_type = 'EndSurvey' AND next_question_id IS NULL)
```

**Foreign Keys** (Link to Questions):
```sql
fk_questions_default_next_question
  questions(default_next_question_id) → questions(id)
  ON DELETE SET NULL

fk_question_options_next_question
  question_options(next_question_id) → questions(id)
  ON DELETE SET NULL
```

**Indexes** (Performance):
```sql
idx_questions_default_next_question_id
idx_question_options_next_question_id
```

### Columns Removed

**None**: Migration is additive. Old columns are not removed (kept for compatibility if needed).

---

## Post-Migration Verification Queries

### 1. Verify Table Structure

```bash
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "\d+ questions"
```

**Look for**:
- `default_next_step_type` column (text type)
- `default_next_question_id` column (int type)
- Constraints listed at bottom

### 2. Test CHECK Constraint

```bash
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "INSERT INTO questions
      (survey_id, question_text, question_type, order_index, is_required,
       default_next_step_type, default_next_question_id, created_at, updated_at)
      VALUES (999, 'Test', 0, 0, true, 'Invalid', 5, NOW(), NOW());"
```

**Expected**: Error - `CHECK constraint "chk_question_default_next_invariant" is violated`

This proves the constraint is working.

### 3. Test Valid Insertion

```bash
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "INSERT INTO questions
      (survey_id, question_text, question_type, order_index, is_required,
       default_next_step_type, default_next_question_id, created_at, updated_at)
      VALUES (1, 'Test Question', 0, 0, true, 'EndSurvey', NULL, NOW(), NOW())
      RETURNING id, default_next_step_type, default_next_question_id;"
```

**Expected**: Successfully inserted row with `EndSurvey` and `NULL` ID.

### 4. Verify Data is Empty

```bash
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "SELECT COUNT(*) as user_count FROM users;
      SELECT COUNT(*) as survey_count FROM surveys;
      SELECT COUNT(*) as question_count FROM questions;
      SELECT COUNT(*) as response_count FROM responses;
      SELECT COUNT(*) as answer_count FROM answers;"
```

**Expected**: All counts = 0 (data was truncated)

### 5. List Constraints

```bash
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "SELECT constraint_name, constraint_type, table_name
      FROM information_schema.table_constraints
      WHERE table_name IN ('questions', 'question_options')
      ORDER BY table_name, constraint_type;"
```

**Expected output shows**:
- `chk_question_default_next_invariant` (CHECK)
- `chk_question_option_next_invariant` (CHECK)
- `fk_questions_default_next_question` (FOREIGN KEY)
- `fk_question_options_next_question` (FOREIGN KEY)
- Regular primary and unique keys

---

## Rollback Procedure

### If Migration Fails During Application

**Option 1: Restore from Backup** (RECOMMENDED)

```bash
# Stop database
docker-compose stop postgres

# Restore from backup
docker exec surveybot-postgres pg_restore \
  -U surveybot_user \
  -d surveybot_db \
  /backups/surveybot_backup_20251123_123456.dump

# Restart
docker-compose up -d postgres
```

### If Migration Partially Applied

**Option 2: Rollback Migration**

```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API

# Rollback to previous migration
dotnet ef database update RemoveNextQuestionFKConstraints --project ../SurveyBot.Infrastructure
```

**Expected**: Removes new columns and constraints, reverts to previous schema.

**WARNING**: This only works if migration was applied but NOT committed to source control. If you've already pushed the migration, rolling back will cause version mismatches.

### If Application Won't Start

**Check logs**:
```bash
dotnet run 2>&1 | tail -50
```

**Common errors**:
- Missing enum conversion: Update Configuration
- Missing columns: Re-apply migration
- CHECK constraint violation: Review data types in entity

**Contact**: If unable to resolve, restore from backup and try again after verifying configuration.

---

## Verification Checklist (Post-Migration)

- [ ] Migration applied successfully (`dotnet ef database update`)
- [ ] Database backup created (`pg_dump`)
- [ ] Constraints verified in PostgreSQL
- [ ] Foreign keys verified in PostgreSQL
- [ ] Test CHECK constraint (should reject invalid values)
- [ ] Test valid insertion (EndSurvey and GoToQuestion)
- [ ] Application starts without errors (`dotnet run`)
- [ ] Swagger UI loads (`http://localhost:5000/swagger`)
- [ ] Can create new survey (via Swagger POST /api/surveys)
- [ ] Can create question with DefaultNext (via API)
- [ ] Database logs show no errors
- [ ] All previous tables exist (check `\dt` in psql)

---

## Code Changes Summary

After this migration, code using conditional flow changes:

### Before (v1.4.0)
```csharp
// Entity
public int? DefaultNextQuestionId { get; set; }  // Magic 0 = end

// Service
if (defaultNextId == 0)
{
    // End survey
}
else
{
    // Go to question
}
```

### After (v1.4.1)
```csharp
// Entity
public NextQuestionDeterminant DefaultNext { get; set; }  // Value object

// Service
if (defaultNext.Type == NextStepType.EndSurvey)
{
    // End survey
}
else if (defaultNext.Type == NextStepType.GoToQuestion)
{
    // Go to question with defaultNext.NextQuestionId.Value
}
```

### Configuration
```csharp
// EF Core Configuration (Owned Type)
builder.OwnsOne(q => q.DefaultNext, nb =>
{
    nb.Property(n => n.Type)
        .HasColumnName("default_next_step_type")
        .HasConversion<string>();  // Store enum as text

    nb.Property(n => n.NextQuestionId)
        .HasColumnName("default_next_question_id");
});
```

---

## Performance Impact

**Positive**:
- Indexes created on new FK columns improve cycle detection performance
- CHECK constraints prevent invalid data, reducing validation overhead
- Owned type mapping efficient (single table, no joins)

**Neutral**:
- Additional columns add ~10 bytes per question row
- Minimal impact on survey operations

**Testing**:
```bash
# Benchmark before/after
dotnet test --filter "Performance" --configuration Release
```

---

## Common Issues

### Issue 1: "Database already up to date"

**Cause**: Migration already applied

**Solution**:
```bash
# Check migration list
dotnet ef migrations list --project ../SurveyBot.Infrastructure

# If CleanSlateNextQuestionDeterminant shows "(Applied)", it's done
```

### Issue 2: "Constraint chk_question_default_next_invariant already exists"

**Cause**: Migration ran twice, constraint already created

**Solution**:
```bash
# Drop constraint and reapply
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db \
  -c "ALTER TABLE questions DROP CONSTRAINT IF EXISTS chk_question_default_next_invariant;"

# Reapply migration
dotnet ef database update --force --project ../SurveyBot.Infrastructure
```

### Issue 3: "Column default_next_step_type does not exist"

**Cause**: Migration failed partway through

**Solution**:
1. Restore from backup (recommended)
2. Or manually add columns:
```bash
docker exec -it surveybot-postgres psql \
  -U surveybot_user \
  -d surveybot_db << EOF
ALTER TABLE questions
ADD COLUMN IF NOT EXISTS default_next_step_type TEXT;

ALTER TABLE questions
ADD COLUMN IF NOT EXISTS default_next_question_id INT;
EOF
```

### Issue 4: "All data was deleted but I didn't want that"

**Cause**: Clean slate migration was applied

**Solution**: Restore from backup (Step 1)

```bash
docker-compose stop postgres
docker exec surveybot-postgres pg_restore \
  -U surveybot_user \
  -d surveybot_db \
  /path/to/backup.dump
docker-compose up -d postgres
```

---

## Support

For issues or questions:
1. Check this guide's "Common Issues" section
2. Review migration file: `20251123131359_CleanSlateNextQuestionDeterminant.cs`
3. Check Infrastructure CLAUDE.md: "Conditional Flow Architecture" section
4. Review PostgreSQL logs: `docker-compose logs postgres`
5. Contact team lead if unable to resolve

---

## Additional Resources

- **Infrastructure Layer**: [CLAUDE.md](../../src/SurveyBot.Infrastructure/CLAUDE.md) - "Value Object Persistence" & "Conditional Flow Architecture"
- **Core Layer**: [CLAUDE.md](../../src/SurveyBot.Core/CLAUDE.md) - Entity definitions and value objects
- **Migration File**: `20251123131359_CleanSlateNextQuestionDeterminant.cs` - Exact SQL executed
- **Database Documentation**: [README.md](../database/README.md) - Schema overview

---

**Last Updated**: 2025-11-23 | **Version**: 1.4.1 | **Status**: Production-Ready (for development environments)
