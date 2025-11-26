# INFRA-003: Migration Flow Diagram

**Visual Guide**: Step-by-step execution flow of the customized migration

---

## Migration Up() Execution Flow

```
┌─────────────────────────────────────────────────────────────┐
│  START: Up() Method Execution                               │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 1: CLEAN SLATE - TRUNCATE CASCADE                     │
│  ────────────────────────────────────────────────────────── │
│  TRUNCATE TABLE answers RESTART IDENTITY CASCADE;           │
│  TRUNCATE TABLE responses RESTART IDENTITY CASCADE;         │
│  TRUNCATE TABLE question_options RESTART IDENTITY CASCADE;  │
│  TRUNCATE TABLE questions RESTART IDENTITY CASCADE;         │
│  TRUNCATE TABLE surveys RESTART IDENTITY CASCADE;           │
│  TRUNCATE TABLE users RESTART IDENTITY CASCADE;             │
│                                                              │
│  Result: All tables empty, sequences reset to 1             │
│  ⚠️  WARNING: All survey data permanently deleted           │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 2: DROP OLD INDEXES                                   │
│  ────────────────────────────────────────────────────────── │
│  DROP INDEX idx_questions_default_next_question_id;         │
│  DROP INDEX idx_question_options_next_question_id;          │
│                                                              │
│  Result: Old indexes removed (will be recreated later)      │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 3: DROP OLD FK CONSTRAINTS (IDEMPOTENT)               │
│  ────────────────────────────────────────────────────────── │
│  DROP CONSTRAINT IF EXISTS fk_questions_default_next_...;   │
│  DROP CONSTRAINT IF EXISTS fk_question_options_next_...;    │
│                                                              │
│  Result: Old FK constraints removed (if they existed)       │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 4: ADD NEW COLUMNS FOR VALUE OBJECT                   │
│  ────────────────────────────────────────────────────────── │
│  ALTER TABLE questions                                      │
│    ADD COLUMN default_next_step_type TEXT NULL;             │
│                                                              │
│  ALTER TABLE question_options                               │
│    ADD COLUMN next_step_type TEXT NULL;                     │
│                                                              │
│  Result: New columns added (existing columns remain)        │
│  Note: default_next_question_id, next_question_id exist     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 5: ADD CHECK CONSTRAINTS                              │
│  ────────────────────────────────────────────────────────── │
│  Question Constraint (chk_question_default_next_invariant): │
│    ✓ NULL/NULL - Uninitialized                              │
│    ✓ GoToQuestion/ID - Valid reference (ID > 0)             │
│    ✓ EndSurvey/NULL - End of survey                         │
│    ✗ GoToQuestion/NULL - Invalid                            │
│    ✗ EndSurvey/ID - Invalid                                 │
│    ✗ NULL/ID - Invalid                                      │
│                                                              │
│  QuestionOption Constraint (chk_question_option_next_...):  │
│    (Same rules as above)                                    │
│                                                              │
│  Result: Database enforces value object invariants          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 6: ADD FOREIGN KEY CONSTRAINTS                        │
│  ────────────────────────────────────────────────────────── │
│  FK: questions.default_next_question_id → questions.id      │
│    ON DELETE SET NULL                                       │
│                                                              │
│  FK: question_options.next_question_id → questions.id       │
│    ON DELETE SET NULL                                       │
│                                                              │
│  Result: Referential integrity enforced                     │
│  ⚠️  Interaction: SET NULL may violate CHECK constraint     │
│     (Application must handle cascade - INFRA-005)           │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 7: CREATE PERFORMANCE INDEXES                         │
│  ────────────────────────────────────────────────────────── │
│  CREATE INDEX idx_questions_default_next_question_id        │
│    ON questions(default_next_question_id);                  │
│                                                              │
│  CREATE INDEX idx_question_options_next_question_id         │
│    ON question_options(next_question_id);                   │
│                                                              │
│  Result: FK lookups and flow traversal optimized           │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  END: Migration Applied Successfully                        │
│  ────────────────────────────────────────────────────────── │
│  ✅ All tables empty (clean slate)                          │
│  ✅ New columns added                                        │
│  ✅ CHECK constraints active                                 │
│  ✅ FK constraints active                                    │
│  ✅ Indexes created                                          │
│  ✅ Schema ready for value object pattern                   │
└─────────────────────────────────────────────────────────────┘
```

---

## Migration Down() Execution Flow (Rollback)

