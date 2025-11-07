# Database Documentation
## Telegram Survey Bot MVP

### Version: 1.0.0
### Last Updated: 2025-11-05

---

## Overview

This directory contains complete database design documentation for the Telegram Survey Bot MVP. The database uses **PostgreSQL 14+** with **Entity Framework Core** as the ORM.

---

## Documentation Files

### 1. [schema.sql](./schema.sql)
**Complete SQL schema definition**
- All table definitions with columns and data types
- Foreign key constraints and relationships
- Check constraints for data validation
- Indexes for performance optimization
- Triggers for automatic timestamp updates
- Useful views for common queries
- Inline comments and documentation

**Use this file to**:
- Create the database from scratch
- Understand complete database structure
- Reference SQL syntax for constraints
- Review index definitions

---

### 2. [ER_DIAGRAM.md](./ER_DIAGRAM.md)
**Entity-Relationship diagram and comprehensive entity documentation**
- Visual ASCII ER diagram
- Detailed entity descriptions
- Relationship explanations with cardinality
- Business rules and constraints
- Data types rationale
- Schema evolution guidelines

**Use this file to**:
- Understand database structure visually
- Learn entity relationships and cardinality
- Review business rules
- Plan schema changes
- Onboard new developers

---

### 3. [RELATIONSHIPS.md](./RELATIONSHIPS.md)
**Detailed relationship mapping and query patterns**
- All five relationships documented in detail
- Cardinality rules and constraints
- Referential integrity and cascade behavior
- Common query patterns with examples
- Data access patterns
- Performance considerations
- Common pitfalls and solutions

**Use this file to**:
- Write efficient queries
- Understand cascade deletes
- Learn optimal join patterns
- Avoid common mistakes
- Optimize data access

---

### 4. [INDEX_OPTIMIZATION.md](./INDEX_OPTIMIZATION.md)
**Comprehensive index strategy and performance tuning**
- Index strategy and principles
- Table-by-table index analysis
- Query performance targets
- Index maintenance procedures
- Monitoring and optimization guide
- Production checklist

**Use this file to**:
- Understand why each index exists
- Optimize slow queries
- Monitor index health
- Plan index maintenance
- Make index addition/removal decisions

---

## Quick Reference

### Database Schema Summary

| Entity | Primary Key | Key Columns | Row Count (Est.) |
|--------|-------------|-------------|------------------|
| users | id (SERIAL) | telegram_id (UNIQUE) | 1,000 - 100,000 |
| surveys | id (SERIAL) | creator_id, is_active | 100 - 1,000 per user |
| questions | id (SERIAL) | survey_id, order_index | 5 - 20 per survey |
| responses | id (SERIAL) | survey_id, respondent_telegram_id | 100+ per survey |
| answers | id (SERIAL) | response_id, question_id | = questions × responses |

### Relationship Summary

```
USERS (1) ──creates──> (N) SURVEYS
                           │
                           ├──> (N) QUESTIONS
                           │
                           └──> (N) RESPONSES ──> (N) ANSWERS
                                                      │
                           QUESTIONS <───────────────┘
```

### Critical Indexes

1. `idx_users_telegram_id` - User lookup (UNIQUE)
2. `idx_surveys_creator_active` - Dashboard queries (COMPOSITE)
3. `idx_questions_survey_order` - Ordered question retrieval (COMPOSITE)
4. `idx_responses_survey_respondent` - Duplicate checking (COMPOSITE)
5. `idx_answers_response_id` - Response display
6. `idx_answers_question_id` - Analytics

---

## Database Setup

### Prerequisites
- PostgreSQL 14 or higher
- psql command-line tool (optional)
- Entity Framework Core tools (for migrations)

### Option 1: Create Database Manually

```bash
# Create database
createdb surveybot

# Run schema script
psql -d surveybot -f schema.sql

# Verify tables created
psql -d surveybot -c "\dt"
```

### Option 2: Use Entity Framework Migrations

```bash
# From project directory
cd SurveyBot.Database

# Create migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update
```

### Option 3: Docker Compose

```bash
# Start PostgreSQL in Docker
docker-compose up -d postgres

# Database will be created automatically
# Connection string in docker-compose.yml
```

---

## Connection Strings

### Development (Docker)
```
Host=localhost;Port=5432;Database=surveybot;Username=postgres;Password=postgres
```

### Production (Recommended)
```
Host=your-host;Port=5432;Database=surveybot;Username=surveybot_user;Password=<strong-password>;SSL Mode=Require
```

