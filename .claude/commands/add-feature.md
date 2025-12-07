---
name: add-feature
description: Guides feature implementation from planning through testing and documentation. Analyzes architectural fit, delegates to layer-specific specialists (API, EF Core, Bot, Frontend), and ensures DDD pattern compliance for SurveyBot v1.6.2.
model: sonnet
color: blue
---

# Add Feature Command

## Command Overview

This command orchestrates end-to-end feature implementation for SurveyBot v1.6.2, ensuring architectural alignment with Clean Architecture, DDD patterns, and value object principles. It coordinates feature planning, architectural analysis, layer-specific implementation, testing, and documentation updates.

**Initial Context**: $ARGUMENTS

The command workflow begins by parsing the feature request from $ARGUMENTS, then guides you through clarifying questions, architectural analysis, implementation planning, and coordinated execution across specialist agents.

**Core Capabilities**:
- Feature breakdown and task planning via @feature-planning-agent
- Architectural fit analysis via @architecture-deep-dive-agent
- Layer-specific implementation delegation (API, Infrastructure, Bot, Frontend)
- Comprehensive test coverage via @dotnet-testing-agent
- Documentation updates via @claude-md-documentation-agent

**v1.6.2 Pattern Enforcement**:
- Entity creation using factory methods (Survey.Create(), Question.Create())
- Entity modification using setter methods (SetTitle(), SetCode())
- Value object usage (NextQuestionDeterminant, AnswerValue hierarchy)
- Owned type configuration in migrations
- Private setters with IReadOnlyCollection exposure

---

## When to Use This Command

Use `/add-feature` when you need to:

- Implement a new feature that spans multiple architectural layers
- Add new endpoints, entities, migrations, or bot handlers with architectural validation
- Ensure DDD and Clean Architecture compliance during feature development
- Coordinate work across API, Infrastructure, Bot, and Frontend layers
- Create comprehensive tests and documentation for new functionality
- Validate that new features align with SurveyBot's architectural principles
- Implement features requiring value objects or entity factory methods

**Examples**:
- `/add-feature Add survey templates feature allowing users to save and reuse survey structures`
- `/add-feature Implement question logic conditions (show/hide based on previous answers)`
- `/add-feature Add export survey results to Excel with charts and statistics`

---

## Agents and Responsibilities

This command coordinates the following specialist agents:

### Planning and Architecture
- **@feature-planning-agent**: Breaks down feature into tasks, estimates effort, identifies dependencies
- **@architecture-deep-dive-agent**: Analyzes architectural fit, validates layer boundaries, ensures DDD compliance

### Implementation Specialists
- **@aspnet-api-agent**: Creates controllers, DTOs, AutoMapper profiles, middleware
- **@ef-core-agent**: Creates entities (with factory methods), repositories, migrations (with owned types)
- **@telegram-bot-handler-agent**: Creates bot handlers, updates conversation state, implements bot commands
- **@frontend-admin-agent**: Creates React components, hooks, API integration, TypeScript types

### Quality Assurance
- **@dotnet-testing-agent**: Creates unit tests, integration tests, validates test coverage
- **@claude-md-documentation-agent**: Updates CLAUDE.md files, API references, user guides

---

## Interaction Workflow

### Step 1: Parse Feature Request

Extract the feature description from $ARGUMENTS. Identify:
- Feature goal and user value
- Affected layers (Core, Infrastructure, API, Bot, Frontend)
- Potential entities, value objects, or migrations needed
- Integration points with existing features

### Step 2: Clarify Scope and Requirements

Ask the user 3-5 focused questions to clarify:

**Scope Questions**:
- Which layers does this feature affect? (API only, Bot only, Full-stack)
- Should this feature modify existing entities or create new ones?
- Are there specific DDD patterns required? (Value objects, aggregates, domain events)

**Priority Questions**:
- Is this feature critical path or can it be implemented incrementally?
- Are there dependencies on other in-progress features?
- What is the preferred implementation timeline?

**Constraint Questions**:
- Are there performance requirements? (Response time, throughput)
- Are there backward compatibility concerns?
- Should this feature be feature-flagged for gradual rollout?

If the user requests "skip questions" or "proceed", make conservative assumptions based on SurveyBot v1.6.2 architecture and document them clearly.

### Step 3: Feature Planning

Call **@feature-planning-agent** with:
- Feature description from $ARGUMENTS
- Answers to clarifying questions
- SurveyBot v1.6.2 context (DDD patterns, Clean Architecture, current version)

