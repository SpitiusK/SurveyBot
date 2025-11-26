# INFRA-003: CHECK Constraint Validation Matrix

**Purpose**: Visual guide to database CHECK constraint validation logic for `NextQuestionDeterminant` value object

---

## Database Schema

### New Columns Added

**Table: `questions`**
```sql
default_next_step_type    TEXT     -- 'GoToQuestion' | 'EndSurvey' | NULL
default_next_question_id  INTEGER  -- Foreign key to questions.id or NULL
```

**Table: `question_options`**
```sql
next_step_type    TEXT     -- 'GoToQuestion' | 'EndSurvey' | NULL
next_question_id  INTEGER  -- Foreign key to questions.id or NULL
```

---

## CHECK Constraint SQL

### Question.DefaultNext Constraint

```sql
ALTER TABLE questions ADD CONSTRAINT chk_question_default_next_invariant
CHECK (
    -- State 1: Uninitialized (both NULL)
    (default_next_step_type IS NULL AND default_next_question_id IS NULL)
    OR
    -- State 2: Go to specific question
    (default_next_step_type = 'GoToQuestion' AND
     default_next_question_id IS NOT NULL AND
     default_next_question_id > 0)
    OR
    -- State 3: End survey
    (default_next_step_type = 'EndSurvey' AND
     default_next_question_id IS NULL)
);
```

### QuestionOption.Next Constraint

```sql
ALTER TABLE question_options ADD CONSTRAINT chk_question_option_next_invariant
CHECK (
    -- State 1: Uninitialized (both NULL)
    (next_step_type IS NULL AND next_question_id IS NULL)
    OR
    -- State 2: Go to specific question
    (next_step_type = 'GoToQuestion' AND
     next_question_id IS NOT NULL AND
     next_question_id > 0)
    OR
    -- State 3: End survey
    (next_step_type = 'EndSurvey' AND
     next_question_id IS NULL)
);
```

---

## Validation Matrix

### Valid States ✅

| State | Type           | ID Value | Description                    | Use Case                          |
|-------|----------------|----------|--------------------------------|-----------------------------------|
| 1     | `NULL`         | `NULL`   | Uninitialized                  | Question created, flow not set    |
| 2     | `'GoToQuestion'` | `5`    | Go to Question 5               | Linear or branching flow          |
| 3     | `'GoToQuestion'` | `10`   | Go to Question 10              | Linear or branching flow          |
| 4     | `'EndSurvey'`  | `NULL`   | End survey                     | Terminal question/option          |

### Invalid States ❌

| State | Type           | ID Value | Why Invalid                                      | Database Error                    |
|-------|----------------|----------|--------------------------------------------------|-----------------------------------|
| 5     | `NULL`         | `5`      | ID without Type                                  | Violates CHECK constraint         |
| 6     | `'GoToQuestion'` | `NULL` | GoToQuestion requires valid ID                   | Violates CHECK constraint         |
| 7     | `'GoToQuestion'` | `0`    | 0 is not a valid question ID                    | Violates CHECK constraint (ID > 0)|
| 8     | `'GoToQuestion'` | `-1`   | Negative ID is not valid                         | Violates CHECK constraint (ID > 0)|
| 9     | `'EndSurvey'`  | `5`      | EndSurvey cannot have next question              | Violates CHECK constraint         |
| 10    | `'EndSurvey'`  | `0`      | EndSurvey must have NULL ID                      | Violates CHECK constraint         |
| 11    | `'Invalid'`    | `NULL`   | Type must be 'GoToQuestion' or 'EndSurvey'       | Violates CHECK constraint         |
| 12    | `'Invalid'`    | `5`      | Type must be 'GoToQuestion' or 'EndSurvey'       | Violates CHECK constraint         |

---

## Test Cases for Manual Verification

### After INFRA-004 Migration Application

Run these SQL statements to verify CHECK constraints work:

#### Test 1: Valid - Uninitialized (NULL, NULL) ✅
```sql
INSERT INTO surveys (title, creator_id, is_active, allow_multiple_responses, show_results, created_at, updated_at)
VALUES ('Test Survey', 1, false, false, false, NOW(), NOW());

INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Test Question', 'text', 0, true, NOW(), NOW(), NULL, NULL);
-- Expected: SUCCESS ✅
```

