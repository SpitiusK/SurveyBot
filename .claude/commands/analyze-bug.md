---
name: analyze-bug
description: Deep bug analysis using codebase-analyzer and architecture-deep-dive-agent to produce a comprehensive Bug Analysis Report. Does NOT fix bugs - only analyzes root causes, impact, and fix strategies. Use this before /fix-bug.
model: sonnet
color: orange
---

# Bug Analysis Command

## Command Overview

This command performs **deep bug analysis** without making any code changes. It coordinates analytical agents (@codebase-analyzer and @architecture-deep-dive-agent) to understand root causes, architectural implications, and impact scope, then produces a comprehensive Bug Analysis Report saved to `.claude/out/BUG_ANALYSIS_REPORT.md`.

**Single Responsibility**: Analysis only - no fixes, no code modifications.

**Workflow**: User describes bug → Analytical agents investigate → Report generated → User proceeds to /fix-bug

**SurveyBot v1.6.2 Context**: This command understands Clean Architecture layers (Core, Infrastructure, Bot, API, Frontend), DDD patterns (value objects, factory methods, private setters), and the conditional flow system (NextQuestionDeterminant, AnswerValue hierarchy).

## User Prompt

$ARGUMENTS

## When to Use This Command

Use `/analyze-bug` when:

- **Compilation errors** preventing build or deployment
- **Runtime exceptions** or crashes during operation (bot, API, frontend)
- **Logic bugs** producing incorrect results (survey flow, answer validation, statistics)
- **Performance issues** or unexpected slowness
- **Integration failures** between layers (Bot ↔ API, API ↔ Database)
- **Data inconsistencies** or validation failures
- **Unclear error root causes** requiring architectural understanding

**Do NOT use** for:
- Feature requests (use /task-execution-agent or /orchestrator-agent)
- Routine code improvements (use specialist agents directly)
- Documentation updates (use @claude-md-documentation-agent)

## Agents and Responsibilities

This command uses **analytical agents only** for deep investigation:

### 1. @codebase-analyzer
**Role**: Static code analysis, compilation errors, syntax issues, dependency problems

**Tasks**:
- Analyze compilation errors and build failures
- Detect syntax issues, missing imports, broken references
- Identify code smells and potential runtime issues
- Search for similar patterns across codebase
- Trace file dependencies and call chains

### 2. @architecture-deep-dive-agent
**Role**: Architectural understanding, component interactions, data flow analysis, root cause identification

**Tasks**:
- Map component interactions across Clean Architecture layers
- Trace data flows (request → response, user input → database)
- Identify architectural root causes (layer violations, coupling issues)
- Assess impact scope across multiple components
- Understand value object and entity relationships
- Analyze conditional flow logic (NextQuestionDeterminant, AnswerValue)

**Note**: No specialist agents are invoked during analysis. Fixes happen in Phase 2 (/fix-bug).

## Interaction Workflow

### Step 1: Parse User Bug Description
- Extract bug symptoms from $ARGUMENTS
- Identify initial context: error messages, stack traces, reproduction steps
- Determine likely affected layer(s) based on user description

### Step 2: Ask Clarifying Questions (Batch 1)
Ask 3-5 focused questions to understand:

**Bug Context**:
- When does the bug occur? (During what user action or API call?)
- What is the expected behavior vs actual behavior?
- Are there error messages, stack traces, or logs available?

**Reproduction**:
- Can you reproduce the bug consistently?
- What are the exact steps to reproduce?

**Impact**:
- Is this blocking production, development, or testing?
- Which features or user flows are affected?

**Environment**:
- Which layer(s) seem affected? (Bot, API, Infrastructure, Core, Frontend)
- Is this a new bug or regression from a recent change?

### Step 3: Invoke @codebase-analyzer
Based on clarification answers, delegate to @codebase-analyzer:

**For compilation/syntax errors**:
```
@codebase-analyzer: Analyze compilation errors in [affected files].
Check for syntax issues, missing imports, broken references, and dependency problems.
Context: [User bug description]
```

**For runtime errors**:
```
@codebase-analyzer: Search for [error pattern] across the codebase.
Identify all occurrences, trace call chains, and find similar patterns.
Context: [Stack trace or error message]
```

**For logic bugs**:
```
@codebase-analyzer: Analyze [affected component] for potential logic issues.
Focus on [specific behavior]. Check for code smells and edge case handling.
Context: Expected behavior: [X], Actual behavior: [Y]
```

### Step 4: Invoke @architecture-deep-dive-agent
Delegate architectural analysis based on bug scope:

**For cross-layer issues**:
```
@architecture-deep-dive-agent: Analyze component interactions between [Layer A] and [Layer B].
Trace data flow from [entry point] to [exit point].
Identify architectural root causes for [bug symptom].
Context: [User bug description + codebase-analyzer findings]
```

**For entity/value object issues**:
```
@architecture-deep-dive-agent: Analyze [Entity/Value Object] relationships and lifecycle.
Focus on factory methods, private setters, and modification patterns.
Identify violations of DDD encapsulation principles.
Context: [Bug description]
```

