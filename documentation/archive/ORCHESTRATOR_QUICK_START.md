# Orchestrator Quick Start Guide - NextQuestionDeterminant Refactoring

**Task File**: `C:\Users\User\Desktop\SurveyBot\task.yaml`
**Total Tasks**: 43
**Estimated Duration**: 2-3 weeks (2 developers)
**Status**: Ready to execute

---

## Quick Commands

### Start Orchestrator
```bash
cd C:\Users\User\Desktop\SurveyBot
/orchestrator-agent
```

### Review Tasks
```bash
# View task.yaml in editor
code task.yaml

# Count tasks
grep "task_id:" task.yaml | wc -l
```

### Monitor Progress
- Track completed tasks in orchestrator output
- Check CLEANUP-001 for final verification

---

## Critical Path (Must Execute in Order)

### Phase 1: Core Foundation (17 hours critical path)
```
CORE-001 (4h) → CORE-002 (3h) → INFRA-001 (4h) → INFRA-002 (3h) → INFRA-003 (3h)
```

**Checkpoint**: Database ready for value object persistence

### Phase 2: Implementation (Parallel Opportunities)
After INFRA-001 completes, can run in parallel:
- INFRA-004, INFRA-005, INFRA-006 (services)
- API-001 → API-002, API-003 (controllers)
- BOT-001, BOT-002, BOT-003 (bot handlers)

### Phase 3: Frontend (Sequential)
```
API-001 (3h) → FRONTEND-001 (2h) → FRONTEND-002/003/004/005 (parallel, 12h total)
```

### Phase 4: Testing & Cleanup
```
[All Implementation] → TEST-001 through TEST-005 → CLEANUP-001
```

---

## Agent Assignment

| Agent | Tasks | Files | Primary Work |
|-------|-------|-------|--------------|
| ef-core-agent | 12 | 15+ | Entities, EF config, migrations, services |
| aspnet-api-agent | 10 | 12+ | DTOs, API controllers, AutoMapper |
| telegram-bot-handler-agent | 5 | 5+ | Bot handlers, navigation |
| frontend-admin-agent | 5 | 7+ | React components, TypeScript |
| dotnet-testing-agent | 5 | 8+ | All tests |
| project-cleanup-agent | 1 | - | Final verification |

---

## Pre-Execution Checklist

### Before Starting
- [ ] Review implementation plan: `src\SurveyBot.API\task-plan-refactor-DDD-implementation.md`
- [ ] Confirm clean slate approach acceptable (DATA WILL BE LOST)
- [ ] Backup existing data (optional but recommended)
- [ ] Create feature branch: `feature/nextquestion-determinant-refactoring`
- [ ] Ensure all agents available

### Database Backup (Optional)
```sql
-- Connect to database
-- Run these commands:
CREATE TABLE questions_backup AS SELECT * FROM questions;
CREATE TABLE question_options_backup AS SELECT * FROM question_options;
CREATE TABLE surveys_backup AS SELECT * FROM surveys;
```

### Git Branch
```bash
cd C:\Users\User\Desktop\SurveyBot
git checkout development
git pull
git checkout -b feature/nextquestion-determinant-refactoring
```

---

## Execution Phases

### Phase 1: Core Layer (5 tasks, ~10 hours)

**Start**: CORE-001
**End**: CORE-005
**Agent**: ef-core-agent, aspnet-api-agent
**Deliverable**: Value object and updated entities

**Key Files Created**:
- `src\SurveyBot.Core\ValueObjects\NextQuestionDeterminant.cs`
- `src\SurveyBot.Core\Enums\NextStepType.cs`
- `src\SurveyBot.Core\DTOs\NextQuestionDeterminantDto.cs`
- `src\SurveyBot.Core\Extensions\NextQuestionDeterminantExtensions.cs`

**Verification**:
```bash
dotnet build src\SurveyBot.Core\SurveyBot.Core.csproj
```

---

### Phase 2: Infrastructure Layer (7 tasks, ~20 hours)

