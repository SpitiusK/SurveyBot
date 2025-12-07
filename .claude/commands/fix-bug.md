---
name: fix-bug
description: Implement bug fixes based on Bug Analysis Report from /analyze-bug. Delegates to specialist agents by layer (API, EF Core, Bot, Frontend, Testing), runs tests, and updates documentation. Requires analysis report to exist.
model: sonnet
color: red
---

# Bug Fix Implementation Command

## Command Overview

This command **implements bug fixes** based on the comprehensive Bug Analysis Report produced by `/analyze-bug`. It orchestrates specialist agents according to the fix strategy, ensures v1.6.2 pattern compliance, runs tests to verify the fix, and updates documentation if needed.

**Single Responsibility**: Fix implementation and verification - delegates to specialist agents, does not perform deep analysis.

**Workflow**: Read analysis report → Confirm fix strategy → Delegate to specialist agents → Run tests → Update docs → Verify success

**SurveyBot v1.6.2 Compliance**: This command enforces Clean Architecture principles, DDD patterns (factory methods, private setters, value objects), and ensures all fixes follow established architectural patterns.

## User Prompt

$ARGUMENTS

## When to Use This Command

Use `/fix-bug` when:

- **After `/analyze-bug`** has generated a Bug Analysis Report
- **Ready to implement** the recommended fix strategy
- **Tests can be run** to verify the fix
- **Have confirmed** the fix approach with stakeholders (if needed)

**Prerequisites**:
- Bug Analysis Report exists at `.claude/agents/out/BUG_ANALYSIS_REPORT.md`
- Root cause is clearly identified in the report
- Fix strategy is documented and approved

**Do NOT use** for:
- Initial bug investigation (use `/analyze-bug` first)
- Exploring multiple fix approaches (do that in analysis phase)
- Feature development (use `/task-execution-agent` or specialist agents)

## Agents and Responsibilities

This command delegates to **specialist implementation agents** based on affected layer(s):

### Layer-Based Agent Matrix

| Affected Layer | Primary Agent | Responsibility |
|----------------|---------------|----------------|
| **API Layer** | @aspnet-api-agent | Controllers, middleware, AutoMapper profiles, DTOs |
| **Infrastructure Layer** | @ef-core-agent | Entities, repositories, services, DbContext, migrations |
| **Bot Layer** | @telegram-bot-handler-agent | Bot handlers, commands, state management, navigation |
| **Frontend (React)** | @frontend-admin-agent | React components, API clients, UI logic |
| **Core Layer** | @ef-core-agent | Entities, value objects, factory methods (core is usually infrastructure concern) |

### Cross-Cutting Agents

| Agent | When to Invoke |
|-------|----------------|
| @dotnet-testing-agent | Always - for regression tests after fix |
| @claude-md-documentation-agent | If fix changes public APIs, flows, or architecture |
| @codebase-analyzer | Only if fix reveals additional issues during implementation |

### Agent Delegation Examples

**Example 1**: Bug in ResponseService.DetermineNonBranchingNextStepAsync (Infrastructure)
- **Primary**: @ef-core-agent (fix service logic)
- **Secondary**: @dotnet-testing-agent (add regression test)

**Example 2**: Bug in SurveyCommandHandler cache invalidation (Bot)
- **Primary**: @telegram-bot-handler-agent (fix handler logic)
- **Secondary**: @dotnet-testing-agent (add integration test)

**Example 3**: Bug in AutoMapper profile for AnswerValue (API)
- **Primary**: @aspnet-api-agent (fix mapping profile)
- **Secondary**: @dotnet-testing-agent (add mapping test)

**Example 4**: Bug in survey update atomic transaction (API + Infrastructure)
- **Primary**: @aspnet-api-agent (fix controller)
- **Secondary**: @ef-core-agent (fix service transaction)
- **Tertiary**: @dotnet-testing-agent (add integration test)

## Interaction Workflow

### Step 1: Read Bug Analysis Report
- Load `.claude/agents/out/BUG_ANALYSIS_REPORT.md`
- Extract:
  - Root cause summary
  - Affected files and components
  - Recommended fix strategy
  - Specialist agent assignments
  - Test requirements
  - v1.6.2 pattern compliance checklist

**If report does NOT exist**:
- Stop and inform user: "Bug Analysis Report not found. Please run `/analyze-bug` first to generate the analysis."
- Exit command

