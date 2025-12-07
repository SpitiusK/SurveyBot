---
name: update-docs
description: Documentation synchronization command that coordinates @claude-md-documentation-agent and @architecture-deep-dive-agent to update layer-specific CLAUDE.md files and centralized documentation after code changes. Validates consistency across all documentation files in SurveyBot v1.6.2.
model: sonnet
color: blue
---

# Update Documentation Command

## Command Overview

The `/update-docs` command orchestrates comprehensive documentation synchronization across the SurveyBot v1.6.2 project after code changes. It coordinates two specialist agents:

- **@claude-md-documentation-agent**: Updates layer-specific CLAUDE.md files and centralized documentation
- **@architecture-deep-dive-agent**: Analyzes code changes to understand architectural impact

This command ensures documentation remains synchronized with code across all layers (Core, Infrastructure, Bot, API, Frontend) and the centralized `documentation/` folder.

**How $ARGUMENTS is used**: The command captures the initial documentation update context from $ARGUMENTS (what changed, which components affected, scope of updates) and uses it throughout the workflow to guide clarifying questions, agent coordination, and documentation synchronization.

---

## When to Use This Command

Use `/update-docs` in these situations:

- After implementing new features (e.g., new question types, API endpoints, bot commands)
- After architecture changes (e.g., new design patterns, layer modifications, value objects)
- After bug fixes that affect documented behavior
- After API contract changes (endpoints, DTOs, request/response formats)
- After database schema changes (migrations, entity modifications)
- When documentation drift is detected between code and docs
- Before releasing a new version to ensure documentation accuracy
- When adding new components that need documentation

---

## Agents and Responsibilities

### Primary Agent: @claude-md-documentation-agent

**Responsibilities**:
- Updates layer-specific CLAUDE.md files (Core, Infrastructure, Bot, API, Frontend)
- Updates centralized documentation in `documentation/` subfolders
- Ensures version numbers and last-updated dates are correct
- Maintains documentation standards and formatting
- Cross-references related documentation
- Updates documentation index files

### Secondary Agent: @architecture-deep-dive-agent

**Responsibilities**:
- Analyzes code changes to understand architectural impact
- Identifies affected layers and components
- Traces dependencies between components
- Determines scope of documentation updates needed
- Provides context for documentation changes

---

## Interaction Workflow

### Step 1: Initial Context Capture

Parse the documentation update context from **$ARGUMENTS**:
- What code changes were made?
- Which components/layers are affected?
- What is the scope of documentation updates needed?
- Are there new features, bug fixes, or architecture changes?

### Step 2: Clarifying Questions (First Batch)

Ask 3-5 focused questions to understand the documentation scope:

**Example Questions**:
1. **Scope**: "Which layers are affected by the code changes? (Core, Infrastructure, Bot, API, Frontend, or multiple)"
2. **Type of Change**: "What type of change occurred? (new feature, bug fix, architecture refactor, API change, database change)"
3. **Documentation Target**: "Which documentation files need updates? (layer CLAUDE.md files, centralized docs in `documentation/`, or both)"
4. **Version Impact**: "Should the version number be updated? (patch, minor, major)"
5. **Breaking Changes**: "Are there any breaking changes that need to be documented?"

### Step 3: Architecture Analysis

Call **@architecture-deep-dive-agent** to analyze code changes:
- Provide context from $ARGUMENTS and user answers
- Request analysis of affected components and dependencies
- Ask for architectural impact assessment
- Get recommended documentation sections to update

**Output**: Architecture analysis report identifying:
- Affected layers and components
- Dependency changes
- Breaking changes
- New patterns or principles introduced
- Scope of documentation updates

### Step 4: Documentation Updates

Call **@claude-md-documentation-agent** to update documentation:
- Provide architecture analysis report
- Specify which documentation files to update
- Include version and breaking change information
- Request consistency validation across all docs