#### Test 2: Valid - GoToQuestion with ID ✅
```sql
-- First, create target question
INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Target Question', 'text', 1, true, NOW(), NOW(), 'EndSurvey', NULL);
-- Get the ID (let's say it's 2)

-- Now create question pointing to it
INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Source Question', 'text', 2, true, NOW(), NOW(), 'GoToQuestion', 2);
-- Expected: SUCCESS ✅
```

#### Test 3: Valid - EndSurvey with NULL ✅
```sql
INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Final Question', 'text', 3, true, NOW(), NOW(), 'EndSurvey', NULL);
-- Expected: SUCCESS ✅
```

#### Test 4: Invalid - Type without ID ❌
```sql
INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Invalid Question', 'text', 4, true, NOW(), NOW(), 'GoToQuestion', NULL);
-- Expected: ERROR - violates check constraint "chk_question_default_next_invariant"
```

#### Test 5: Invalid - ID without Type ❌
```sql
INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Invalid Question', 'text', 5, true, NOW(), NOW(), NULL, 2);
-- Expected: ERROR - violates check constraint "chk_question_default_next_invariant"
```

#### Test 6: Invalid - EndSurvey with ID ❌
```sql
INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Invalid Question', 'text', 6, true, NOW(), NOW(), 'EndSurvey', 2);
-- Expected: ERROR - violates check constraint "chk_question_default_next_invariant"
```

#### Test 7: Invalid - GoToQuestion with 0 ❌
```sql
INSERT INTO questions (survey_id, question_text, question_type, order_index, is_required, created_at, updated_at, default_next_step_type, default_next_question_id)
VALUES (1, 'Invalid Question', 'text', 7, true, NOW(), NOW(), 'GoToQuestion', 0);
-- Expected: ERROR - violates check constraint "chk_question_default_next_invariant"
```

---

## Foreign Key + CHECK Constraint Interaction

### Scenario: Cascade Delete Interaction

**Initial State**:
```
Question 5: { Type: 'GoToQuestion', Id: 10 }
Question 10: { Type: 'EndSurvey', Id: NULL }
```

**Action**: `DELETE FROM questions WHERE id = 10;`

**What Happens**:

1. **FK Constraint Attempts**: Set `Question 5.default_next_question_id = NULL`
   ```
   Question 5: { Type: 'GoToQuestion', Id: NULL }
   ```

2. **CHECK Constraint Validation**:
   - Checks: `(Type = 'GoToQuestion' AND Id IS NOT NULL AND Id > 0)`
   - Current: `(Type = 'GoToQuestion' AND Id IS NULL)`
   - Result: **VIOLATION** ❌

3. **Database Action**: **Transaction FAILS**, Question 10 NOT deleted

**Error Message**:
```
ERROR: update or delete on table "questions" violates foreign key constraint "fk_questions_default_next_question"
DETAIL: Key (id)=(10) is still referenced from table "questions".
```

### Solution (Implemented in INFRA-005)

**Before Deleting Question 10**:

1. Find all Questions/Options referencing Question 10:
   ```sql
   SELECT id FROM questions WHERE default_next_question_id = 10;
   SELECT id FROM question_options WHERE next_question_id = 10;
   ```

2. Update them to EndSurvey:
   ```sql
   UPDATE questions
   SET default_next_step_type = 'EndSurvey',
       default_next_question_id = NULL
   WHERE default_next_question_id = 10;

   UPDATE question_options
   SET next_step_type = 'EndSurvey',
       next_question_id = NULL
   WHERE next_question_id = 10;
   ```

3. Now delete Question 10:
   ```sql
   DELETE FROM questions WHERE id = 10;
   -- SUCCESS ✅
   ```

**Result**: All referencing entities now have valid state `{ Type: 'EndSurvey', Id: NULL }`

---

## QuestionOption Validation (Same Logic)

### Table: `question_options`

**Applies identical constraint**: `chk_question_option_next_invariant`

### Valid Option States

```sql
-- Option 1: No flow set (uninitialized)
INSERT INTO question_options (question_id, option_text, order_index, next_step_type, next_question_id)
VALUES (1, 'Option A', 0, NULL, NULL);
-- ✅ SUCCESS

-- Option 2: Go to Question 5
INSERT INTO question_options (question_id, option_text, order_index, next_step_type, next_question_id)
VALUES (1, 'Option B', 1, 'GoToQuestion', 5);
-- ✅ SUCCESS

-- Option 3: End survey
INSERT INTO question_options (question_id, option_text, order_index, next_step_type, next_question_id)
VALUES (1, 'Option C', 2, 'EndSurvey', NULL);
-- ✅ SUCCESS
```