**Start**: INFRA-001
**End**: INFRA-007
**Agent**: ef-core-agent
**Deliverable**: Database migration and updated services

**Key Tasks**:
1. INFRA-001: EF Core owned type configuration
2. INFRA-002: Generate migration
3. INFRA-003: Customize migration (ADD TRUNCATE CASCADE)
4. INFRA-004/005/006: Update services
5. INFRA-007: Update repositories

**Migration Commands** (INFRA-002):
```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet ef migrations add CleanSlateNextQuestionDeterminant --project ..\SurveyBot.Infrastructure
```

**CRITICAL**: INFRA-003 requires manual editing of migration file to add:
- TRUNCATE CASCADE statements
- CHECK constraints
- FK constraints with ON DELETE SET NULL

**Apply Migration**:
```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet ef database update
```

**Verification**:
```bash
dotnet build src\SurveyBot.Infrastructure\SurveyBot.Infrastructure.csproj
```

---

### Phase 3: API Layer (4 tasks, ~9 hours)

**Start**: API-001
**End**: API-004
**Agent**: aspnet-api-agent
**Deliverable**: Updated controllers and Swagger docs

**Key Files**:
- `src\SurveyBot.API\Mapping\QuestionMappingProfile.cs`
- `src\SurveyBot.API\Controllers\SurveysController.cs`
- `src\SurveyBot.API\Controllers\QuestionFlowController.cs`

**Verification**:
```bash
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet run
# Open http://localhost:5000/swagger
# Test endpoints with new DTO structure
```

---

### Phase 4: Bot Layer (4 tasks, ~10 hours)

**Start**: BOT-001
**End**: BOT-004
**Agent**: telegram-bot-handler-agent
**Deliverable**: Updated bot navigation

**Key Files**:
- `src\SurveyBot.Bot\Handlers\SurveyResponseHandler.cs`
- `src\SurveyBot.Bot\Models\ConversationState.cs`
- `src\SurveyBot.Bot\Utilities\SurveyNavigationHelper.cs`

**Verification**: Manual bot testing (BOT-004)
- Test linear flow
- Test conditional flow
- Test survey completion

---

### Phase 5: Frontend Layer (5 tasks, ~12 hours)

**Start**: FRONTEND-001
**End**: FRONTEND-005
**Agent**: frontend-admin-agent
**Deliverable**: Updated React UI

**Key Files**:
- `frontend\src\types\index.ts`
- `frontend\src\components\SurveyBuilder\QuestionEditor.tsx`
- `frontend\src\components\SurveyBuilder\FlowVisualization.tsx`
- `frontend\src\services\questionFlowService.ts`

**Verification**:
```bash
cd C:\Users\User\Desktop\SurveyBot\frontend
npm run build
npm run dev
# Test UI: Create survey with conditional flow
```

---

### Phase 6: Testing (5 tasks, ~17 hours)

**Start**: TEST-001
**End**: TEST-005
**Agent**: dotnet-testing-agent
**Deliverable**: Complete test coverage

**Run All Tests**:
```bash
cd C:\Users\User\Desktop\SurveyBot
dotnet test
```

**Expected**: All tests pass (100% success rate)

---

### Phase 7: Documentation & Cleanup (6 tasks, ~9 hours)

**Start**: DOCS-001
**End**: CLEANUP-001
**Agent**: Various agents + project-cleanup-agent
**Deliverable**: Updated documentation and clean code

**Final Verification (CLEANUP-001)**:
```bash
# Search for magic values
cd C:\Users\User\Desktop\SurveyBot
grep -r "NextQuestionId.*==.*0" --include="*.cs"
grep -r "EndOfSurveyMarker" --include="*.cs"

# Should return no results

# Run all tests
dotnet test

# Should be 100% pass
```

---

## Success Criteria

### Technical Success
- [ ] NextQuestionDeterminant value object created
- [ ] All entities updated (no magic values)
- [ ] Database migrated successfully
- [ ] All services use value object
- [ ] API accepts new DTO structure
- [ ] Bot navigation works with value object
- [ ] Frontend shows "End Survey" option
- [ ] All tests pass (100%)