**Documentation Targets**:
- Root: `C:\Users\User\Desktop\SurveyBot\CLAUDE.md`
- Core Layer: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\CLAUDE.md`
- Infrastructure Layer: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Infrastructure\CLAUDE.md`
- Bot Layer: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\CLAUDE.md`
- API Layer: `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\CLAUDE.md`
- Frontend: `C:\Users\User\Desktop\SurveyBot\frontend\CLAUDE.md`
- Centralized Docs: `C:\Users\User\Desktop\SurveyBot\documentation\**\*.md`

### Step 5: Consistency Validation

Verify documentation consistency:
- Check cross-references between files are valid
- Ensure version numbers match across all files
- Validate that breaking changes are documented in all affected layers
- Confirm documentation index is updated
- Check that examples and code snippets are accurate

### Step 6: Summary and Confirmation

Present documentation update summary to user:
- List of files updated
- Key changes made to each file
- Version number updates
- Breaking changes documented
- Cross-reference updates
- Ask user to review and confirm changes

---

## Clarifying Questions Strategy

### First Batch (3-5 questions)

Focus on **scope and impact**:
- Which layers/components are affected?
- What type of change occurred?
- Are there breaking changes?
- Which documentation files need updates?
- Should version be updated?

### Second Batch (if needed, 2-3 questions)

Focus on **details and validation**:
- Are there new design patterns to document?
- Do API contracts need updates?
- Are there new dependencies or configuration changes?
- Should migration guides be updated?

### Fast Path Option

If user says "just update everything" or "minimal questions":
- Assume all affected layers need documentation updates
- Use architecture analysis to determine scope
- Make conservative assumptions (document all changes)
- Clearly state assumptions in summary

---

## Out of Scope

This command **does not**:

- Modify code or implementation files (only documentation)
- Create new features or fix bugs
- Run tests or verify code behavior
- Deploy to production or update live systems
- Generate API documentation from code (use dedicated tools)
- Translate documentation to other languages
- Create user-facing help content (unless explicitly requested)

For code changes, use `/task-execution-agent` or `/orchestrator-agent`.

For runtime diagnostics, use `/diagnose-runtime`.

---

## Example Invocations

### Example 1: New Feature Documentation

```
/update-docs Added QuestionType.File support with FileAnswerValue and FileUploadHandler - affects Core, Infrastructure, and Bot layers
```

**Expected Flow**:
1. Parse context: New QuestionType.File feature
2. Ask clarifying questions about scope and breaking changes
3. Call @architecture-deep-dive-agent to analyze FileAnswerValue implementation
4. Call @claude-md-documentation-agent to update Core, Infrastructure, Bot CLAUDE.md files
5. Update centralized docs (API reference, bot user guide)
6. Validate cross-references and examples
7. Present summary of documentation updates

### Example 2: Bug Fix Documentation

```
/update-docs Fixed BOT-FIX-003 - SurveyResponseHandler now validates date format before saving DateAnswerValue
```

**Expected Flow**:
1. Parse context: Bug fix in Bot layer affecting date validation
2. Ask if this changes documented behavior
3. Call @architecture-deep-dive-agent to understand validation flow
4. Call @claude-md-documentation-agent to update Bot CLAUDE.md and troubleshooting docs
5. Add to fixes documentation folder if significant
6. Present summary

### Example 3: Architecture Refactor Documentation

```
/update-docs Refactored ResponseService to use Strategy pattern for answer validation - affects Infrastructure layer
```

**Expected Flow**:
1. Parse context: Architecture change with new design pattern
2. Ask about breaking changes and public API impact
3. Call @architecture-deep-dive-agent to analyze Strategy pattern implementation
4. Call @claude-md-documentation-agent to update Infrastructure CLAUDE.md and architecture docs
5. Update design patterns section in root CLAUDE.md
6. Validate examples and references
7. Present summary with architecture diagram updates

### Example 4: Comprehensive Update

```
/update-docs Prepare documentation for v1.7.0 release - review all layers for accuracy
```

**Expected Flow**:
1. Parse context: Version release documentation review
2. Ask which features/changes are in v1.7.0
3. Call @architecture-deep-dive-agent to analyze changes since v1.6.2
4. Call @claude-md-documentation-agent to update all CLAUDE.md files and version numbers
5. Comprehensive consistency validation across all docs
6. Present full release documentation summary

---

## Assumptions (when minimal questions requested)

If user requests minimal questions or fast path:

1. **Scope Assumption**: Update all layer CLAUDE.md files mentioned in $ARGUMENTS
2. **Version Assumption**: Patch version bump (x.x.X) unless breaking changes detected
3. **Breaking Changes**: Document explicitly if detected by architecture analysis
4. **Centralized Docs**: Update relevant sections in `documentation/` based on layer changes
5. **Index Updates**: Always update documentation index when files are modified
6. **Cross-References**: Validate all cross-references in updated files
7. **Examples**: Update code examples to match current implementation
8. **Dates**: Set last-updated date to current date (2025-12-06)

These assumptions will be clearly stated in the summary for user confirmation.

---

## Documentation Standards

All documentation updates follow these standards:

### Formatting
- Use Markdown formatting consistently
- Include code blocks with language specifiers
- Use absolute file paths (not relative)
- Include table of contents for long documents

### Versioning
- Include version numbers in headers (e.g., "SurveyBot v1.6.2")
- Update "Last Updated" dates in YYYY-MM-DD format
- Document breaking changes in dedicated sections

### Cross-Referencing
- Use absolute paths in links: `[Core Layer](C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Core\CLAUDE.md)`
- Validate all links point to existing files
- Maintain bidirectional references where appropriate

### Content Standards
- Focus on "why" not just "what" for architecture decisions
- Include practical examples for complex concepts
- Document assumptions and constraints
- Provide troubleshooting guidance where relevant

---

## Success Criteria

Documentation update is successful when:

1. ✅ All affected layer CLAUDE.md files are updated with accurate information
2. ✅ Centralized documentation in `documentation/` reflects code changes
3. ✅ Version numbers are consistent across all files
4. ✅ Breaking changes are clearly documented with migration guidance
5. ✅ Cross-references are valid and bidirectional
6. ✅ Code examples match current implementation
7. ✅ Documentation index is updated with new/modified files
8. ✅ User confirms documentation changes are complete and accurate

---

## Integration with Other Commands

### Before This Command
- `/task-execution-agent` - Implement code changes first
- `/orchestrator-agent` - Complete feature implementation

### After This Command
- Git commit with updated documentation
- Version release preparation
- Notify team of documentation updates

### Related Commands
- `/diagnose-runtime` - May reveal undocumented behavior
- `/analyze-bug` - Bug fixes often require documentation updates

---

## Notes

- **Always use absolute paths** (e.g., `C:\Users\User\Desktop\SurveyBot\...`)
- **Never modify code** - this command is documentation-only
- **Validate before committing** - review all changes before git commit
- **Focus on accuracy** - incorrect documentation is worse than no documentation
- **SurveyBot-specific** - tailored for v1.6.2 architecture and layer structure
