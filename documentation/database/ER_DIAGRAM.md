# Entity-Relationship Diagram
## Telegram Survey Bot MVP - Database Schema

### Version: 1.0.0
### Last Updated: 2025-11-05

---

## Visual ER Diagram

```
┌─────────────────────────┐
│        USERS            │
├─────────────────────────┤
│ PK  id                  │
│ UQ  telegram_id         │
│     username            │
│     first_name          │
│     last_name           │
│     created_at          │
│     updated_at          │
└─────────────────────────┘
            │
            │ 1
            │
            │ creates
            │
            │ *
            ▼
┌─────────────────────────┐          ┌─────────────────────────┐
│       SURVEYS           │          │      QUESTIONS          │
├─────────────────────────┤          ├─────────────────────────┤
│ PK  id                  │ 1     * │ PK  id                  │
│     title               │◄─────────┤ FK  survey_id           │
│     description         │  has     │     question_text       │
│ FK  creator_id          │          │     question_type       │
│     is_active           │          │     order_index         │
│     allow_multiple...   │          │     is_required         │
│     show_results        │          │     options_json        │
│     created_at          │          │     created_at          │
│     updated_at          │          └─────────────────────────┘
└─────────────────────────┘                      │
            │                                    │
            │ 1                                  │ 1
            │                                    │
            │ receives                           │ answered by
            │                                    │
            │ *                                  │ *
            ▼                                    ▼
┌─────────────────────────┐          ┌─────────────────────────┐
│      RESPONSES          │ 1     * │       ANSWERS           │
├─────────────────────────┤◄─────────├─────────────────────────┤
│ PK  id                  │  has     │ PK  id                  │
│ FK  survey_id           │          │ FK  response_id         │
│     respondent_tele...  │          │ FK  question_id         │
│     is_complete         │          │     answer_text         │
│     started_at          │          │     answer_json         │
│     submitted_at        │          │     created_at          │
└─────────────────────────┘          └─────────────────────────┘

Legend:
  PK  = Primary Key
  FK  = Foreign Key
  UQ  = Unique Constraint
  1   = One
  *   = Many
  ◄── = Relationship direction
```

---

## Entity Descriptions

### 1. USERS
**Purpose**: Central repository for all Telegram users who interact with the bot.

**Key Attributes**:
- `id`: Internal sequential identifier
- `telegram_id`: Unique Telegram user ID (from Telegram API)
- `username`: Telegram handle (optional, user may not have one)
- `first_name`, `last_name`: User's display name from Telegram

**Business Rules**:
- A user is automatically created on first interaction with the bot
- `telegram_id` must be unique (enforced by database constraint)
- Users cannot be deleted (soft delete should be implemented if needed)

---

### 2. SURVEYS
**Purpose**: Defines survey metadata, configuration, and ownership.

**Key Attributes**:
- `id`: Unique survey identifier
- `title`: Survey name (max 500 characters)
- `description`: Optional detailed description
- `creator_id`: References the user who created the survey
- `is_active`: Controls whether survey accepts new responses
- `allow_multiple_responses`: If true, same user can respond multiple times
- `show_results`: If true, show statistics after submission

**Business Rules**:
- Every survey must have a creator (user)
- A survey can be deactivated but not deleted (preserves response data)
- Creator can manage (edit/deactivate) their own surveys
- Minimum 1 question required before survey can be activated

**Lifecycle States**:
- Draft: `is_active = false`, no questions yet
- Active: `is_active = true`, accepting responses
- Closed: `is_active = false`, no longer accepting responses

---

### 3. QUESTIONS
**Purpose**: Stores individual questions within a survey with type-specific configuration.

**Key Attributes**:
- `id`: Unique question identifier
- `survey_id`: Parent survey reference
- `question_text`: The actual question content
- `question_type`: Enum-like field for question type
- `order_index`: Zero-based position in survey (0, 1, 2, ...)
- `is_required`: Must be answered before submission
- `options_json`: JSONB field for choice options

**Question Types**:
1. `text`: Free-form text answer
2. `multiple_choice`: Select multiple options
3. `single_choice`: Select one option
4. `rating`: Numeric rating (e.g., 1-5)
5. `yes_no`: Binary choice

**Options JSON Format**:
```json
{
  "options": ["Option 1", "Option 2", "Option 3"],
  "min_rating": 1,
  "max_rating": 5,
  "allow_other": false
}
```

**Business Rules**:
- Questions are ordered by `order_index` within a survey
- `order_index` must be unique per survey
- Choice-type questions must have `options_json` populated
- Questions cannot be deleted if answers exist (referential integrity)