```
┌─────────────────────────────────────────────────────────────┐
│  START: Down() Method Execution (Rollback)                 │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 1: DROP INDEXES                                       │
│  ────────────────────────────────────────────────────────── │
│  DROP INDEX idx_questions_default_next_question_id;         │
│  DROP INDEX idx_question_options_next_question_id;          │
│                                                              │
│  Result: Performance indexes removed                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 2: DROP FOREIGN KEY CONSTRAINTS                       │
│  ────────────────────────────────────────────────────────── │
│  DROP CONSTRAINT fk_questions_default_next_question;        │
│  DROP CONSTRAINT fk_question_options_next_question;         │
│                                                              │
│  Result: FK constraints removed                             │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 3: DROP CHECK CONSTRAINTS                             │
│  ────────────────────────────────────────────────────────── │
│  DROP CONSTRAINT chk_question_default_next_invariant;       │
│  DROP CONSTRAINT chk_question_option_next_invariant;        │
│                                                              │
│  Result: CHECK constraints removed                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  STEP 4: DROP NEW COLUMNS                                   │
│  ────────────────────────────────────────────────────────── │
│  ALTER TABLE questions DROP COLUMN default_next_step_type;  │
│  ALTER TABLE question_options DROP COLUMN next_step_type;   │
│                                                              │
│  Result: Value object columns removed                       │
│  Note: Original columns (default_next_question_id, etc)     │
│        remain unchanged                                     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  END: Migration Rolled Back                                 │
│  ────────────────────────────────────────────────────────── │
│  ✅ New columns removed                                      │
│  ✅ CHECK constraints removed                                │
│  ✅ FK constraints removed                                   │
│  ✅ Indexes removed                                          │
│  ⚠️  DATA STILL LOST (TRUNCATE is permanent)                │
│  ⚠️  Cannot restore truncated data                          │
└─────────────────────────────────────────────────────────────┘
```

---

## Database State Transitions

### Before Migration

```
┌─────────────────────────────┐
│  questions                  │
├─────────────────────────────┤
│  id                   PK    │
│  survey_id            FK    │
│  question_text              │
│  question_type              │
│  order_index                │
│  is_required                │
│  options_json         JSONB │
│  media_content        JSONB │
│  default_next_question_id   │  ← Simple int?, no constraints
│  created_at                 │
│  updated_at                 │
└─────────────────────────────┘

┌─────────────────────────────┐
│  question_options           │
├─────────────────────────────┤
│  id                   PK    │
│  question_id          FK    │
│  option_text                │
│  order_index                │
│  next_question_id           │  ← Simple int?, no constraints
└─────────────────────────────┘

Problems:
❌ No type information (how to interpret NULL? 0?)
❌ No validation (can store invalid states)
❌ No FK constraint (can reference non-existent questions)
```

### After Migration (Up)

```
┌─────────────────────────────┐
│  questions                  │
├─────────────────────────────┤
│  id                   PK    │
│  survey_id            FK    │
│  question_text              │
│  question_type              │
│  order_index                │
│  is_required                │
│  options_json         JSONB │
│  media_content        JSONB │
│  default_next_step_type     │  ← NEW: 'GoToQuestion' | 'EndSurvey' | NULL
│  default_next_question_id   │  ← Enhanced: FK, CHECK, Index
│  created_at                 │
│  updated_at                 │
└─────────────────────────────┘
         │
         └─────── FK ────────┐
                              │
┌─────────────────────────────┘
│  CHECK: Type/ID invariants
│  - NULL/NULL: Valid
│  - GoToQuestion/ID: Valid (ID > 0)
│  - EndSurvey/NULL: Valid
│  - GoToQuestion/NULL: INVALID
│  - EndSurvey/ID: INVALID
│  - NULL/ID: INVALID

┌─────────────────────────────┐
│  question_options           │
├─────────────────────────────┤
│  id                   PK    │
│  question_id          FK    │
│  option_text                │
│  order_index                │
│  next_step_type             │  ← NEW: 'GoToQuestion' | 'EndSurvey' | NULL
│  next_question_id           │  ← Enhanced: FK, CHECK, Index
└─────────────────────────────┘
         │
         └─────── FK ────────┐
                              │
┌─────────────────────────────┘
│  CHECK: Same invariants as questions

Benefits:
✅ Type information (explicit GoToQuestion/EndSurvey)
✅ Database-level validation (CHECK constraints)
✅ Referential integrity (FK constraints)
✅ Performance optimization (Indexes)
✅ Data integrity guarantees
```

---

## FK + CHECK Constraint Interaction Diagram

### Scenario: Deleting Referenced Question