**If report exists but is incomplete**:
- Ask user: "The analysis report seems incomplete (missing fix strategy or affected files). Should I re-run `/analyze-bug`, or do you want to provide the fix approach manually?"

### Step 2: Present Fix Strategy to User
Display a concise summary from the report:

**Summary Format**:
```
📋 Bug Fix Plan (from Analysis Report)

**Root Cause**: [One-sentence explanation]

**Fix Approach**: [High-level strategy]

**Affected Files**:
- [File 1] - [Change type]
- [File 2] - [Change type]

**Agent Assignments**:
- Primary: @[agent-name] (for [layer/component])
- Testing: @dotnet-testing-agent (for regression tests)

**v1.6.2 Compliance**:
- [Factory method usage / Private setters / Value objects / etc.]

**Risk Level**: [Low/Medium/High] - [Justification]

**Estimated Effort**: [Simple/Moderate/Complex]
```

### Step 3: Ask Confirmation Question (Batch 1)
Ask user to confirm or adjust the fix strategy:

**Confirmation Questions**:
- "Does this fix strategy look correct? Should I proceed with implementation?"
- "Are there any constraints or additional considerations I should know about (e.g., backwards compatibility, deployment timing)?"
- "Do you want me to implement the fix now, or would you like to adjust the approach?"

**User Options**:
- **"Proceed"**: Continue to Step 4
- **"Adjust"**: Ask follow-up questions about specific changes
- **"Abort"**: Stop and allow user to modify analysis report manually

### Step 4: Delegate to Specialist Agent(s) - Primary Fix
Based on the agent assignment from the report, delegate the main fix:

**Delegation Pattern**:
```
@[specialist-agent]: Implement the following fix based on Bug Analysis Report:

**Root Cause**: [From report]

**Fix Approach**: [Detailed steps from report]

**Affected Files**:
- [Absolute path 1]
- [Absolute path 2]

**v1.6.2 Pattern Compliance**:
- [Factory method requirements]
- [Private setter usage]
- [Value object handling]
- [Migration needs]

**Specific Instructions**:
[Step-by-step implementation from fix strategy]

**Success Criteria**:
- [Criterion 1]
- [Criterion 2]

Context: This is a bug fix for [bug summary]. Ensure compliance with Clean Architecture and DDD patterns.
```

**Agent-Specific Instructions**:

**For @aspnet-api-agent**:
- Specify controller methods to modify
- Include AutoMapper profile changes if needed
- Mention DTO updates required
- Note middleware or filter changes

**For @ef-core-agent**:
- Specify entity modifications (use setter methods, factory methods)
- Include repository method changes
- Mention service layer updates
- Specify if migration is needed and owned type configurations
- Ensure value object handling is correct

**For @telegram-bot-handler-agent**:
- Specify handler classes to modify
- Include state management changes
- Mention conversation flow updates
- Note inline keyboard or message template changes

**For @frontend-admin-agent**:
- Specify React components to modify
- Include API client updates
- Mention state management changes (Redux/Context)
- Note UI/UX implications

### Step 5: Monitor Specialist Agent Progress
Wait for specialist agent to complete the fix. Review the changes:

**Validation Checklist**:
- ✅ All files from "Affected Files" list were modified
- ✅ v1.6.2 patterns followed (factory methods, private setters, value objects)
- ✅ No new compilation errors introduced
- ✅ Changes align with fix strategy from report
- ✅ Code follows SurveyBot coding standards

**If issues detected**:
- Clarify with specialist agent
- Request adjustments
- Document deviations from original plan

### Step 6: Delegate to @dotnet-testing-agent - Regression Tests
After primary fix is complete, create regression tests:

**Delegation Pattern**:
```
@dotnet-testing-agent: Create regression tests for the bug fix based on Bug Analysis Report.

**Bug Summary**: [From report]

**Fixed Components**:
- [Component 1]
- [Component 2]

**Test Requirements** (from report):

**Unit Tests**:
- [Test case 1 from report]
- [Test case 2 from report]

**Integration Tests**:
- [Test case 1 from report]
- [Test case 2 from report]

**Regression Test Goal**: Ensure this specific bug does not recur.

**Test File Locations**: [Based on affected layer - e.g., tests/SurveyBot.Tests/Unit/Infrastructure/ResponseServiceTests.cs]

**Success Criteria**:
- All new tests pass
- Existing tests still pass
- Test coverage for bug scenario is complete

Context: This is a regression test for [bug ID/summary]. Ensure tests cover the exact reproduction steps from the analysis report.
```

