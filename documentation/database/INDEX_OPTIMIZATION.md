# Database Index Optimization Guide
## Telegram Survey Bot MVP

### Version: 1.0.0
### Last Updated: 2025-11-05

---

## Table of Contents
1. [Overview](#overview)
2. [Index Strategy](#index-strategy)
3. [Table-by-Table Index Analysis](#table-by-table-index-analysis)
4. [Query Performance Targets](#query-performance-targets)
5. [Index Maintenance](#index-maintenance)
6. [Monitoring and Optimization](#monitoring-and-optimization)

---

## Overview

### Purpose
This document defines the indexing strategy for optimal query performance in the Telegram Survey Bot MVP. The strategy balances read performance, write performance, and storage efficiency.

### Key Principles
1. **Index for common queries** - Cover 80% of query patterns with 20% of indexes
2. **Measure before optimizing** - Use EXPLAIN ANALYZE to verify improvements
3. **Consider write cost** - Every index slows down INSERT/UPDATE/DELETE
4. **Partial indexes for filters** - Index only relevant subset of data
5. **Composite indexes for joins** - Optimize multi-column query patterns

### Index Types Used
| Type | Use Case | Tables |
|------|----------|--------|
| B-tree | Standard lookups, sorting | All tables |
| Unique | Enforce uniqueness | users.telegram_id, questions.survey_order |
| Partial | Filter specific rows | surveys.is_active, responses.is_complete |
| Composite | Multi-column queries | Multiple tables |
| GIN (JSONB) | JSON searches | questions.options_json, answers.answer_json |

---

## Index Strategy

### Primary Access Patterns

#### 1. User Operations
**Pattern**: Lookup by telegram_id (most common)
```sql
SELECT * FROM users WHERE telegram_id = 123456789;
```
**Index**: `idx_users_telegram_id` (B-tree, unique)
**Cardinality**: Very high (one row per query)
**Frequency**: Every bot interaction
**Priority**: CRITICAL

#### 2. Survey Management
**Pattern**: Creator's active surveys
```sql
SELECT * FROM surveys
WHERE creator_id = 123 AND is_active = true
ORDER BY created_at DESC;
```
**Index**: `idx_surveys_creator_active` (Composite)
**Cardinality**: Medium (10-100 surveys per user)
**Frequency**: High (dashboard view)
**Priority**: HIGH

#### 3. Survey Display
**Pattern**: Ordered questions for survey
```sql
SELECT * FROM questions
WHERE survey_id = 789
ORDER BY order_index ASC;
```
**Index**: `idx_questions_survey_order` (Composite)
**Cardinality**: Low (5-20 questions per survey)
**Frequency**: Very high (every survey view)
**Priority**: CRITICAL

#### 4. Response Submission
**Pattern**: Insert response and answers in transaction
```sql
INSERT INTO responses (survey_id, respondent_telegram_id, ...)
VALUES (789, 123456789, ...);
-- Validate no duplicates
SELECT * FROM responses
WHERE survey_id = 789 AND respondent_telegram_id = 123456789
AND is_complete = true;
```
**Index**: `idx_responses_survey_respondent` (Composite)
**Cardinality**: Low (0-N per user per survey)
**Frequency**: High
**Priority**: HIGH

#### 5. Survey Analytics
**Pattern**: Aggregate answers by question
```sql
SELECT question_id, answer_text, COUNT(*)
FROM answers
WHERE question_id = 777
GROUP BY question_id, answer_text;
```
**Index**: `idx_answers_question_id` (B-tree)
**Cardinality**: High (many answers per question)
**Frequency**: Medium (analytics view)
**Priority**: MEDIUM

---

## Table-by-Table Index Analysis

### Table: USERS

**Table Size**: Thousands to tens of thousands of rows
**Growth Rate**: Linear with bot adoption
**Write Frequency**: Low (new user registration only)
**Read Frequency**: Very high (every interaction)

#### Indexes

##### 1. Primary Key
```sql
CREATE INDEX users_pkey ON users(id);
```
- **Type**: B-tree (automatic with PRIMARY KEY)
- **Purpose**: Internal referencing
- **Usage**: Foreign key joins
- **Selectivity**: Perfect (unique)

##### 2. Telegram ID Lookup
```sql
CREATE INDEX idx_users_telegram_id ON users(telegram_id);
```
- **Type**: B-tree, Unique
- **Purpose**: Primary user identification
- **Usage**: `SELECT * FROM users WHERE telegram_id = ?`
- **Selectivity**: Perfect (unique)
- **Size Impact**: ~8 bytes per row + overhead
- **Write Cost**: Minimal (inserts only)
- **Priority**: CRITICAL

**Query Performance**:
```sql
EXPLAIN ANALYZE
SELECT * FROM users WHERE telegram_id = 123456789;

-- Expected plan:
-- Index Scan using idx_users_telegram_id (cost=0.42..8.44 rows=1)
-- Execution time: < 1ms
```

##### 3. Username Search
```sql
CREATE INDEX idx_users_username
ON users(username)
WHERE username IS NOT NULL;
```
- **Type**: B-tree, Partial
- **Purpose**: Optional username lookup
- **Usage**: Find user by @username
- **Selectivity**: High (unique usernames)
- **Size Impact**: ~100 bytes per row (only non-null)
- **Write Cost**: Low (most users have username)
- **Priority**: LOW (MVP may not need this)

**Considerations**:
- Partial index saves space (some users lack username)
- Not critical for MVP (telegram_id is primary identifier)
- Can be added later if username search needed

#### Write Impact
- New user: Update 2 indexes (pkey, telegram_id)
- Cost: Negligible (< 1ms)
- Benefit: Enables O(log n) lookups vs O(n) table scan

---

### Table: SURVEYS

**Table Size**: Hundreds per active user
**Growth Rate**: Steady with user activity
**Write Frequency**: Medium (create, update, deactivate)
**Read Frequency**: High (list, display)

#### Indexes

##### 1. Primary Key
```sql
CREATE INDEX surveys_pkey ON surveys(id);
```
- **Type**: B-tree (automatic)
- **Purpose**: Internal referencing
- **Usage**: Foreign key joins

##### 2. Creator Lookup
```sql
CREATE INDEX idx_surveys_creator_id ON surveys(creator_id);
```
- **Type**: B-tree
- **Purpose**: List user's surveys
- **Usage**: `SELECT * FROM surveys WHERE creator_id = ?`
- **Selectivity**: Medium (10-100 surveys per user)
- **Priority**: HIGH

##### 3. Active Survey Filter (Partial)
```sql
CREATE INDEX idx_surveys_is_active
ON surveys(is_active)
WHERE is_active = true;
```
- **Type**: B-tree, Partial
- **Purpose**: List only active surveys
- **Usage**: `SELECT * FROM surveys WHERE is_active = true`
- **Selectivity**: Medium (50-80% of surveys active)
- **Size Impact**: Small (only indexes true rows)
- **Priority**: MEDIUM

**Why Partial Index?**:
- Most queries only want active surveys
- Inactive surveys rarely queried
- Saves 20-50% index size
- Faster index maintenance

##### 4. Creator + Active (Composite)
```sql
CREATE INDEX idx_surveys_creator_active
ON surveys(creator_id, is_active);
```
- **Type**: B-tree, Composite
- **Purpose**: User's active surveys (common pattern)
- **Usage**: `WHERE creator_id = ? AND is_active = true`
- **Selectivity**: High (few results per user)
- **Priority**: HIGH

**Index Column Order**:
- `creator_id` first: Most selective filter
- `is_active` second: Further filters results
- Can also serve queries with only `creator_id` (leftmost prefix)

**Query Performance**:
```sql
EXPLAIN ANALYZE
SELECT * FROM surveys
WHERE creator_id = 123 AND is_active = true
ORDER BY created_at DESC;

-- Expected plan:
-- Index Scan using idx_surveys_creator_active
-- Sort by created_at (may need separate index if slow)
-- Execution time: < 10ms
```

##### 5. Creation Date Sorting
```sql
CREATE INDEX idx_surveys_created_at ON surveys(created_at DESC);
```
- **Type**: B-tree, Descending
- **Purpose**: Recent surveys first
- **Usage**: `ORDER BY created_at DESC`
- **Priority**: LOW (can sort in application)

**Optimization Note**:
- DESC index matches typical query pattern
- Avoid sorting penalty for large result sets
- Consider adding if result set > 100 rows

#### Write Impact
- New survey: Update 5 indexes
- Update survey: Update 2-3 indexes (depends on changed columns)
- Cost: ~2-5ms per operation
- Benefit: Sub-10ms query times vs 100ms+ table scans

#### Index Selection by Query

| Query Pattern | Best Index | Fallback Index |
|---------------|------------|----------------|
| `WHERE creator_id = ?` | idx_surveys_creator_id | idx_surveys_creator_active |
| `WHERE is_active = true` | idx_surveys_is_active | idx_surveys_creator_active |
| `WHERE creator_id = ? AND is_active = ?` | idx_surveys_creator_active | idx_surveys_creator_id |
| `ORDER BY created_at DESC` | idx_surveys_created_at | Table scan + sort |

---

### Table: QUESTIONS

**Table Size**: 5-20 rows per survey
**Growth Rate**: Proportional to survey creation
**Write Frequency**: Low (survey creation/editing)
**Read Frequency**: Very high (every survey display)

#### Indexes

##### 1. Primary Key
```sql
CREATE INDEX questions_pkey ON questions(id);
```

##### 2. Survey Lookup
```sql
CREATE INDEX idx_questions_survey_id ON questions(survey_id);
```
- **Type**: B-tree
- **Purpose**: Get all questions for survey
- **Usage**: `SELECT * FROM questions WHERE survey_id = ?`
- **Selectivity**: Low (5-20 rows)
- **Priority**: HIGH

##### 3. Survey + Order (Composite)
```sql
CREATE INDEX idx_questions_survey_order
ON questions(survey_id, order_index);
```
- **Type**: B-tree, Composite
- **Purpose**: Ordered question retrieval
- **Usage**: `WHERE survey_id = ? ORDER BY order_index`
- **Selectivity**: Perfect (unique combination)
- **Priority**: CRITICAL

**Why This Index is Critical**:
- Every survey display needs ordered questions
- Eliminates sort operation
- Index-only scan possible (covering index)
- Small size (few rows per survey)

**Query Performance**:
```sql
EXPLAIN ANALYZE
SELECT * FROM questions
WHERE survey_id = 789
ORDER BY order_index ASC;

-- Expected plan:
-- Index Scan using idx_questions_survey_order
-- No additional sort needed
-- Execution time: < 5ms
```

##### 4. Survey + Order (Unique Constraint)
```sql
CREATE UNIQUE INDEX idx_questions_survey_order_unique
ON questions(survey_id, order_index);
```
- **Type**: B-tree, Unique, Composite
- **Purpose**: Enforce ordering uniqueness
- **Usage**: Data integrity
- **Benefit**: Prevents bugs, enables assumptions

**Note**: This duplicates idx_questions_survey_order but adds uniqueness constraint. Consider if both are needed or make idx_questions_survey_order UNIQUE instead.

##### 5. Question Type Filter
```sql
CREATE INDEX idx_questions_type ON questions(question_type);
```
- **Type**: B-tree
- **Purpose**: Filter by question type (analytics)
- **Usage**: `SELECT * FROM questions WHERE question_type = 'rating'`
- **Priority**: LOW (MVP unlikely to need this)

##### 6. Options JSON Search (GIN)
```sql
CREATE INDEX idx_questions_options_json
ON questions USING GIN (options_json);
```
- **Type**: GIN (Generalized Inverted Index)
- **Purpose**: Search within JSONB options
- **Usage**: `WHERE options_json @> '{"allow_other": true}'`
- **Size Impact**: Larger than B-tree (~3x data size)
- **Priority**: LOW (MVP unlikely to search JSON)

**JSONB Query Examples**:
```sql
-- Find questions with specific option
SELECT * FROM questions
WHERE options_json @> '{"options": ["Option A"]}';

-- Find questions with key
SELECT * FROM questions
WHERE options_json ? 'min_rating';

-- Find questions with numeric range
SELECT * FROM questions
WHERE (options_json->>'max_rating')::int > 5;
```

**Recommendation**: Defer GIN index until analytics features needed.

#### Write Impact
- New question: Update 3-4 indexes
- Reorder questions: Update order_index indexes for multiple rows
- Cost: ~1-3ms per question
- Reorder cost: ~5-10ms for batch update

---

### Table: RESPONSES

**Table Size**: Hundreds to thousands per survey
**Growth Rate**: Rapid with survey distribution
**Write Frequency**: High (new responses, completion updates)
**Read Frequency**: Medium (analytics, user checks)

#### Indexes

##### 1. Primary Key
```sql
CREATE INDEX responses_pkey ON responses(id);
```

##### 2. Survey Lookup
```sql
CREATE INDEX idx_responses_survey_id ON responses(survey_id);
```
- **Type**: B-tree
- **Purpose**: Get all responses for survey
- **Usage**: `SELECT * FROM responses WHERE survey_id = ?`
- **Selectivity**: High (many responses)
- **Priority**: HIGH

##### 3. Respondent Lookup
```sql
CREATE INDEX idx_responses_respondent
ON responses(respondent_telegram_id);
```
- **Type**: B-tree
- **Purpose**: Get user's responses across surveys
- **Usage**: `WHERE respondent_telegram_id = ?`
- **Priority**: MEDIUM

##### 4. Survey + Respondent (Composite)
```sql
CREATE INDEX idx_responses_survey_respondent
ON responses(survey_id, respondent_telegram_id);
```
- **Type**: B-tree, Composite
- **Purpose**: Check if user responded to survey
- **Usage**: `WHERE survey_id = ? AND respondent_telegram_id = ?`
- **Selectivity**: Very high (0-few rows)
- **Priority**: CRITICAL

**Critical Use Case - Duplicate Prevention**:
```sql
-- Check before allowing response
SELECT * FROM responses
WHERE survey_id = 789
AND respondent_telegram_id = 123456789
AND is_complete = true;

-- Uses: idx_responses_survey_respondent
-- Execution time: < 5ms
```

##### 5. Complete Responses (Partial)
```sql
CREATE INDEX idx_responses_complete
ON responses(is_complete)
WHERE is_complete = true;
```
- **Type**: B-tree, Partial
- **Purpose**: Count completed responses only
- **Usage**: Analytics queries
- **Size Impact**: Small (only completed responses)
- **Priority**: MEDIUM

**Why Partial Index?**:
- Most analytics queries want completed responses only
- Incomplete responses are transient
- Saves space and maintenance for incomplete responses

##### 6. Submission Date Sorting (Partial)
```sql
CREATE INDEX idx_responses_submitted_at
ON responses(submitted_at DESC)
WHERE submitted_at IS NOT NULL;
```
- **Type**: B-tree, Partial, Descending
- **Purpose**: Recent responses first
- **Usage**: `ORDER BY submitted_at DESC` in analytics
- **Priority**: LOW (small result sets)

#### Write Impact
- New response: Update 4 indexes (~3ms)
- Complete response: Update 2 additional indexes (~2ms)
- Total insert + complete: ~5-8ms
- Benefit: Fast duplicate checking, analytics

---

### Table: ANSWERS

**Table Size**: Questions × Responses (largest table)
**Growth Rate**: Fastest growing table
**Write Frequency**: Very high (bulk inserts with responses)
**Read Frequency**: High (response display, analytics)

#### Indexes

##### 1. Primary Key
```sql
CREATE INDEX answers_pkey ON answers(id);
```

##### 2. Response Lookup
```sql
CREATE INDEX idx_answers_response_id ON answers(response_id);
```
- **Type**: B-tree
- **Purpose**: Get all answers for response
- **Usage**: `SELECT * FROM answers WHERE response_id = ?`
- **Selectivity**: Low (5-20 answers per response)
- **Priority**: CRITICAL

**Query Performance**:
```sql
EXPLAIN ANALYZE
SELECT a.*, q.question_text
FROM answers a
JOIN questions q ON a.question_id = q.id
WHERE a.response_id = 999;

-- Expected plan:
-- Index Scan using idx_answers_response_id
-- Nested Loop Join to questions
-- Execution time: < 10ms
```

##### 3. Question Lookup (Analytics)
```sql
CREATE INDEX idx_answers_question_id ON answers(question_id);
```
- **Type**: B-tree
- **Purpose**: Get all answers for question
- **Usage**: Analytics, aggregation
- **Selectivity**: High (hundreds of answers)
- **Priority**: HIGH

**Query Performance**:
```sql
EXPLAIN ANALYZE
SELECT answer_text, COUNT(*) as frequency
FROM answers
WHERE question_id = 777
GROUP BY answer_text;

-- Expected plan:
-- Index Scan using idx_answers_question_id
-- HashAggregate
-- Execution time: < 50ms (depends on answer count)
```

##### 4. Response + Question (Composite, Unique)
```sql
CREATE UNIQUE INDEX idx_answers_response_question_unique
ON answers(response_id, question_id);
```
- **Type**: B-tree, Composite, Unique
- **Purpose**: Prevent duplicate answers, fast lookup
- **Usage**: Integrity + `WHERE response_id = ? AND question_id = ?`
- **Selectivity**: Perfect (unique combination)
- **Priority**: HIGH

**Benefits**:
1. Data integrity (one answer per question)
2. Fast specific answer lookup
3. Can replace idx_answers_response_id for some queries

**Query Optimization**:
```sql
-- This query uses the composite index
SELECT * FROM answers
WHERE response_id = 999 AND question_id = 777;

-- This query also benefits (leftmost prefix)
SELECT * FROM answers WHERE response_id = 999;
```

##### 5. Answer JSON Search (GIN)
```sql
CREATE INDEX idx_answers_answer_json
ON answers USING GIN (answer_json);
```
- **Type**: GIN
- **Purpose**: Search/aggregate complex answers
- **Usage**: Analytics on multiple choice, ratings
- **Size Impact**: Large (~3x of JSON data)
- **Priority**: MEDIUM (needed for analytics)

**Analytics Use Cases**:
```sql
-- Multiple choice option frequency
SELECT jsonb_array_elements_text(answer_json) as option,
       COUNT(*) as count
FROM answers
WHERE question_id = 777
GROUP BY option;

-- Rating distribution
SELECT (answer_json->>'rating')::int as rating,
       COUNT(*) as count
FROM answers
WHERE question_id = 888
GROUP BY rating;

-- Complex filters
SELECT * FROM answers
WHERE answer_json @> '{"rating": 5}';
```

**GIN Index Trade-offs**:
- **Pros**: Fast JSONB searches, enables analytics
- **Cons**: Larger size, slower writes
- **Recommendation**: Essential for MVP analytics features

#### Write Impact
- New answer: Update 4-5 indexes (~5ms)
- Bulk insert (10 answers): ~50ms total
- GIN index: Adds ~30% to write time
- Benefit: Sub-100ms analytics queries

#### Optimization Strategy
Given that answers is the largest table:
1. Keep only essential indexes
2. Consider partitioning if grows beyond 10M rows
3. Monitor GIN index size and rebuild if fragmented
4. Use batch inserts (10-20 answers) in single transaction

---

## Query Performance Targets

### Performance Benchmarks

| Query Type | Target Time | Index Used | Notes |
|------------|-------------|------------|-------|
| User lookup by telegram_id | < 1ms | idx_users_telegram_id | Single row |
| List user's surveys | < 10ms | idx_surveys_creator_active | 10-100 rows |
| Get survey questions | < 5ms | idx_questions_survey_order | 5-20 rows |
| Check response exists | < 5ms | idx_responses_survey_respondent | 0-1 rows |
| Get response answers | < 10ms | idx_answers_response_id | 5-20 rows |
| Question analytics | < 100ms | idx_answers_question_id + GIN | 100-1000 rows |
| Survey statistics | < 200ms | Multiple indexes + aggregation | Complex query |

### Query Complexity Levels

#### Level 1: Simple Lookups (< 10ms)
- Single table queries
- Primary key or unique index access
- Small result sets (< 100 rows)

**Example**:
```sql
SELECT * FROM users WHERE telegram_id = 123456789;
SELECT * FROM questions WHERE survey_id = 789 ORDER BY order_index;
```

#### Level 2: Simple Joins (< 50ms)
- 2-3 table joins
- Indexed foreign keys
- Medium result sets (< 1000 rows)

**Example**:
```sql
SELECT a.*, q.question_text
FROM answers a
JOIN questions q ON a.question_id = q.id
WHERE a.response_id = 999;
```

#### Level 3: Aggregation (< 200ms)
- Multi-table joins with aggregation
- GROUP BY, COUNT, AVG
- Large result sets processed (< 10000 rows)

**Example**:
```sql
SELECT q.id, q.question_text, COUNT(a.id) as answer_count
FROM questions q
LEFT JOIN answers a ON q.id = a.question_id
WHERE q.survey_id = 789
GROUP BY q.id;
```

#### Level 4: Complex Analytics (< 1s)
- Multi-table joins with JSONB processing
- Multiple aggregations
- Full table scans on filtered data

**Example**:
```sql
SELECT
    q.question_text,
    jsonb_array_elements_text(a.answer_json) as option,
    COUNT(*) as frequency
FROM questions q
JOIN answers a ON q.id = a.question_id
WHERE q.survey_id = 789 AND q.question_type = 'multiple_choice'
GROUP BY q.id, q.question_text, option;
```

---

## Index Maintenance

### Index Health Monitoring

#### 1. Check Index Usage
```sql
-- Find unused indexes
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE idx_scan = 0
AND indexname NOT LIKE '%pkey'
ORDER BY pg_relation_size(indexrelid) DESC;
```

**Action**: Drop indexes with 0 scans after sufficient observation period.

#### 2. Check Index Bloat
```sql
-- Estimate index bloat
SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    ROUND(100 * pg_relation_size(indexrelid) / pg_relation_size(indrelid), 2) as index_ratio
FROM pg_stat_user_indexes
ORDER BY pg_relation_size(indexrelid) DESC;
```

**Action**: REINDEX if index_ratio > 50% for B-tree or bloat detected.

#### 3. Check Missing Indexes
```sql
-- Identify sequential scans on large tables
SELECT
    schemaname,
    tablename,
    seq_scan,
    seq_tup_read,
    idx_scan,
    seq_tup_read / NULLIF(seq_scan, 0) as avg_seq_read
FROM pg_stat_user_tables
WHERE seq_scan > 100
AND seq_tup_read / NULLIF(seq_scan, 0) > 1000
ORDER BY seq_tup_read DESC;
```

**Action**: Investigate queries causing sequential scans, add indexes if beneficial.

### Index Rebuild Schedule

#### When to Rebuild
- After bulk data loads (> 10% table size)
- If query performance degrades
- After many UPDATE/DELETE operations
- GIN indexes after significant JSON updates

#### Rebuild Commands
```sql
-- Rebuild single index (locks table)
REINDEX INDEX idx_answers_answer_json;

-- Rebuild concurrently (no lock, slower)
REINDEX INDEX CONCURRENTLY idx_answers_answer_json;

-- Rebuild all indexes for table
REINDEX TABLE answers;

-- Rebuild all indexes in database (offline operation)
REINDEX DATABASE surveybot;
```

**Best Practice**: Use CONCURRENTLY in production to avoid locking.

### Index Statistics Update

```sql
-- Update statistics for query planner
ANALYZE users;
ANALYZE surveys;
ANALYZE questions;
ANALYZE responses;
ANALYZE answers;

-- Or all tables
ANALYZE;
```

**Schedule**: Run ANALYZE after significant data changes (> 5% of rows).

---

## Monitoring and Optimization

### Performance Monitoring Queries

#### 1. Slowest Queries
```sql
-- Requires pg_stat_statements extension
SELECT
    query,
    calls,
    mean_exec_time,
    max_exec_time,
    total_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 20;
```

#### 2. Most Expensive Queries (Total Time)
```sql
SELECT
    query,
    calls,
    mean_exec_time,
    total_exec_time
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 20;
```

#### 3. Cache Hit Ratio
```sql
SELECT
    sum(heap_blks_read) as heap_read,
    sum(heap_blks_hit) as heap_hit,
    sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100 as cache_hit_ratio
FROM pg_statio_user_tables;
```

**Target**: Cache hit ratio > 99% for optimal performance.

### Query Optimization Process

#### Step 1: Identify Slow Query
Use monitoring tools or application logging to find queries > target time.

#### Step 2: Analyze Query Plan
```sql
EXPLAIN ANALYZE
SELECT * FROM surveys s
JOIN questions q ON s.id = q.survey_id
WHERE s.creator_id = 123;
```

**Look for**:
- Seq Scan (should be Index Scan)
- High cost values
- Sort operations (may need index)
- Nested Loop vs Hash Join (depends on data size)

#### Step 3: Identify Missing/Unused Indexes
- Check if indexes exist for WHERE/JOIN conditions
- Verify index is being used (not skipped by planner)
- Check index selectivity

#### Step 4: Test Index Addition
```sql
-- Create index
CREATE INDEX idx_test ON surveys(creator_id);

-- Rerun EXPLAIN ANALYZE
EXPLAIN ANALYZE [query];

-- Compare execution times
```

#### Step 5: Evaluate Trade-offs
- Write performance impact
- Storage space
- Maintenance overhead
- Actual performance improvement

#### Step 6: Apply or Drop
```sql
-- If improvement is significant (> 50%)
-- Keep the index

-- If improvement is minimal (< 20%)
DROP INDEX idx_test;
```

### Optimization Examples

#### Example 1: Adding Composite Index

**Problem**: Slow user dashboard query
```sql
-- Before: 150ms
EXPLAIN ANALYZE
SELECT * FROM surveys
WHERE creator_id = 123 AND is_active = true;

-- Seq Scan on surveys (cost=0.00..25.00 rows=5)
-- Filter: (creator_id = 123 AND is_active = true)
```

**Solution**: Add composite index
```sql
CREATE INDEX idx_surveys_creator_active
ON surveys(creator_id, is_active);
```

**Result**: 8ms
```sql
-- Index Scan using idx_surveys_creator_active (cost=0.42..8.44 rows=5)
-- Index Cond: (creator_id = 123 AND is_active = true)
```

**Improvement**: 94% faster

#### Example 2: Partial Index for Filtered Queries

**Problem**: Slow active survey listing
```sql
-- Before: 80ms
SELECT * FROM surveys WHERE is_active = true;

-- Seq Scan on surveys (cost=0.00..20.00 rows=500)
-- Filter: is_active = true
```

**Solution**: Partial index on active only
```sql
CREATE INDEX idx_surveys_is_active
ON surveys(is_active)
WHERE is_active = true;
```

**Result**: 12ms
```sql
-- Index Scan using idx_surveys_is_active (cost=0.42..12.44 rows=500)
```

**Benefits**:
- 85% faster
- Smaller index size (only active surveys)
- Lower maintenance cost

#### Example 3: GIN Index for JSONB

**Problem**: Slow analytics on multiple choice answers
```sql
-- Before: 800ms
SELECT jsonb_array_elements_text(answer_json) as option,
       COUNT(*) as count
FROM answers
WHERE question_id = 777
GROUP BY option;

-- Seq Scan on answers (cost=0.00..500.00 rows=1000)
-- Filter: (question_id = 777)
-- Planning time: 2ms, Execution time: 798ms
```

**Solution**: Add GIN index
```sql
CREATE INDEX idx_answers_answer_json
ON answers USING GIN (answer_json);
```

**Result**: 120ms
```sql
-- Bitmap Heap Scan on answers (cost=20.00..150.00 rows=1000)
-- Recheck Cond: (question_id = 777)
-- Bitmap Index Scan on idx_answers_question_id
-- Planning time: 1ms, Execution time: 118ms
```

**Improvement**: 85% faster
**Note**: Most time now spent in JSONB processing, not data retrieval.

---

## Index Recommendations Summary

### Must-Have Indexes (CRITICAL)
These indexes are essential for MVP functionality:

1. `idx_users_telegram_id` - User identification
2. `idx_surveys_creator_active` - User dashboard
3. `idx_questions_survey_order` - Survey display
4. `idx_responses_survey_respondent` - Duplicate checking
5. `idx_answers_response_id` - Response display
6. `idx_answers_question_id` - Analytics

### Should-Have Indexes (HIGH)
These indexes significantly improve performance:

7. `idx_surveys_creator_id` - Survey management
8. `idx_responses_survey_id` - Survey analytics
9. `idx_answers_response_question_unique` - Data integrity + lookup

### Nice-to-Have Indexes (MEDIUM)
These indexes help specific features:

10. `idx_surveys_is_active` - Active survey filtering (partial)
11. `idx_responses_complete` - Completed responses (partial)
12. `idx_answers_answer_json` - JSONB analytics (GIN)

### Optional Indexes (LOW)
These indexes can be deferred or skipped:

13. `idx_users_username` - Username search (rarely used)
14. `idx_questions_type` - Question type filtering
15. `idx_questions_options_json` - JSONB search (rarely needed)
16. `idx_surveys_created_at` - Date sorting (small result sets)
17. `idx_responses_submitted_at` - Date sorting (partial)

### Total Index Count: 17 indexes
- Critical: 6
- High: 3
- Medium: 3
- Low: 5

### Storage Overhead
Estimated index storage (for 100k responses):
- B-tree indexes: ~50 MB
- GIN indexes: ~30 MB
- Total: ~80 MB (vs ~200 MB data)
- Ratio: 40% overhead (acceptable)

### Write Performance Impact
- Users: Negligible (rare inserts)
- Surveys: Low (~3ms overhead)
- Questions: Low (~2ms overhead)
- Responses: Medium (~5ms overhead)
- Answers: Higher (~5ms per answer, batch inserts recommended)

---

## Checklist for Production

### Pre-Launch
- [ ] All CRITICAL indexes created
- [ ] Query performance targets met
- [ ] EXPLAIN ANALYZE verified for key queries
- [ ] Index sizes monitored
- [ ] pg_stat_statements enabled

### Post-Launch
- [ ] Monitor slow query log
- [ ] Check index usage statistics weekly
- [ ] ANALYZE tables after bulk operations
- [ ] Review and drop unused indexes monthly
- [ ] Plan for REINDEX during maintenance windows

### Scaling Considerations
- [ ] Consider table partitioning if answers > 10M rows
- [ ] Evaluate materialized views for complex analytics
- [ ] Monitor GIN index bloat
- [ ] Consider separate analytics database if read load high
- [ ] Plan for connection pooling (PgBouncer)

---

**Document Status**: Complete and reviewed
**Next Steps**:
1. Implement indexes in schema.sql
2. Verify with EXPLAIN ANALYZE
3. Monitor post-deployment
4. Iterate based on actual usage patterns