### Entity Framework Core
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=surveybot;Username=postgres;Password=postgres"
  }
}
```

---

## Entity Framework Core Models

### Example Entity Class

```csharp
public class Survey
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    public bool AllowMultipleResponses { get; set; }
    public bool ShowResults { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User Creator { get; set; }
    public ICollection<Question> Questions { get; set; }
    public ICollection<Response> Responses { get; set; }
}
```

See database agent documentation for complete entity models.

---

## Common Queries

### Get Survey with Questions
```sql
SELECT s.*, json_agg(q.* ORDER BY q.order_index) as questions
FROM surveys s
LEFT JOIN questions q ON s.id = q.survey_id
WHERE s.id = ?
GROUP BY s.id;
```

### Check if User Already Responded
```sql
SELECT EXISTS(
    SELECT 1 FROM responses
    WHERE survey_id = ?
    AND respondent_telegram_id = ?
    AND is_complete = true
);
```

### Get Survey Statistics
```sql
SELECT
    s.id,
    s.title,
    COUNT(DISTINCT q.id) as question_count,
    COUNT(DISTINCT r.id) FILTER (WHERE r.is_complete) as response_count,
    COUNT(DISTINCT r.respondent_telegram_id) as unique_respondents
FROM surveys s
LEFT JOIN questions q ON s.id = q.survey_id
LEFT JOIN responses r ON s.id = r.survey_id
WHERE s.id = ?
GROUP BY s.id;
```

### Get Answer Distribution (Multiple Choice)
```sql
SELECT
    jsonb_array_elements_text(answer_json) as option,
    COUNT(*) as count
FROM answers
WHERE question_id = ?
GROUP BY option
ORDER BY count DESC;
```

More examples in [RELATIONSHIPS.md](./RELATIONSHIPS.md).

---

## Performance Guidelines

### Query Performance Targets

| Query Type | Target Time | Notes |
|------------|-------------|-------|
| User lookup | < 1ms | By telegram_id |
| List surveys | < 10ms | User's active surveys |
| Get questions | < 5ms | Ordered by index |
| Check response | < 5ms | Duplicate prevention |
| Get answers | < 10ms | Single response |
| Analytics | < 100ms | Question aggregation |

### Optimization Tips

1. **Always use indexes** for WHERE and JOIN conditions
2. **Batch insert answers** in single transaction
3. **Use partial indexes** for filtered queries (is_active, is_complete)
4. **Enable JSONB indexes** only when needed for analytics
5. **Monitor query plans** with EXPLAIN ANALYZE
6. **Run ANALYZE** after bulk data changes

See [INDEX_OPTIMIZATION.md](./INDEX_OPTIMIZATION.md) for detailed guidance.

---

## Data Integrity

### Constraints Summary

| Constraint Type | Count | Examples |
|----------------|-------|----------|
| Primary Keys | 5 | All tables have SERIAL id |
| Foreign Keys | 5 | All relationships enforced |
| Unique | 3 | telegram_id, survey+order, response+question |
| Check | 4 | question_type, order_index, dates, answer not null |
| Not Null | 20+ | All critical fields |

### Cascade Delete Rules

- Delete User → Deletes all their surveys → Deletes all questions/responses/answers
- Delete Survey → Deletes all questions/responses/answers
- Delete Response → Deletes all answers
- Delete Question → Deletes all answers to that question

**Warning**: Cascade deletes are irreversible. Consider soft deletes for important entities.

---

## Monitoring

### Essential Queries

```sql
-- Check table sizes
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Check index usage
SELECT
    indexrelname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;

-- Check slow queries (requires pg_stat_statements)
SELECT query, calls, mean_exec_time, max_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;
```

### Maintenance Schedule

- **Daily**: Monitor slow queries, check error logs
- **Weekly**: Review index usage, check table sizes
- **Monthly**: Run ANALYZE, review unused indexes
- **Quarterly**: REINDEX large tables, review schema

---

## Migration Strategy

### Adding New Columns

```sql
-- Safe: Nullable column with default
ALTER TABLE surveys
ADD COLUMN new_feature BOOLEAN DEFAULT false;

-- Update existing rows if needed
UPDATE surveys SET new_feature = true WHERE condition;

-- Make NOT NULL if required
ALTER TABLE surveys
ALTER COLUMN new_feature SET NOT NULL;
```

### Adding New Question Types

```sql
-- Update check constraint
ALTER TABLE questions
DROP CONSTRAINT chk_question_type;