**For conditional flow bugs**:
```
@architecture-deep-dive-agent: Trace conditional flow logic for [survey/question scenario].
Analyze NextQuestionDeterminant value object usage, cycle detection, and navigation.
Map data flow from user response → ResponseService → SurveyNavigationHelper.
Context: [Flow bug description]
```

### Step 5: Aggregate Findings
Synthesize outputs from both agents:

- **Root Cause**: Combine static analysis findings with architectural understanding
- **Affected Components**: List all files, classes, methods involved
- **Impact Scope**: Determine blast radius across layers
- **Data Flow**: Trace the bug through component interactions
- **Risk Assessment**: Evaluate severity and urgency

### Step 6: Draft Fix Strategy
Based on aggregated findings, outline recommended fix approach:

**Fix Strategy Should Include**:
- **Approach**: High-level fix description (e.g., "Add null check", "Fix value object mapping", "Correct DFS cycle detection")
- **Affected Files**: Specific files requiring changes
- **Agent Assignment**: Which specialist agent(s) should handle the fix in Phase 2
- **Risks**: Potential side effects or breaking changes
- **Alternatives**: Other possible fix approaches if applicable

**v1.6.2 Pattern Compliance**:
- If entity modification needed: Use setter methods, not direct property assignment
- If entity creation needed: Use factory methods (Survey.Create(), Answer.CreateWithValue())
- If value object changes needed: Ensure immutability and value semantics
- If migration needed: Document owned type configuration changes

### Step 7: Determine Test Requirements
Specify tests needed to verify the fix:

- **Regression Tests**: Tests to prevent this bug from recurring
- **Unit Tests**: New or modified unit tests for affected components
- **Integration Tests**: Cross-layer tests if multiple layers affected
- **Manual Testing**: Steps for manual verification

### Step 8: Generate Bug Analysis Report
Create a comprehensive markdown report at `.claude/out/BUG_ANALYSIS_REPORT.md`:

**Report Structure**:

```markdown
# Bug Analysis Report

**Generated**: [Timestamp]
**Bug ID**: [Short identifier based on bug type]
**Severity**: [Critical/High/Medium/Low]
**Status**: Analysis Complete - Ready for Fix Phase

## Bug Summary

[2-3 sentence summary of the bug]

## Reproduction Steps

1. [Step 1]
2. [Step 2]
3. [Expected: X, Actual: Y]

## Root Cause Analysis

### Static Analysis Findings
[Output from @codebase-analyzer]

### Architectural Analysis
[Output from @architecture-deep-dive-agent]

### Root Cause Conclusion
[Synthesized root cause explanation]

## Affected Files and Components

| File Path | Component | Change Type | Risk Level |
|-----------|-----------|-------------|------------|
| [Absolute path] | [Class/Method] | [Add/Modify/Delete] | [High/Med/Low] |

## Impact Assessment

**Affected Layers**: [Core/Infrastructure/Bot/API/Frontend]

**Affected Features**: [List of features impacted]

**User Impact**: [Description of user-facing effects]

**Severity Justification**: [Why this severity level]

## Recommended Fix Strategy

### Approach
[Detailed fix description]

### Implementation Steps
1. [Step 1]
2. [Step 2]

### Specialist Agent Assignment
- **Primary Agent**: @[agent-name] (for [reason])
- **Secondary Agent**: @[agent-name] (for [reason])
- **Testing Agent**: @dotnet-testing-agent (for regression tests)

### v1.6.2 Pattern Compliance
- [Checklist of DDD patterns to follow]
- [Factory method usage]
- [Value object considerations]
- [Migration requirements if database affected]

### Alternatives Considered
- **Alternative 1**: [Description] - [Pros/Cons]
- **Alternative 2**: [Description] - [Pros/Cons]

## Risk Assessment

**Fix Complexity**: [Simple/Moderate/Complex]

**Breaking Change Risk**: [None/Low/Medium/High]

**Regression Risk**: [Areas that might break]

**Deployment Impact**: [Migration needed? API changes? Breaking changes?]

## Test Requirements

### Unit Tests
- [Test case 1]
- [Test case 2]

### Integration Tests
- [Test case 1]
- [Test case 2]

### Manual Testing Checklist
- [ ] [Manual test step 1]
- [ ] [Manual test step 2]

## Next Steps

1. Review this analysis report
2. Confirm fix strategy with user (if needed)
3. Proceed to `/fix-bug` command to implement the fix
4. Run tests and verify fix
5. Update documentation if needed

## References

**Related Files**: [Links to key files]

**Related Documentation**: [Links to CLAUDE.md sections or documentation/]

**Similar Issues**: [Past bugs or patterns]

---

**Analysis performed by**: @codebase-analyzer, @architecture-deep-dive-agent
**Ready for Fix Phase**: Yes
```

### Step 9: Present Report Summary to User
After saving the report, provide a concise summary:

**Summary Format**:
```
✅ Bug Analysis Complete

**Root Cause**: [One-sentence explanation]

**Affected Components**: [Layer(s) and key files]

**Severity**: [Level] - [Justification]

**Fix Strategy**: [High-level approach]

**Next Step**: Review the full report at .claude/out/BUG_ANALYSIS_REPORT.md, then run `/fix-bug` to implement the fix.

**Estimated Fix Complexity**: [Simple/Moderate/Complex]
```