---

### 4. RESPONSES
**Purpose**: Tracks individual user attempts to complete a survey.

**Key Attributes**:
- `id`: Unique response identifier
- `survey_id`: Which survey is being responded to
- `respondent_telegram_id`: Telegram ID of respondent (not FK to allow anonymous)
- `is_complete`: False during survey, true after submission
- `started_at`: When user began the survey
- `submitted_at`: When user completed submission

**Business Rules**:
- A response is created when user starts a survey
- Incomplete responses allow users to resume later
- `submitted_at` can only be set if `is_complete = true`
- Multiple responses per user only if survey allows it
- Response is linked to survey, not necessarily to registered user

**Response States**:
- In Progress: `is_complete = false`, `submitted_at = NULL`
- Completed: `is_complete = true`, `submitted_at` is set

---

### 5. ANSWERS
**Purpose**: Stores individual answer values for each question in a response.

**Key Attributes**:
- `id`: Unique answer identifier
- `response_id`: Parent response reference
- `question_id`: Which question is being answered
- `answer_text`: Simple text answer
- `answer_json`: Complex structured answer (for multiple choice, ratings, etc.)

**Answer Storage Strategy**:
- Text questions: Use `answer_text`
- Single choice: Store selected option in `answer_text`
- Multiple choice: Store array in `answer_json`: `["Option 1", "Option 3"]`
- Rating: Store numeric value in `answer_json`: `{"rating": 4}`
- Yes/No: Store boolean in `answer_json`: `{"value": true}`

**Business Rules**:
- Each question in a response should have exactly one answer
- Required questions must have answers before response completion
- At least one of `answer_text` or `answer_json` must be populated
- Unique constraint prevents duplicate answers per question

---

## Relationships

### 1. USERS → SURVEYS (One-to-Many)
- **Relationship**: A user creates many surveys
- **Cardinality**: 1:N
- **Foreign Key**: `surveys.creator_id` → `users.id`
- **Delete Rule**: CASCADE (if user deleted, delete their surveys)
- **Business Logic**:
  - User must exist before creating survey
  - User can retrieve all their created surveys
  - Survey always has exactly one creator

### 2. SURVEYS → QUESTIONS (One-to-Many)
- **Relationship**: A survey has many questions
- **Cardinality**: 1:N
- **Foreign Key**: `questions.survey_id` → `surveys.id`
- **Delete Rule**: CASCADE (if survey deleted, delete all questions)
- **Business Logic**:
  - Questions cannot exist without parent survey
  - Questions maintain order via `order_index`
  - Survey can have 0 to unlimited questions

### 3. SURVEYS → RESPONSES (One-to-Many)
- **Relationship**: A survey receives many responses
- **Cardinality**: 1:N
- **Foreign Key**: `responses.survey_id` → `surveys.id`
- **Delete Rule**: CASCADE (if survey deleted, delete all responses)
- **Business Logic**:
  - Responses track user interaction with survey
  - Survey can have 0 to unlimited responses
  - Deactivated surveys retain historical responses

### 4. RESPONSES → ANSWERS (One-to-Many)
- **Relationship**: A response has many answers
- **Cardinality**: 1:N
- **Foreign Key**: `answers.response_id` → `responses.id`
- **Delete Rule**: CASCADE (if response deleted, delete all answers)
- **Business Logic**:
  - One answer per question per response
  - Answers collected during response lifecycle
  - Answer count should match question count when complete

### 5. QUESTIONS → ANSWERS (One-to-Many)
- **Relationship**: A question has many answers (across all responses)
- **Cardinality**: 1:N
- **Foreign Key**: `answers.question_id` → `questions.id`
- **Delete Rule**: CASCADE (if question deleted, delete all answers)
- **Business Logic**:
  - Enables analytics: "all answers to this question"
  - Question text/type stored in question, not duplicated in answers
  - Changing question affects all future answers, not historical ones

---

## Referential Integrity

### CASCADE Delete Chains

When a **USER** is deleted:
```
USER → SURVEYS → QUESTIONS → ANSWERS
                        ↓
                   RESPONSES → ANSWERS
```
All surveys, their questions, responses, and answers are deleted.

When a **SURVEY** is deleted:
```
SURVEY → QUESTIONS → ANSWERS
         ↓
       RESPONSES → ANSWERS
```
All questions, responses, and answers are deleted.

When a **RESPONSE** is deleted:
```
RESPONSE → ANSWERS
```
All answers in that response are deleted.

When a **QUESTION** is deleted:
```
QUESTION → ANSWERS
```
All answers to that question (across all responses) are deleted.