**Expected Output**:
- Task breakdown by layer (Core, Infrastructure, API, Bot, Frontend)
- Implementation sequence with dependencies
- Effort estimates
- Risk identification

Present the plan to the user and confirm before proceeding.

### Step 4: Architectural Fit Analysis

Call **@architecture-deep-dive-agent** with:
- Feature plan from Step 3
- Affected layers and entities
- DDD pattern requirements

**Expected Output**:
- Architectural compliance validation
- Layer boundary verification
- Value object and factory method recommendations
- Migration strategy for entity changes
- Potential architectural violations or risks

Present the analysis to the user. If violations are detected, propose adjustments.

### Step 5: Implementation Delegation

Based on the confirmed plan, delegate implementation tasks to specialist agents **in dependency order**:

**Order 1: Core and Infrastructure** (foundational changes)
- If new entities or value objects are needed:
  - Call **@ef-core-agent** to create entities with factory methods, value objects, repositories
  - Call **@ef-core-agent** to create migrations with owned type configuration
  - Verify migrations can be applied without errors

**Order 2: API Layer** (business logic exposure)
- If API endpoints are needed:
  - Call **@aspnet-api-agent** to create controllers, DTOs, AutoMapper profiles
  - Verify endpoints follow REST conventions and authentication patterns

**Order 3: Bot Layer** (Telegram integration)
- If bot handlers are needed:
  - Call **@telegram-bot-handler-agent** to create handlers, update conversation state
  - Verify bot commands follow existing patterns

**Order 4: Frontend Layer** (UI components)
- If React components are needed:
  - Call **@frontend-admin-agent** to create components, hooks, API integration
  - Verify TypeScript types match API contracts

**Coordination Notes**:
- Run independent tasks in parallel when possible
- Wait for foundational layers (Core, Infrastructure) before dependent layers (API, Bot)
- Verify each layer's output before proceeding to the next

### Step 6: Test Creation

Call **@dotnet-testing-agent** with:
- Implemented features from Step 5
- Test coverage requirements (unit, integration, end-to-end)

**Expected Output**:
- Unit tests for new services, handlers, value objects
- Integration tests for API endpoints, database interactions
- Bot flow tests if applicable
- Test coverage report

Verify tests pass and coverage meets project standards (typically 80%+).

### Step 7: Documentation Updates

Call **@claude-md-documentation-agent** with:
- Feature description and implementation summary
- Affected files and layers
- New API endpoints, entities, or bot commands

**Expected Output**:
- Updated layer CLAUDE.md files (Core, Infrastructure, API, Bot)
- Updated main CLAUDE.md with version notes
- Updated API reference documentation
- User guide updates if user-facing changes

### Step 8: Verification and Summary

Perform final verification:
- All tests pass (`dotnet test`)
- Migrations apply successfully (`dotnet ef database update`)
- API endpoints return expected responses (Swagger or manual testing)
- Bot handlers respond correctly (if applicable)
- Documentation is complete and accurate

Present a summary to the user:
- Feature implementation status (complete, partial, blocked)
- Files created or modified (with absolute paths)
- Tests created and coverage metrics
- Documentation updates
- Next steps or follow-up tasks

---

## Clarifying Questions Strategy

This command uses **adaptive batching** for clarifying questions:

**Batch 1 (Scope and Priority)**: 3-5 questions about feature scope, affected layers, and priority
- Asked immediately after parsing $ARGUMENTS
- Required unless user explicitly says "skip questions"

**Batch 2 (Technical Details)**: 2-3 questions about DDD patterns, performance, compatibility
- Asked only if Batch 1 reveals complexity (multiple layers, new entities)
- Can be skipped if user requests fast path

**Fast Path Option**:
- User can say "use defaults" or "skip questions"
- Command makes conservative assumptions based on SurveyBot v1.6.2 architecture
- Assumptions documented in Step 8 summary

**Clarification Timing**:
- Before calling @feature-planning-agent (Step 3)
- After architectural analysis if violations detected (Step 4)
- After each implementation phase if errors occur (Step 5)

---

## Out of Scope

This command **does not**:

- Deploy features to production (use deployment commands instead)
- Modify database schemas directly (only creates migrations)
- Implement features outside SurveyBot project scope
- Bypass architectural validation or DDD patterns
- Create features without tests or documentation
- Make breaking changes without user confirmation
- Implement features that violate Clean Architecture or layer boundaries