ALTER TABLE questions
ADD CONSTRAINT chk_question_type
CHECK (question_type IN (
    'text', 'multiple_choice', 'single_choice',
    'rating', 'yes_no', 'new_type'  -- Add new type
));
```

### Adding Indexes

```sql
-- Create concurrently to avoid locking
CREATE INDEX CONCURRENTLY idx_new_index
ON table_name(column_name);

-- Verify index is being used
EXPLAIN ANALYZE [your query];
```

---

## Troubleshooting

### Problem: Slow Queries

**Solution**:
1. Run `EXPLAIN ANALYZE` on the query
2. Check if indexes are being used
3. Review [INDEX_OPTIMIZATION.md](./INDEX_OPTIMIZATION.md)
4. Add missing indexes if needed

### Problem: Duplicate Responses

**Solution**:
1. Check unique constraint on `responses(survey_id, respondent_telegram_id)`
2. Verify application checks `allow_multiple_responses` flag
3. Review cascade delete rules

### Problem: Missing Answers

**Solution**:
1. Check foreign key constraints (response_id, question_id)
2. Verify transaction completed successfully
3. Check if question was deleted (cascade deletes answers)

### Problem: Index Bloat

**Solution**:
```sql
-- Rebuild index concurrently
REINDEX INDEX CONCURRENTLY idx_name;

-- Or rebuild entire table
REINDEX TABLE CONCURRENTLY table_name;
```

---

## Best Practices

### Development
- Always use migrations for schema changes
- Test queries with EXPLAIN ANALYZE
- Use transactions for multi-table operations
- Validate data before database insertion
- Handle foreign key violations gracefully

### Production
- Enable connection pooling (PgBouncer recommended)
- Set up automated backups (daily minimum)
- Monitor slow query log
- Use read replicas for analytics if needed
- Plan maintenance windows for REINDEX

### Security
- Use least-privilege database user
- Never store plain passwords (even in users table)
- Enable SSL for connections
- Audit sensitive operations
- Regular security updates

---

## Tools and Extensions

### Recommended PostgreSQL Extensions
```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";     -- UUID generation
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements"; -- Query monitoring
CREATE EXTENSION IF NOT EXISTS "pg_trgm";        -- Text search (optional)
```

### Useful Tools
- **pgAdmin 4**: GUI for database management
- **psql**: Command-line interface
- **pg_dump**: Backup utility
- **PgBouncer**: Connection pooler
- **Prometheus + Grafana**: Monitoring (with postgres_exporter)

---

## Version History

### v1.0.0 (2025-11-05)
- Initial schema design
- Five core entities: users, surveys, questions, responses, answers
- Complete indexing strategy
- Documentation created

### Planned Features (Future Versions)
- v1.1.0: Survey templates, question branching logic
- v1.2.0: Multi-language support, survey scheduling
- v1.3.0: Advanced analytics, custom reports
- v2.0.0: Survey sharing, collaboration features

---

## Support and Resources

### Internal Documentation
- [PRD](../PRD_SurveyBot_MVP.md) - Product requirements
- [Architecture](../architecture/) - System architecture (when created)
- [API Documentation](../api/) - REST API specs (when created)

### External Resources
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [PostgreSQL Index Types](https://www.postgresql.org/docs/current/indexes-types.html)
- [JSONB in PostgreSQL](https://www.postgresql.org/docs/current/datatype-json.html)

---

## Contact

For questions about database design or issues:
- Database Agent: Specialized in EF Core and PostgreSQL
- Review [RELATIONSHIPS.md](./RELATIONSHIPS.md) for query patterns
- Check [INDEX_OPTIMIZATION.md](./INDEX_OPTIMIZATION.md) for performance issues

---

**Document Status**: Complete and ready for implementation
**Next Steps**:
1. Create Entity Framework Core models
2. Generate initial migration
3. Set up database in development environment
4. Verify schema with test data
5. Run performance benchmarks

---

## Quick Start Checklist

- [ ] PostgreSQL 14+ installed
- [ ] Database created (`surveybot`)
- [ ] Schema applied (schema.sql or EF migration)
- [ ] Connection string configured
- [ ] Tables verified (`\dt` in psql)
- [ ] Sample data inserted for testing
- [ ] Indexes verified (`\di` in psql)
- [ ] Performance targets tested
- [ ] Backup strategy configured

**Ready to start development!**