Ask user:
- "Does this analysis align with your understanding of the bug?"
- "Should I proceed with the recommended fix strategy, or would you like to discuss alternatives?"

If user confirms, suggest: "Run `/fix-bug` to implement the fix based on this analysis."

## Clarifying Questions Strategy

**Batch 1 (Always Asked)**: 3-5 questions about bug context, reproduction, impact, environment (see Step 2)

**Batch 2 (Conditional)**: Asked only if Batch 1 answers are vague or incomplete
- "Can you provide the exact error message or stack trace?"
- "Which specific survey/question/feature triggers this bug?"
- "What changed recently that might have caused this?"

**Fast Path**: If user provides comprehensive bug description with error logs and reproduction steps in $ARGUMENTS, skip most clarifications and proceed directly to agent delegation.

**Conservative Assumptions**: If user wants minimal questions:
- Assume bug affects the layer mentioned in error stack trace
- Assume standard SurveyBot v1.6.2 architecture
- Document assumptions in "Assumptions" section of report

## Out of Scope

This command does **NOT**:

- **Fix bugs** - that's Phase 2 (/fix-bug)
- **Modify code** - analysis only
- **Run tests** - test execution happens in /fix-bug
- **Deploy changes** - no deployment actions
- **Update documentation** - documentation updates happen in /fix-bug if needed
- **Handle feature requests** - use /task-execution-agent instead
- **Replace specialist agents** - this is orchestration, not implementation

## Example Invocations

### Example 1: Compilation Error
```
/analyze-bug Getting compilation error in SurveyMappingProfile.cs after adding new AnswerValue type. Build fails with "cannot convert from TextAnswerValue to string"
```

**Expected Flow**:
1. Ask about which mapping specifically fails
2. @codebase-analyzer: Check AutoMapper profile configurations
3. @architecture-deep-dive-agent: Analyze AnswerValue polymorphic hierarchy and mapping strategy
4. Generate report with fix strategy: Update AutoMapper profile to handle AnswerValue → string conversion

### Example 2: Runtime Logic Bug
```
/analyze-bug Survey ends prematurely when non-branching questions have EndSurvey configured. Rating questions ignore EndSurvey and go to next question sequentially.
```

**Expected Flow**:
1. Ask for reproduction steps and specific survey configuration
2. @codebase-analyzer: Search for EndSurvey handling in ResponseService
3. @architecture-deep-dive-agent: Trace DetermineNonBranchingNextStepAsync logic and NextQuestionDeterminant usage
4. Generate report identifying missing EndSurvey check before sequential fallback (similar to INFRA-FIX-001)

### Example 3: Integration Issue
```
/analyze-bug Bot shows "Survey Updated" alert when user clicks survey button, but admin hasn't changed anything. Happening after every button click.
```

**Expected Flow**:
1. Ask when this started happening and reproduction frequency
2. @codebase-analyzer: Search for version comparison logic in SurveyResponseHandler
3. @architecture-deep-dive-agent: Trace cache invalidation flow between SurveyCommandHandler and SurveyResponseHandler
4. Generate report identifying cache invalidation timing issue (similar to BOT-FIX-001)

### Example 4: Database/Entity Issue
```
/analyze-bug Getting EF Core exception "The instance of entity type 'Question' cannot be tracked" when updating survey with new questions.
```

**Expected Flow**:
1. Ask for full stack trace and entity modification code
2. @codebase-analyzer: Check entity tracking in repository methods
3. @architecture-deep-dive-agent: Analyze factory method usage, private setters, and EF change tracking
4. Generate report with fix strategy for proper entity attachment and tracking

## Success Criteria

This command succeeds when:

1. ✅ Bug Analysis Report generated at `.claude/out/BUG_ANALYSIS_REPORT.md`
2. ✅ Root cause identified with high confidence
3. ✅ All affected files and components documented
4. ✅ Clear fix strategy outlined with agent assignments
5. ✅ Test requirements specified
6. ✅ User confirms analysis accuracy
7. ✅ Ready to proceed to `/fix-bug` command

**Failure Conditions**:
- Cannot reproduce bug or identify root cause → Ask for more information
- Ambiguous architectural impact → Request more context or logs
- Multiple possible root causes → Document all in report, recommend diagnostic steps

## Notes

**SurveyBot v1.6.2 Awareness**: This command knows about:
- Clean Architecture layers and boundaries
- DDD patterns (private setters, factory methods, value objects)
- Conditional flow system (NextQuestionDeterminant, AnswerValue)
- Owned types and migrations
- Recent bug fixes (BOT-FIX-001, INFRA-FIX-001)

**Integration with Phase 2**: The Bug Analysis Report is the primary input for `/fix-bug`. Ensure report is comprehensive enough that `/fix-bug` can execute without re-analyzing.

**Report Location**: Always `.claude/out/BUG_ANALYSIS_REPORT.md` (consistent location for /fix-bug to find)

**Model Choice**: Sonnet for complex architectural analysis and multi-agent coordination.