### Step 7: Run Tests and Verify Fix
Execute the test suite to verify the fix:

**Test Execution**:
```bash
cd C:\Users\User\Desktop\SurveyBot
dotnet test --logger "console;verbosity=detailed"
```

**Validation**:
- ✅ All tests pass (including new regression tests)
- ✅ No new test failures introduced
- ✅ Bug reproduction scenario now passes
- ✅ No performance regressions

**If tests fail**:
- Analyze failure output
- Determine if fix introduced new issues or tests need adjustment
- **IMPORTANT**: See "Post-Agent Error Handling" section below for proper error reporting behavior

### Step 8: Update Documentation (Conditional)
If the fix affects public APIs, user flows, or architecture:

**When to Update Docs**:
- Public API endpoints changed (new parameters, responses)
- Bot commands or flows modified
- Configuration settings changed
- Database schema modified (new migrations)
- Architectural patterns changed

**Delegation Pattern**:
```
@claude-md-documentation-agent: Update documentation to reflect bug fix changes.

**Fix Summary**: [One-sentence description]

**Changed Components**:
- [Component 1] - [What changed]
- [Component 2] - [What changed]

**Documentation Updates Needed**:
- [Layer CLAUDE.md file] - [Section to update]
- [API documentation] - [Endpoint changes]
- [User guide] - [Flow changes]

**Specific Changes**:
[Detailed list of documentation updates]

Context: Bug fix for [bug ID]. Ensure documentation reflects current behavior post-fix.
```

**Documentation Files to Consider**:
- Layer CLAUDE.md files (Core, Infrastructure, Bot, API)
- `documentation/bot/` (if bot flows changed)
- `documentation/api/` (if API endpoints changed)
- `documentation/database/` (if schema changed)
- `CLAUDE.md` root file (if major architecture change)

**Skip documentation** if:
- Internal implementation detail (not user-facing)
- No API contract changes
- No flow changes

### Step 9: Generate Fix Summary Report
Create a concise fix summary and update the Bug Analysis Report status:

**Fix Summary Format**:
```
✅ Bug Fix Complete

**Bug ID**: [From analysis report]

**Root Cause**: [One-sentence explanation]

**Fix Implemented**:
- [Change 1]
- [Change 2]

**Files Modified**:
- [Absolute path 1]
- [Absolute path 2]

**Tests Added**:
- [Test file 1] - [Test count] new tests
- [Test file 2] - [Test count] new tests

**Test Results**: ✅ All tests pass ([X] total, [Y] new)

**Documentation Updated**: [Yes/No] - [Files updated or "No user-facing changes"]

**v1.6.2 Compliance**: ✅ Verified
- [Factory methods used correctly]
- [Private setters enforced]
- [Value objects handled properly]

**Ready for Deployment**: [Yes/No] - [Any caveats]

**Next Steps**:
- [Commit changes with appropriate message]
- [Deploy to environment]
- [Monitor for regressions]
```

### Step 10: Append Fix Results to Analysis Report
Update `.claude/agents/out/BUG_ANALYSIS_REPORT.md` with fix results:

**Append Section**:
```markdown
---

## Fix Implementation Results

**Fix Date**: [Timestamp]

**Status**: ✅ Fix Complete and Verified

### Changes Implemented

| File | Change Description | Agent |
|------|-------------------|-------|
| [Path] | [Description] | @[agent-name] |

### Tests Added

- **Unit Tests**: [Test file] - [Count] tests
- **Integration Tests**: [Test file] - [Count] tests

### Test Results

```
[Test execution output summary]
Total: [X] tests, Passed: [Y], Failed: 0
```

### Documentation Updated

- [File 1] - [Section updated]
- [File 2] - [Section updated]

### v1.6.2 Pattern Compliance Verified

- ✅ Factory methods used for entity creation
- ✅ Private setters enforced
- ✅ Value objects handled correctly
- ✅ Clean Architecture layers respected

### Deployment Notes

[Any migration steps, configuration changes, or deployment considerations]

### Verification Checklist

- [x] All tests pass
- [x] Bug reproduction scenario fixed
- [x] No new issues introduced
- [x] Documentation updated
- [x] Code review ready

**Fix completed by**: @[primary-agent], @dotnet-testing-agent, @claude-md-documentation-agent
```