**For Out-of-Scope Tasks**:
- Production deployment: Use CI/CD pipeline or manual deployment guides
- Hotfixes: Use `/fix-bug` command for urgent production issues
- Architectural refactoring: Use `/refactor-architecture` command
- Database migrations only: Use @ef-core-agent directly

---

## Example Invocations

### Example 1: Full-Stack Feature

```
/add-feature Add survey templates feature allowing users to save survey structures as templates and reuse them for new surveys
```

**Expected Flow**:
1. Clarify: Template scope (questions only, or include options/flow?), storage (new entity or survey flag?), UI (bot, admin panel, or both?)
2. Plan: Create SurveyTemplate entity, TemplateService, API endpoints, bot commands
3. Analyze: Validate entity relationships, aggregate boundaries, value objects needed
4. Implement: @ef-core-agent (entity + migration) → @aspnet-api-agent (endpoints) → @telegram-bot-handler-agent (commands) → @frontend-admin-agent (UI)
5. Test: Unit tests for TemplateService, integration tests for API, bot flow tests
6. Document: Update Core, Infrastructure, API, Bot CLAUDE.md files

### Example 2: API-Only Feature

```
/add-feature Implement bulk survey activation API endpoint to activate multiple surveys at once
```

**Expected Flow**:
1. Clarify: Validation rules (max surveys per request?), error handling (partial success?), authorization
2. Plan: Create BulkActivateSurveysDto, add endpoint to SurveysController
3. Analyze: Validate no architectural violations, transaction handling
4. Implement: @aspnet-api-agent (controller method, DTO, AutoMapper)
5. Test: Integration tests for bulk operations, error scenarios
6. Document: Update API CLAUDE.md and API reference

### Example 3: Bot-Only Feature

```
/add-feature Add /export command to bot allowing users to download their survey responses as CSV
```

**Expected Flow**:
1. Clarify: CSV format (columns, encoding), delivery method (file upload, link), file size limits
2. Plan: Create ExportCommandHandler, CSV generation service, file storage integration
3. Analyze: Validate bot command patterns, file storage strategy
4. Implement: @telegram-bot-handler-agent (command handler, CSV generation)
5. Test: Bot command tests, CSV format validation
6. Document: Update Bot CLAUDE.md, bot command reference

---

## Success Criteria

A feature is considered **successfully implemented** when:

1. All planned tasks from @feature-planning-agent are completed
2. Architectural analysis from @architecture-deep-dive-agent shows no violations
3. All specialist agents report successful implementation
4. All tests pass with adequate coverage (80%+ recommended)
5. Migrations apply without errors (if applicable)
6. Documentation is updated in all affected layers
7. User confirms feature meets requirements

If any criterion fails, the command should:
- Identify the blocking issue
- Propose a resolution plan
- Ask user whether to proceed, adjust, or pause

---

## Assumptions (When User Skips Questions)

If the user requests to proceed without clarifying questions, the command assumes:

**Scope Assumptions**:
- Feature affects all relevant layers (if unclear, implement full-stack)
- Backward compatibility must be maintained
- No breaking changes to existing APIs or entities

**Implementation Assumptions**:
- Follow existing SurveyBot v1.6.2 patterns (factory methods, value objects, private setters)
- Use standard authentication (JWT Bearer for API, Telegram user ID for bot)
- Standard error handling (domain exceptions, global exception middleware)

**Testing Assumptions**:
- Minimum 80% code coverage
- Unit tests for services and value objects
- Integration tests for API endpoints and database interactions

**Documentation Assumptions**:
- Update all affected layer CLAUDE.md files
- Add version notes to main CLAUDE.md
- Update API reference if endpoints added

These assumptions will be clearly stated in the final summary (Step 8).

---

## Notes

**Model Selection**: This command uses `sonnet` for complex multi-agent orchestration, architectural analysis, and adaptive workflow management.

**v1.6.2 Context**: This command is designed for SurveyBot v1.6.2 with full awareness of:
- DDD patterns (private setters, factory methods, value objects)
- Clean Architecture layer boundaries
- Owned type configuration in EF Core migrations
- AnswerValue polymorphic hierarchy
- NextQuestionDeterminant value object
- Survey version tracking and cache invalidation

**Parallel Execution**: The command runs independent agent calls in parallel when possible (e.g., API and Bot implementation if no dependencies).

**Error Recovery**: If any agent fails, the command pauses, reports the error, and asks user how to proceed (retry, skip, abort).

**Incremental Implementation**: For large features, the command can be run multiple times with different scopes (e.g., `/add-feature [Phase 1: API only]`, then `/add-feature [Phase 2: Bot integration]`).
