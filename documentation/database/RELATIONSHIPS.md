# Database Relationships Documentation
## Telegram Survey Bot MVP

### Version: 1.0.0
### Last Updated: 2025-11-05

---

## Table of Contents
1. [Relationship Overview](#relationship-overview)
2. [Detailed Relationship Mappings](#detailed-relationship-mappings)
3. [Cardinality Rules](#cardinality-rules)
4. [Referential Integrity](#referential-integrity)
5. [Query Patterns](#query-patterns)
6. [Data Access Patterns](#data-access-patterns)

---

## Relationship Overview

### Relationship Graph
```
USERS (1) ──creates──► (N) SURVEYS
                           │
                           │ has
                           ▼
                        QUESTIONS (N)
                           │
                           │ answered by
                           ▼
SURVEYS (1) ──receives──► (N) RESPONSES (1) ──has──► (N) ANSWERS
                                                          │
                                                          │ references
                                                          ▼
                                                     QUESTIONS (N)
```

### Summary Table
| Relationship | Type | Parent Table | Child Table | Foreign Key | Delete Rule |
|--------------|------|--------------|-------------|-------------|-------------|
| R1 | 1:N | users | surveys | creator_id | CASCADE |
| R2 | 1:N | surveys | questions | survey_id | CASCADE |
| R3 | 1:N | surveys | responses | survey_id | CASCADE |
| R4 | 1:N | responses | answers | response_id | CASCADE |
| R5 | 1:N | questions | answers | question_id | CASCADE |

---

## Detailed Relationship Mappings

### R1: USERS → SURVEYS (Creator Relationship)

**Type**: One-to-Many (1:N)

**Description**: A user can create multiple surveys, but each survey has exactly one creator.

**Technical Details**:
```sql
-- Foreign Key Definition
ALTER TABLE surveys
ADD CONSTRAINT fk_surveys_creator
FOREIGN KEY (creator_id) REFERENCES users(id)
ON DELETE CASCADE;

-- Supporting Index
CREATE INDEX idx_surveys_creator_id ON surveys(creator_id);
```

**Cardinality**:
- Minimum: A user can have 0 surveys (new user)
- Maximum: A user can have unlimited surveys (N)
- Mandatory: A survey MUST have exactly 1 creator

**Business Rules**:
1. Creator must exist in users table before creating survey
2. Creator ID cannot be NULL
3. Creator cannot be changed after survey creation (in MVP)
4. If creator account is deleted, all their surveys are deleted (CASCADE)

**Use Cases**:
- List all surveys created by a user
- Find survey creator information
- Count surveys per user
- Filter surveys by creator

**Example Queries**:
```sql
-- Get all surveys by a user
SELECT * FROM surveys
WHERE creator_id = 123
ORDER BY created_at DESC;

-- Get survey with creator info
SELECT s.*, u.username, u.telegram_id
FROM surveys s
JOIN users u ON s.creator_id = u.id
WHERE s.id = 456;

-- Count surveys per user
SELECT u.username, COUNT(s.id) as survey_count
FROM users u
LEFT JOIN surveys s ON u.id = s.creator_id
GROUP BY u.id, u.username;
```

**Navigation**:
- Forward: `user.surveys` (User → Surveys)
- Backward: `survey.creator` (Survey → User)

---

### R2: SURVEYS → QUESTIONS (Composition Relationship)

**Type**: One-to-Many (1:N)

**Description**: A survey contains multiple questions in a specific order.

**Technical Details**:
```sql
-- Foreign Key Definition
ALTER TABLE questions
ADD CONSTRAINT fk_questions_survey
FOREIGN KEY (survey_id) REFERENCES surveys(id)
ON DELETE CASCADE;

-- Supporting Indexes
CREATE INDEX idx_questions_survey_id ON questions(survey_id);
CREATE INDEX idx_questions_survey_order ON questions(survey_id, order_index);
CREATE UNIQUE INDEX idx_questions_survey_order_unique
ON questions(survey_id, order_index);
```

**Cardinality**:
- Minimum: A survey can have 0 questions (draft state)
- Maximum: A survey can have unlimited questions (N)
- Mandatory: A question MUST belong to exactly 1 survey

**Business Rules**:
1. Questions are ordered using `order_index` (0-based)
2. Each question has unique order within survey
3. Questions cannot be shared between surveys
4. If survey is deleted, all questions are deleted (CASCADE)
5. Order must be consecutive (0, 1, 2, ...) for UI purposes
6. Reordering questions updates `order_index` values

**Use Cases**:
- Fetch all questions for survey display
- Retrieve questions in correct order
- Add/remove questions from survey
- Reorder questions in survey builder

**Example Queries**:
```sql
-- Get all questions for a survey in order
SELECT * FROM questions
WHERE survey_id = 789
ORDER BY order_index ASC;

-- Count questions in survey
SELECT survey_id, COUNT(*) as question_count
FROM questions
GROUP BY survey_id;

-- Get question with survey context
SELECT q.*, s.title as survey_title
FROM questions q
JOIN surveys s ON q.survey_id = s.id
WHERE q.id = 101;

-- Check if survey has required questions
SELECT survey_id,
       COUNT(*) FILTER (WHERE is_required = true) as required_count
FROM questions
WHERE survey_id = 789
GROUP BY survey_id;
```

**Ordering Logic**:
```sql
-- Insert new question at end
INSERT INTO questions (survey_id, question_text, order_index, ...)
SELECT 789, 'New question', COALESCE(MAX(order_index) + 1, 0), ...
FROM questions WHERE survey_id = 789;

-- Reorder: move question from position 2 to position 0
BEGIN;
UPDATE questions SET order_index = order_index + 1
WHERE survey_id = 789 AND order_index < 2;
UPDATE questions SET order_index = 0
WHERE id = 555;
COMMIT;
```

**Navigation**:
- Forward: `survey.questions` (Survey → Questions)
- Backward: `question.survey` (Question → Survey)

---

### R3: SURVEYS → RESPONSES (Collection Relationship)

**Type**: One-to-Many (1:N)

**Description**: A survey can receive multiple responses from users.

**Technical Details**:
```sql
-- Foreign Key Definition
ALTER TABLE responses
ADD CONSTRAINT fk_responses_survey
FOREIGN KEY (survey_id) REFERENCES surveys(id)
ON DELETE CASCADE;

-- Supporting Indexes
CREATE INDEX idx_responses_survey_id ON responses(survey_id);
CREATE INDEX idx_responses_survey_respondent
ON responses(survey_id, respondent_telegram_id);
```

**Cardinality**:
- Minimum: A survey can have 0 responses (new survey)
- Maximum: A survey can have unlimited responses (N)
- Mandatory: A response MUST belong to exactly 1 survey

**Business Rules**:
1. Respondent identified by `respondent_telegram_id` (not FK)
2. Multiple responses per user allowed if `allow_multiple_responses = true`
3. Response lifecycle: started → in_progress → completed
4. Incomplete responses can be resumed
5. If survey is deleted, all responses are deleted (CASCADE)
6. Completed responses are immutable (business logic, not DB constraint)

**Use Cases**:
- Count total responses for survey
- Get all responses for analytics
- Check if user already responded
- Calculate completion rate
- Find incomplete responses for reminders

**Example Queries**:
```sql
-- Count completed responses
SELECT survey_id, COUNT(*) as response_count
FROM responses
WHERE is_complete = true
GROUP BY survey_id;

-- Check if user already responded
SELECT EXISTS(
    SELECT 1 FROM responses
    WHERE survey_id = 789
    AND respondent_telegram_id = 123456789
    AND is_complete = true
) as has_responded;

-- Get completion rate
SELECT
    survey_id,
    COUNT(*) as total_started,
    COUNT(*) FILTER (WHERE is_complete = true) as completed,
    ROUND(100.0 * COUNT(*) FILTER (WHERE is_complete = true) / COUNT(*), 2) as completion_rate
FROM responses
GROUP BY survey_id;

-- Find abandoned responses (started > 1 hour ago, not completed)
SELECT * FROM responses
WHERE is_complete = false
AND started_at < NOW() - INTERVAL '1 hour';

-- Get unique respondents
SELECT survey_id, COUNT(DISTINCT respondent_telegram_id) as unique_respondents
FROM responses
WHERE is_complete = true
GROUP BY survey_id;
```

**Navigation**:
- Forward: `survey.responses` (Survey → Responses)
- Backward: `response.survey` (Response → Survey)

---

### R4: RESPONSES → ANSWERS (Composition Relationship)

**Type**: One-to-Many (1:N)

**Description**: A response contains multiple answers, one for each question.

**Technical Details**:
```sql
-- Foreign Key Definition
ALTER TABLE answers
ADD CONSTRAINT fk_answers_response
FOREIGN KEY (response_id) REFERENCES responses(id)
ON DELETE CASCADE;

-- Supporting Indexes
CREATE INDEX idx_answers_response_id ON answers(response_id);
CREATE INDEX idx_answers_response_question
ON answers(response_id, question_id);
CREATE UNIQUE INDEX idx_answers_response_question_unique
ON answers(response_id, question_id);
```

**Cardinality**:
- Minimum: A response can have 0 answers (just started)
- Maximum: A response has N answers (equal to question count)
- Mandatory: An answer MUST belong to exactly 1 response

**Business Rules**:
1. Each question can be answered at most once per response
2. Answer count should match question count when response is complete
3. Required questions must have answers before completion
4. If response is deleted, all answers are deleted (CASCADE)
5. Answers are immutable after response completion (business logic)

**Use Cases**:
- Fetch all answers for a response
- Display user's completed survey
- Validate response completeness
- Export response data

**Example Queries**:
```sql
-- Get all answers for a response
SELECT a.*, q.question_text, q.question_type
FROM answers a
JOIN questions q ON a.question_id = q.id
WHERE a.response_id = 999
ORDER BY q.order_index;

-- Check response completeness
SELECT
    r.id,
    r.is_complete,
    COUNT(DISTINCT q.id) as total_questions,
    COUNT(DISTINCT a.id) as answered_questions,
    CASE
        WHEN COUNT(DISTINCT q.id) = COUNT(DISTINCT a.id) THEN true
        ELSE false
    END as is_fully_answered
FROM responses r
JOIN questions q ON r.survey_id = q.survey_id
LEFT JOIN answers a ON a.response_id = r.id AND a.question_id = q.id
WHERE r.id = 999
GROUP BY r.id, r.is_complete;

-- Get unanswered questions for response
SELECT q.*
FROM questions q
LEFT JOIN answers a ON a.question_id = q.id AND a.response_id = 999
WHERE q.survey_id = (SELECT survey_id FROM responses WHERE id = 999)
AND a.id IS NULL
ORDER BY q.order_index;
```

**Navigation**:
- Forward: `response.answers` (Response → Answers)
- Backward: `answer.response` (Answer → Response)

---

### R5: QUESTIONS → ANSWERS (Reference Relationship)

**Type**: One-to-Many (1:N)

**Description**: A question has many answers across all responses (analytics dimension).

**Technical Details**:
```sql
-- Foreign Key Definition
ALTER TABLE answers
ADD CONSTRAINT fk_answers_question
FOREIGN KEY (question_id) REFERENCES questions(id)
ON DELETE CASCADE;

-- Supporting Indexes
CREATE INDEX idx_answers_question_id ON answers(question_id);
CREATE INDEX idx_answers_answer_json ON answers USING GIN (answer_json);
```

**Cardinality**:
- Minimum: A question can have 0 answers (new question, no responses yet)
- Maximum: A question can have unlimited answers (N)
- Mandatory: An answer MUST reference exactly 1 question

**Business Rules**:
1. Answer references question for context and validation
2. Question type determines answer storage format
3. If question is deleted, all its answers are deleted (CASCADE)
4. This relationship enables analytics and reporting

**Use Cases**:
- Analytics: aggregate answers per question
- Generate response statistics
- Display answer distribution
- Export survey results
- Analyze question effectiveness

**Example Queries**:
```sql
-- Get all answers for a question (across all responses)
SELECT a.*, r.respondent_telegram_id, r.submitted_at
FROM answers a
JOIN responses r ON a.response_id = r.id
WHERE a.question_id = 777
AND r.is_complete = true;

-- Text question: Get all text answers
SELECT answer_text, COUNT(*) as frequency
FROM answers
WHERE question_id = 777
AND answer_text IS NOT NULL
GROUP BY answer_text
ORDER BY frequency DESC;

-- Single choice: Answer distribution
SELECT
    answer_text as choice,
    COUNT(*) as count,
    ROUND(100.0 * COUNT(*) / SUM(COUNT(*)) OVER(), 2) as percentage
FROM answers
WHERE question_id = 777
GROUP BY answer_text
ORDER BY count DESC;

-- Multiple choice: Option frequency
SELECT
    jsonb_array_elements_text(answer_json) as option,
    COUNT(*) as count
FROM answers
WHERE question_id = 777
AND answer_json IS NOT NULL
GROUP BY option
ORDER BY count DESC;

-- Rating: Calculate statistics
SELECT
    question_id,
    AVG((answer_json->>'rating')::numeric) as avg_rating,
    MIN((answer_json->>'rating')::numeric) as min_rating,
    MAX((answer_json->>'rating')::numeric) as max_rating,
    COUNT(*) as response_count
FROM answers
WHERE question_id = 777
AND answer_json ? 'rating'
GROUP BY question_id;
```

**Navigation**:
- Forward: `question.answers` (Question → Answers)
- Backward: `answer.question` (Answer → Question)

---

## Cardinality Rules

### Entity Instance Rules

| Entity | Minimum Instances | Maximum Instances | Typical Range |
|--------|-------------------|-------------------|---------------|
| User | 1 (system) | Unlimited | 1,000 - 100,000 |
| Survey | 0 | Unlimited per user | 10 - 100 per user |
| Question | 1 per survey | Unlimited per survey | 5 - 20 per survey |
| Response | 0 | Unlimited per survey | 10 - 10,000 per survey |
| Answer | 0 | Questions × Responses | = Question count per response |

### Relationship Cardinality Matrix

```
             │ User │ Survey │ Question │ Response │ Answer │
─────────────┼──────┼────────┼──────────┼──────────┼────────┤
User         │  -   │  1:N   │    -     │    -     │   -    │
Survey       │  N:1 │   -    │   1:N    │   1:N    │   -    │
Question     │  -   │  N:1   │    -     │    -     │  1:N   │
Response     │  -   │  N:1   │    -     │    -     │  1:N   │
Answer       │  -   │   -    │   N:1    │   N:1    │   -    │
```

---

## Referential Integrity

### Cascade Delete Chains

#### Scenario 1: Delete User
```
DELETE FROM users WHERE id = 123;

Cascade chain:
1. Delete all surveys where creator_id = 123
   ├─ 2a. Delete all questions for those surveys
   │     └─ 3a. Delete all answers to those questions
   └─ 2b. Delete all responses for those surveys
         └─ 3b. Delete all answers in those responses
```

**Impact**: Complete removal of user's content

#### Scenario 2: Delete Survey
```
DELETE FROM surveys WHERE id = 789;

Cascade chain:
1. Delete all questions where survey_id = 789
   └─ 2a. Delete all answers to those questions
2. Delete all responses where survey_id = 789
   └─ 2b. Delete all answers in those responses
```

**Impact**: Complete removal of survey and all response data

#### Scenario 3: Delete Question
```
DELETE FROM questions WHERE id = 555;

Cascade chain:
1. Delete all answers where question_id = 555
```

**Impact**: Removes answers across all responses
**Warning**: This breaks response integrity if response already submitted

#### Scenario 4: Delete Response
```
DELETE FROM responses WHERE id = 999;

Cascade chain:
1. Delete all answers where response_id = 999
```

**Impact**: Removes single user's response

### Orphan Prevention

The database schema prevents orphaned records through:

1. **NOT NULL constraints** on foreign keys
   - `surveys.creator_id` cannot be NULL
   - `questions.survey_id` cannot be NULL
   - `responses.survey_id` cannot be NULL
   - `answers.response_id` cannot be NULL
   - `answers.question_id` cannot be NULL

2. **CASCADE delete rules** ensure dependent data is removed

3. **No circular dependencies** in relationship graph

### Data Consistency Rules

1. **Answer Integrity**
   - Answer must reference existing response AND existing question
   - Question must belong to same survey as response's survey

2. **Response Completeness**
   - Complete response should have answers for all required questions
   - Enforced by application logic, not DB constraints

3. **Question Ordering**
   - `order_index` must be unique within survey
   - Application ensures consecutive ordering (0, 1, 2, ...)

---

## Query Patterns

### Pattern 1: Survey with Full Hierarchy
**Use Case**: Display survey to user

```sql
-- Get survey with questions
SELECT
    s.id, s.title, s.description,
    json_agg(
        json_build_object(
            'id', q.id,
            'text', q.question_text,
            'type', q.question_type,
            'order', q.order_index,
            'required', q.is_required,
            'options', q.options_json
        ) ORDER BY q.order_index
    ) as questions
FROM surveys s
LEFT JOIN questions q ON s.id = q.survey_id
WHERE s.id = 789
GROUP BY s.id, s.title, s.description;
```

### Pattern 2: Response with Answers
**Use Case**: Show completed response

```sql
-- Get response with answers
SELECT
    r.id, r.submitted_at,
    json_agg(
        json_build_object(
            'question_id', q.id,
            'question_text', q.question_text,
            'answer_text', a.answer_text,
            'answer_json', a.answer_json
        ) ORDER BY q.order_index
    ) as answers
FROM responses r
JOIN answers a ON r.id = a.response_id
JOIN questions q ON a.question_id = q.id
WHERE r.id = 999
GROUP BY r.id, r.submitted_at;
```

### Pattern 3: Survey Analytics
**Use Case**: Dashboard statistics

```sql
-- Comprehensive survey statistics
SELECT
    s.id,
    s.title,
    COUNT(DISTINCT q.id) as question_count,
    COUNT(DISTINCT r.id) as total_responses,
    COUNT(DISTINCT r.id) FILTER (WHERE r.is_complete = true) as completed_responses,
    COUNT(DISTINCT r.respondent_telegram_id) as unique_respondents,
    MIN(r.submitted_at) as first_response,
    MAX(r.submitted_at) as last_response
FROM surveys s
LEFT JOIN questions q ON s.id = q.survey_id
LEFT JOIN responses r ON s.id = r.survey_id
WHERE s.id = 789
GROUP BY s.id, s.title;
```

### Pattern 4: User's Survey List
**Use Case**: Show user's created surveys

```sql
-- Get user's surveys with stats
SELECT
    s.*,
    COUNT(DISTINCT q.id) as question_count,
    COUNT(DISTINCT r.id) FILTER (WHERE r.is_complete = true) as response_count
FROM surveys s
LEFT JOIN questions q ON s.id = q.survey_id
LEFT JOIN responses r ON s.id = r.survey_id
WHERE s.creator_id = 123
GROUP BY s.id
ORDER BY s.updated_at DESC;
```

### Pattern 5: Check User Response Status
**Use Case**: Prevent duplicate responses

```sql
-- Check if user can respond to survey
SELECT
    s.id,
    s.allow_multiple_responses,
    EXISTS(
        SELECT 1 FROM responses
        WHERE survey_id = s.id
        AND respondent_telegram_id = 123456789
        AND is_complete = true
    ) as has_completed_response
FROM surveys s
WHERE s.id = 789;
```

---

## Data Access Patterns

### Read Patterns

1. **Survey Display** (High frequency)
   - Read: surveys + questions (JOIN)
   - Index: `idx_questions_survey_order`

2. **Response Submission** (High frequency)
   - Read: questions for validation
   - Write: responses + answers (TRANSACTION)
   - Index: `idx_questions_survey_id`

3. **Analytics** (Medium frequency)
   - Read: questions + answers (JOIN)
   - Aggregation: COUNT, AVG, etc.
   - Index: `idx_answers_question_id`, `idx_answers_answer_json` (GIN)

4. **User Dashboard** (High frequency)
   - Read: surveys by creator
   - Stats: COUNT responses
   - Index: `idx_surveys_creator_active`

### Write Patterns

1. **Create Survey**
```sql
BEGIN;
INSERT INTO surveys (...) VALUES (...) RETURNING id;
INSERT INTO questions (survey_id, ...) VALUES (?, ...), (?, ...), ...;
COMMIT;
```

2. **Submit Response**
```sql
BEGIN;
INSERT INTO responses (survey_id, respondent_telegram_id, ...)
VALUES (?, ?, ...) RETURNING id;
INSERT INTO answers (response_id, question_id, answer_text, ...)
VALUES (?, ?, ?), (?, ?, ?), ...;
UPDATE responses SET is_complete = true, submitted_at = NOW()
WHERE id = ?;
COMMIT;
```

3. **Update Survey**
```sql
BEGIN;
UPDATE surveys SET title = ?, description = ?, updated_at = NOW()
WHERE id = ? AND creator_id = ?;
-- Optionally update questions
COMMIT;
```

### Transaction Boundaries

| Operation | Transaction Required | Entities Involved |
|-----------|---------------------|-------------------|
| Create Survey | Yes | surveys + questions |
| Submit Response | Yes | responses + answers |
| Update Survey | Yes | surveys (+ questions) |
| View Survey | No | surveys + questions |
| Analytics | No | All tables (read-only) |
| Delete Survey | Yes (auto) | surveys → cascade |

---

## Performance Considerations

### Index Usage by Relationship

| Relationship | Primary Index | Secondary Index | Query Pattern |
|--------------|---------------|-----------------|---------------|
| R1 (User→Survey) | idx_surveys_creator_id | idx_surveys_creator_active | User's active surveys |
| R2 (Survey→Question) | idx_questions_survey_order | idx_questions_survey_order_unique | Ordered retrieval |
| R3 (Survey→Response) | idx_responses_survey_id | idx_responses_survey_respondent | Response stats |
| R4 (Response→Answer) | idx_answers_response_id | idx_answers_response_question_unique | Answer lookup |
| R5 (Question→Answer) | idx_answers_question_id | idx_answers_answer_json (GIN) | Analytics |

### Join Optimization

**Efficient Join Order** for complex queries:
1. Start with most selective filter (usually survey_id or response_id)
2. Join questions early (usually small result set)
3. Join answers last (largest table)

**Example**:
```sql
-- Efficient: Filter survey first, then join
SELECT ...
FROM surveys s
JOIN questions q ON s.id = q.survey_id  -- Small result set
JOIN answers a ON q.id = a.question_id  -- Filtered by question
WHERE s.id = 789;  -- Most selective filter
```

---

## Common Pitfalls and Solutions

### Pitfall 1: N+1 Query Problem
**Problem**: Loading survey, then each question separately
```sql
-- Bad: N+1 queries
SELECT * FROM surveys WHERE id = 789;
-- Then for each survey:
SELECT * FROM questions WHERE survey_id = 789;
```

**Solution**: Use JOIN or subquery
```sql
-- Good: Single query with aggregation
SELECT s.*, json_agg(q.*) as questions
FROM surveys s
LEFT JOIN questions q ON s.id = q.survey_id
WHERE s.id = 789
GROUP BY s.id;
```

### Pitfall 2: Missing Transaction for Response Submission
**Problem**: Partial response if answer insert fails

**Solution**: Wrap in transaction
```sql
BEGIN;
-- Insert response
-- Insert all answers
-- Update response as complete
COMMIT;
```

### Pitfall 3: Cascade Delete Accidents
**Problem**: Deleting survey accidentally removes all responses

**Solution**: Implement soft delete for surveys
```sql
-- Add deleted_at column
ALTER TABLE surveys ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE;

-- Soft delete instead of hard delete
UPDATE surveys SET deleted_at = NOW(), is_active = false
WHERE id = 789;

-- Filter deleted surveys in queries
SELECT * FROM surveys WHERE deleted_at IS NULL;
```

### Pitfall 4: Duplicate Response Checking
**Problem**: Race condition allows duplicate responses

**Solution**: Use unique constraint or serializable transaction
```sql
-- Option 1: Unique constraint (if allow_multiple_responses = false globally)
CREATE UNIQUE INDEX idx_responses_survey_respondent_unique
ON responses(survey_id, respondent_telegram_id)
WHERE is_complete = true;

-- Option 2: Serializable transaction with check
BEGIN ISOLATION LEVEL SERIALIZABLE;
SELECT ... FROM responses WHERE ... FOR UPDATE;
-- Check and insert if not exists
COMMIT;
```

---

## Summary Checklist

### Relationship Integrity
- [ ] All foreign keys defined with appropriate constraints
- [ ] Cascade rules match business requirements
- [ ] Indexes support foreign key lookups
- [ ] Unique constraints prevent duplicates

### Query Performance
- [ ] Composite indexes for common join patterns
- [ ] Partial indexes for filtered queries
- [ ] GIN indexes for JSONB searches
- [ ] Covering indexes considered for hot queries

### Data Consistency
- [ ] Transactions wrap multi-table operations
- [ ] Check constraints validate data
- [ ] Application logic enforces business rules
- [ ] Audit trails for important operations

### Maintenance
- [ ] Relationship documentation up to date
- [ ] Query patterns documented
- [ ] Performance baselines established
- [ ] Migration strategy defined

---

**Document Status**: Complete and reviewed
**Next Steps**: Implement Entity Framework Core models matching this schema