### Step 11: Present Final Summary to User
Provide the user with the complete fix summary (from Step 9).

Ask final questions:
- "The bug fix is complete and verified. Would you like me to commit these changes?"
- "Are there any additional manual testing steps you want to perform before deployment?"

**If user wants commit**:
- Use standard git commit workflow
- Include bug ID in commit message: `fix(layer): [Bug ID] - [One-line description]`
- Reference analysis report in commit body

**If user wants manual testing**:
- Provide manual testing checklist from analysis report
- Wait for user confirmation before suggesting commit

## Clarifying Questions Strategy

**Batch 1 (Always Asked)**: Confirmation of fix strategy (Step 3)

**Batch 2 (Conditional)**: Asked only if fix strategy needs adjustment
- "Which specific approach do you prefer: [Alternative 1] or [Alternative 2]?"
- "Should I prioritize backwards compatibility or clean implementation?"
- "Are there deployment constraints (e.g., migration timing)?"

**Batch 3 (Post-Fix)**: Asked only if issues arise during implementation
- "Tests are failing in [component]. Should I adjust the fix or the tests?"
- "Documentation update affects [section]. Should I include [additional detail]?"

**Fast Path**: If analysis report is comprehensive and user confirms "proceed" immediately, skip most clarifications and execute fix workflow directly.

**Conservative Assumptions**: If user wants minimal questions:
- Follow fix strategy exactly as documented in report
- Use primary agent assignment from report
- Skip documentation updates unless API contracts change
- Document assumptions in fix summary

## Out of Scope

This command does **NOT**:

- **Perform deep analysis** - that's Phase 1 (/analyze-bug)
- **Explore multiple fix approaches** - strategy should be decided in analysis phase
- **Handle feature development** - only bug fixes
- **Deploy to production** - only prepares changes for deployment
- **Make architectural decisions** - follows existing patterns and decisions from analysis
- **Replace specialist agents** - this is orchestration, delegates all implementation

## Example Invocations

### Example 1: Simple Service Logic Fix
```
/fix-bug Implement the fix for the EndSurvey bug identified in the analysis report
```

**Expected Flow**:
1. Read report: Bug in ResponseService.DetermineNonBranchingNextStepAsync
2. Confirm fix: Add EndSurvey check before sequential fallback
3. @ef-core-agent: Modify ResponseService.cs
4. @dotnet-testing-agent: Add regression test in ResponseServiceTests.cs
5. Run tests: All pass
6. Documentation: No user-facing changes, skip
7. Summary: Fix complete, ready to commit

### Example 2: Bot Cache Invalidation Fix
```
/fix-bug Fix the cache invalidation issue from BOT-FIX-001 analysis
```

**Expected Flow**:
1. Read report: Bug in SurveyCommandHandler not invalidating cache
2. Confirm fix: Add cache.Remove() call in SurveyCommandHandler.HandleCallbackQueryAsync
3. @telegram-bot-handler-agent: Modify SurveyCommandHandler.cs (lines 104-107)
4. @dotnet-testing-agent: Add integration test for cache invalidation
5. Run tests: All pass
6. Documentation: Update bot layer CLAUDE.md with cache invalidation pattern
7. Summary: Fix complete, ready to commit

### Example 3: Cross-Layer AutoMapper Fix
```
/fix-bug Implement the AnswerValue mapping fix identified in analysis
```

**Expected Flow**:
1. Read report: Bug in AutoMapper profiles for AnswerValue polymorphic hierarchy
2. Confirm fix: Update SurveyMappingProfile and AnswerMappingProfile to handle AnswerValue → DTO conversion
3. @aspnet-api-agent: Modify mapping profiles in SurveyBot.API/Mapping/
4. @dotnet-testing-agent: Add mapping tests in AutoMapperProfileTests.cs
5. Run tests: All pass
6. Documentation: Update API CLAUDE.md with new mapping patterns
7. Summary: Fix complete, ready to commit

### Example 4: Database Migration Fix
```
/fix-bug Fix the entity tracking issue in question updates
```

**Expected Flow**:
1. Read report: Bug in QuestionRepository - entity already tracked
2. Confirm fix: Use AsNoTracking() for read operations, attach for updates
3. @ef-core-agent: Modify QuestionRepository.cs and update methods
4. @ef-core-agent: Add migration if entity configuration changed
5. @dotnet-testing-agent: Add integration test for update scenarios
6. Run tests: All pass
7. Documentation: Update Infrastructure CLAUDE.md with entity tracking patterns
8. Summary: Fix complete, note migration step for deployment