```
INITIAL STATE:
┌─────────────────────┐       ┌─────────────────────┐
│  Question 5         │       │  Question 10        │
│  ─────────────────  │  ───> │  ─────────────────  │
│  Type: GoToQuestion │       │  Type: EndSurvey    │
│  ID:   10           │       │  ID:   NULL         │
└─────────────────────┘       └─────────────────────┘
         │                              │
         └──────── references ──────────┘

ACTION: DELETE FROM questions WHERE id = 10;

STEP 1: FK Constraint Attempts ON DELETE SET NULL
┌─────────────────────┐
│  Question 5         │
│  ─────────────────  │
│  Type: GoToQuestion │  ← FK tries to set ID to NULL
│  ID:   NULL         │  ← RESULT: { GoToQuestion, NULL }
└─────────────────────┘

STEP 2: CHECK Constraint Validation
┌──────────────────────────────────────────────────────┐
│  CHECK: (Type = 'GoToQuestion' AND ID IS NOT NULL)  │
│                                                      │
│  Current State: Type = 'GoToQuestion', ID = NULL    │
│                                                      │
│  Result: ❌ VIOLATION - CHECK constraint fails      │
└──────────────────────────────────────────────────────┘

STEP 3: Transaction Rollback
┌─────────────────────┐       ┌─────────────────────┐
│  Question 5         │       │  Question 10        │
│  ─────────────────  │  ───> │  ─────────────────  │
│  Type: GoToQuestion │       │  Type: EndSurvey    │
│  ID:   10           │       │  ID:   NULL         │
└─────────────────────┘       └─────────────────────┘
         │                              │
         └──────── still references ────┘

ERROR: Cannot delete Question 10 - still referenced by Question 5

SOLUTION (INFRA-005): Update references BEFORE deletion
┌─────────────────────┐
│  Question 5         │
│  ─────────────────  │
│  Type: EndSurvey    │  ← Changed by application
│  ID:   NULL         │  ← Changed by application
└─────────────────────┘

NOW: DELETE FROM questions WHERE id = 10;
✅ SUCCESS - No references remain
```

---

## Valid State Transitions

```
┌──────────────────┐
│  Uninitialized   │
│  NULL, NULL      │
└──────────────────┘
         │
         ├─────────────────────┐
         ▼                     ▼
┌──────────────────┐  ┌──────────────────┐
│  Go To Question  │  │   End Survey     │
│  GoToQuestion, 5 │  │  EndSurvey, NULL │
└──────────────────┘  └──────────────────┘
         │                     ▲
         └─────────────────────┘
              (can change)

Valid Transitions:
✅ NULL/NULL → GoToQuestion/5
✅ NULL/NULL → EndSurvey/NULL
✅ GoToQuestion/5 → GoToQuestion/10
✅ GoToQuestion/5 → EndSurvey/NULL
✅ EndSurvey/NULL → GoToQuestion/5
✅ EndSurvey/NULL → NULL/NULL (unset)

Invalid Transitions:
❌ NULL/NULL → GoToQuestion/NULL
❌ GoToQuestion/5 → GoToQuestion/NULL (unless Type changed)
❌ EndSurvey/NULL → EndSurvey/5
```

---

## Migration Safety Analysis

### Clean Slate Decision Tree

```
┌────────────────────────────────────────────────────┐
│  Need to apply migration?                          │
└────────────────────────────────────────────────────┘
                     │
                     ▼
            ┌────────────────────┐
            │  Production data?  │
            └────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
    ┌────────┐            ┌────────────┐
    │  YES   │            │  NO (DEV)  │
    └────────┘            └────────────┘
         │                       │
         ▼                       ▼
    ┌─────────────────┐   ┌──────────────┐
    │ ⚠️  DO NOT      │   │ ✅ SAFE TO   │
    │   APPLY!        │   │   APPLY      │
    │                 │   │              │
    │ Feature not     │   │ TRUNCATE     │
    │ deployed yet    │   │ will wipe    │
    │ in production   │   │ dev data     │
    │                 │   │ (expected)   │
    └─────────────────┘   └──────────────┘
```

---

## Summary Flow

```
Migration Execution Order:

1. TRUNCATE CASCADE        → Clean slate
2. DROP old indexes        → Prepare for changes
3. DROP old FK constraints → Remove old relationships
4. ADD new columns         → Value object support
5. ADD CHECK constraints   → Enforce invariants
6. ADD FK constraints      → Referential integrity
7. CREATE indexes          → Performance optimization

Result: Database ready for NextQuestionDeterminant value object

Next Steps:
→ INFRA-004: Apply migration locally and verify
→ INFRA-005: Update service layer to use new schema
→ TEST-005: Verify constraints enforce invariants
```

---

**Generated**: 2025-11-23
**Task**: INFRA-003
**Purpose**: Visual guide for understanding migration flow