---

## Constraints Summary

### Primary Keys
- All tables use sequential integer primary keys (`SERIAL`)
- Simple, efficient, and compatible with ORMs

### Foreign Keys
| Table     | Column           | References      | On Delete |
|-----------|------------------|-----------------|-----------|
| surveys   | creator_id       | users.id        | CASCADE   |
| questions | survey_id        | surveys.id      | CASCADE   |
| responses | survey_id        | surveys.id      | CASCADE   |
| answers   | response_id      | responses.id    | CASCADE   |
| answers   | question_id      | questions.id    | CASCADE   |

### Unique Constraints
| Table     | Columns                    | Purpose                          |
|-----------|----------------------------|----------------------------------|
| users     | telegram_id                | One record per Telegram user     |
| questions | survey_id, order_index     | No duplicate order in survey     |
| answers   | response_id, question_id   | One answer per question          |

### Check Constraints
| Table     | Constraint              | Rule                                    |
|-----------|-------------------------|-----------------------------------------|
| questions | chk_question_type       | Must be valid question type enum        |
| questions | chk_order_index         | Order must be >= 0                      |
| responses | chk_submitted_at        | Submit time must be after start time    |
| answers   | chk_answer_not_null     | Must have answer_text OR answer_json    |

---

## Indexes Strategy

### Performance-Critical Indexes

**Fast Lookups**:
- `users.telegram_id` - Primary user identification method
- `surveys.creator_id` - List user's surveys
- `questions.survey_id` - Fetch survey questions
- `responses.survey_id` - Survey analytics
- `answers.response_id` - Fetch response answers

**Composite Indexes** (for common query patterns):
- `questions(survey_id, order_index)` - Ordered question retrieval
- `surveys(creator_id, is_active)` - Active surveys by user
- `responses(survey_id, respondent_telegram_id)` - User's survey responses
- `answers(response_id, question_id)` - Specific answer lookup

**Specialized Indexes**:
- `surveys.is_active` (partial) - Only index active surveys
- `responses.is_complete` (partial) - Only index completed responses
- `questions.options_json` (GIN) - JSONB search capabilities
- `answers.answer_json` (GIN) - Analytics on structured answers

See `index_optimization.md` for detailed performance analysis.

---

## Data Types Rationale

### Integer vs BigInt
- `telegram_id`: BIGINT (Telegram IDs exceed INT range)
- Other IDs: SERIAL/INTEGER (sufficient for millions of records)

### Text vs Varchar
- `title`: VARCHAR(500) - Enforces reasonable survey title length
- `description`, `question_text`, `answer_text`: TEXT - Unlimited length for content

### JSONB for Flexibility
- `options_json`: Stores question configuration without schema changes
- `answer_json`: Stores complex answers without creating multiple tables
- Indexed with GIN for fast searches and analytics

### Timestamps
- All timestamps use `TIMESTAMP WITH TIME ZONE` for global consistency
- Automatic timezone conversion for international users

---

## Schema Evolution Guidelines

### Adding New Question Types
1. Add new type to `chk_question_type` constraint
2. Document JSON structure in this document
3. Update application code to handle new type
4. No schema migration needed (constraint update only)

### Adding Survey Features
- New survey settings: Add columns to `surveys` table
- Backward compatible: Use DEFAULT values
- Consider adding to `v_active_surveys` view

### Analytics Extensions
- Create materialized views for expensive calculations
- Add GIN indexes for JSONB queries
- Consider time-series tables for response tracking

---

## Notes

### Why telegram_id in responses is not a Foreign Key
The `respondent_telegram_id` field intentionally does not reference `users.id` to allow:
- Anonymous responses (user not registered in bot)
- Responses before user account created
- Flexibility for future multi-platform support

### Why separate answer_text and answer_json
- Performance: Text answers don't need JSON parsing
- Simplicity: Simple queries for text-based questions
- Flexibility: Complex answers use structured JSON

### Transaction Boundaries
- Creating survey + questions: Single transaction
- Submitting response: Single transaction (response + all answers)
- Analytics queries: Read-only, no transaction needed

---

## Quick Reference

### Table Sizes (MVP Estimates)
- Users: Thousands
- Surveys: Hundreds per active user
- Questions: 5-20 per survey
- Responses: Hundreds per popular survey
- Answers: Questions × Responses

### Query Performance Targets
- User lookup by telegram_id: < 1ms
- Survey with questions: < 10ms
- Response with answers: < 10ms
- Survey statistics: < 100ms (with proper indexes)

---

**Document Status**: Complete and reviewed
**Next Review**: After MVP testing phase