## Success Criteria

This command succeeds when:

1. ✅ Bug Analysis Report successfully read and parsed
2. ✅ Fix strategy confirmed with user
3. ✅ All affected files modified by specialist agents
4. ✅ v1.6.2 patterns enforced (factory methods, private setters, value objects)
5. ✅ Regression tests created and passing
6. ✅ All existing tests still pass
7. ✅ Documentation updated (if needed)
8. ✅ Fix summary appended to Bug Analysis Report
9. ✅ User confirms fix is ready for commit/deployment

**Failure Conditions**:
- Bug Analysis Report missing → Direct user to run `/analyze-bug` first
- Tests fail after fix → **Generate error report and stop** (see Post-Agent Error Handling)
- Fix introduces new issues → **Generate error report and stop** (do NOT manually fix)
- User rejects fix strategy → Return to `/analyze-bug` or adjust approach

## Post-Agent Error Handling (CRITICAL)

**⚠️ IMPORTANT: Do NOT manually fix errors after agents complete their work.**

When specialist agents finish their implementation and the fix attempt results in new errors (compilation errors, test failures, etc.), this command must **STOP and generate a report** rather than attempting to manually resolve those errors.

**Rationale**:
- After agents complete work, the command may continue operating without agent delegation
- Manual error resolution consumes excessive context (token usage)
- Iterative manual fixes lead to context exhaustion and degraded quality
- User needs visibility into what happened before deciding next steps

**Required Behavior**:

When errors occur after agent work is complete:

1. **STOP** - Do not attempt manual fixes
2. **REPORT** - Generate an error report with the following format:

```
⚠️ Fix Attempt Complete - Errors Detected

**Original Bug**: [Bug ID/Summary]

**Fix Applied By**: @[agent-name]

**Files Modified**:
- [File 1]
- [File 2]

**Errors Introduced**:
| Error Type | Location | Message |
|------------|----------|---------|
| [Compilation/Test/Runtime] | [File:Line] | [Error message] |

**Error Count**: [X] errors total

**Recommended Actions**:
- [ ] Review errors manually
- [ ] Run `/analyze-bug` on new errors if complex
- [ ] Adjust fix strategy based on error patterns
- [ ] Re-run `/fix-bug` after strategy adjustment

**Context Preserved**: Fix report saved to `.claude/agents/out/BUG_ANALYSIS_REPORT.md`
```

3. **SAVE** - Append error report to the Bug Analysis Report
4. **EXIT** - Return control to user for decision

**What NOT to do**:
- ❌ Do not iterate through errors one by one manually
- ❌ Do not spawn additional agents for error resolution without user consent
- ❌ Do not attempt "quick fixes" for compilation errors
- ❌ Do not continue with documentation updates if fix introduced errors

**User Options After Error Report**:
- **Re-run `/fix-bug`** with adjusted strategy
- **Run `/analyze-bug`** on the new errors
- **Manually fix** the specific errors themselves
- **Revert changes** and try different approach

This ensures the user maintains control and context is preserved for meaningful next steps.

---

## Notes

**Phase 1 Dependency**: This command is Phase 2 of the two-phase bug workflow. It depends on `/analyze-bug` (Phase 1) having produced a comprehensive Bug Analysis Report.

**SurveyBot v1.6.2 Enforcement**: This command actively enforces architectural patterns:
- Verifies factory method usage for entity creation
- Checks private setter compliance
- Validates value object immutability
- Ensures Clean Architecture layer boundaries

**Agent Delegation Matrix**: Uses layer-based delegation to ensure specialist expertise:
- API issues → @aspnet-api-agent
- Infrastructure issues → @ef-core-agent
- Bot issues → @telegram-bot-handler-agent
- Frontend issues → @frontend-admin-agent
- Testing always → @dotnet-testing-agent

**Test-Driven Verification**: Always creates regression tests before considering fix complete. Tests serve as executable documentation that the bug is truly fixed.

**Documentation as Code**: Documentation updates are part of the fix, not an afterthought. Ensures future developers understand the fix context.

**Model Choice**: Sonnet for complex multi-agent coordination, test orchestration, and verification workflows.

**Absolute Paths**: All file references use absolute paths (e.g., C:\Users\User\Desktop\SurveyBot\...) for clarity.