### Code Quality
- [ ] Zero magic value (0) references
- [ ] No EndOfSurveyMarker constant
- [ ] Clean Architecture compliance maintained
- [ ] All CLAUDE.md files updated
- [ ] Swagger documentation accurate

### Verification
```bash
# No magic values
grep -r "== 0" --include="*.cs" | grep -i "nextquestion" | wc -l
# Expected: 0

# All tests pass
dotnet test --verbosity normal
# Expected: 100% pass rate

# Migration applied
dotnet ef migrations list
# Expected: CleanSlateNextQuestionDeterminant listed
```

---

## Common Issues & Solutions

### Issue 1: Migration Fails
**Solution**: Check database connection, ensure PostgreSQL running
```bash
docker ps | grep postgres
docker-compose up -d postgres
```

### Issue 2: Tests Fail After Migration
**Solution**: Clear test database and reapply migrations
```bash
cd src\SurveyBot.API
dotnet ef database drop --force
dotnet ef database update
```

### Issue 3: AutoMapper Errors
**Solution**: Verify mapping configuration in API-001
```bash
# Run AutoMapper configuration test
dotnet test --filter "AutoMapper"
```

### Issue 4: Frontend Build Errors
**Solution**: Check TypeScript types match backend DTOs
```bash
cd frontend
npm run type-check
```

---

## Post-Execution Steps

### Code Review
```bash
# Show all changed files
git status

# Review changes
git diff development
```

### Final Testing
```bash
# Run all tests
dotnet test

# Run API
cd src\SurveyBot.API
dotnet run

# Test Swagger: http://localhost:5000/swagger
```

### Commit & Push
```bash
git add .
git commit -m "feat: Implement NextQuestionDeterminant DDD refactoring

- Replaced magic value (0) with semantic value object
- Clean slate database migration
- Updated all layers (Core, Infrastructure, API, Bot, Frontend)
- 100% test coverage maintained
- Documentation updated

BREAKING CHANGE: NextQuestionId field replaced with NextQuestionDeterminant object"

git push origin feature/nextquestion-determinant-refactoring
```

### Create Pull Request
- Target: `development` branch
- Title: "Implement NextQuestionDeterminant DDD Refactoring"
- Description: Link to implementation plan and this guide
- Reviewers: Assign team members

---

## Timeline Tracking

### Week 1 (40 hours)
- [ ] Day 1-2: Phase 1 + Phase 2 (Core + Infrastructure)
- [ ] Day 3-4: Phase 3 + Phase 4 (API + Bot)
- [ ] Day 5: Start Phase 5 (Frontend)

### Week 2 (40 hours)
- [ ] Day 1-2: Complete Phase 5 (Frontend)
- [ ] Day 3-4: Phase 6 (Testing)
- [ ] Day 5: Phase 7 (Documentation & Cleanup)

### Week 3 (Optional Buffer)
- [ ] Final testing and refinement
- [ ] Code review
- [ ] Deploy to staging
- [ ] Production deployment

---

## Emergency Rollback

If critical issues arise and rollback needed:

```bash
# Revert migration
cd C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API
dotnet ef database update <PreviousMigrationName>

# Revert code
git checkout development
git branch -D feature/nextquestion-determinant-refactoring

# Restore backup (if created)
# Run backup restoration SQL scripts
```

**Note**: Clean slate approach means rollback = data loss. Only rollback if absolutely necessary.

---

## Contact & Support

**Implementation Plan**: `src\SurveyBot.API\task-plan-refactor-DDD-implementation.md`
**Detailed Report**: `TASK_YAML_GENERATION_REPORT.md`
**Task File**: `task.yaml`

**Questions?** Review the implementation plan and detailed report first.

---

**Ready to Execute**: YES ✅
**Status**: All 43 tasks defined and ready
**Risk Level**: MEDIUM (mitigated)
**Estimated Completion**: 2-3 weeks

Good luck! 🚀