### Invalid Option States

```sql
-- Invalid 1: Type without ID
INSERT INTO question_options (question_id, option_text, order_index, next_step_type, next_question_id)
VALUES (1, 'Option D', 3, 'GoToQuestion', NULL);
-- ❌ ERROR: violates check constraint

-- Invalid 2: EndSurvey with ID
INSERT INTO question_options (question_id, option_text, order_index, next_step_type, next_question_id)
VALUES (1, 'Option E', 4, 'EndSurvey', 5);
-- ❌ ERROR: violates check constraint
```

---

## Verification Queries for INFRA-004

### After Migration Application

#### 1. Verify Constraints Exist
```sql
SELECT
    conname AS constraint_name,
    contype AS constraint_type,
    conrelid::regclass AS table_name
FROM pg_constraint
WHERE conname IN (
    'chk_question_default_next_invariant',
    'chk_question_option_next_invariant'
);
-- Expected: 2 rows (one per constraint)
```

#### 2. Verify FK Constraints Exist
```sql
SELECT
    conname AS constraint_name,
    conrelid::regclass AS from_table,
    confrelid::regclass AS to_table
FROM pg_constraint
WHERE contype = 'f'
  AND conname IN (
    'fk_questions_default_next_question',
    'fk_question_options_next_question'
);
-- Expected: 2 rows (one per FK)
```

#### 3. Verify Indexes Exist
```sql
SELECT indexname, tablename
FROM pg_indexes
WHERE indexname IN (
    'idx_questions_default_next_question_id',
    'idx_question_options_next_question_id'
);
-- Expected: 2 rows (one per index)
```

#### 4. Verify Clean Slate
```sql
SELECT
    'users' AS table_name, COUNT(*) AS count FROM users
UNION ALL
SELECT 'surveys', COUNT(*) FROM surveys
UNION ALL
SELECT 'questions', COUNT(*) FROM questions
UNION ALL
SELECT 'question_options', COUNT(*) FROM question_options
UNION ALL
SELECT 'responses', COUNT(*) FROM responses
UNION ALL
SELECT 'answers', COUNT(*) FROM answers;
-- Expected: All counts = 0
```

---

## Expected PostgreSQL Error Messages

### CHECK Constraint Violation

```
ERROR:  new row for relation "questions" violates check constraint "chk_question_default_next_invariant"
DETAIL:  Failing row contains (..., GoToQuestion, null, ...).
```

### FK Violation with CHECK Interaction

```
ERROR:  update or delete on table "questions" violates foreign key constraint "fk_questions_default_next_question" on table "questions"
DETAIL:  Key (id)=(10) is still referenced from table "questions".
```

### Successful Operations

```
INSERT 0 1
-- OR
UPDATE 1
-- OR
DELETE 1
```

---

## Migration Impact Summary

### Before Migration

**Schema**:
- `questions.default_next_question_id` (int?, no constraints)
- `question_options.next_question_id` (int?, no constraints)

**Problem**: Could store invalid states (ID without type, type without ID)

### After Migration

**Schema**:
- `questions.default_next_step_type` (text, CHECK constraint)
- `questions.default_next_question_id` (int?, CHECK + FK constraints)
- `question_options.next_step_type` (text, CHECK constraint)
- `question_options.next_question_id` (int?, CHECK + FK constraints)

**Benefit**: Database-level validation prevents invalid states

**Trade-off**: Application must handle cascade delete logic (INFRA-005)

---

## Conclusion

The CHECK constraints implement the `NextQuestionDeterminant` value object invariants at the database level, providing:

1. **Type Safety**: Only valid enum values ('GoToQuestion', 'EndSurvey', NULL)
2. **Referential Integrity**: GoToQuestion must reference valid question
3. **State Consistency**: Type and ID must be synchronized
4. **Data Quality**: Invalid states rejected at database layer

**Next Steps**:
- INFRA-004: Apply migration and verify constraints work
- INFRA-005: Update service layer to work with new owned type properties
- TEST: Verify constraint violations happen as expected

---

**Generated**: 2025-11-23
**Task**: INFRA-003
**Related**: INFRA-004 (testing), INFRA-005 (service updates)
