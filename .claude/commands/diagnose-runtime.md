---
name: diagnose-runtime
description: Runtime diagnostics command that coordinates @docker-log-analyzer and @codebase-analyzer to analyze Docker Compose logs, trace request flows, identify error patterns, and generate diagnostic reports without making code changes. Recommends follow-up actions including /analyze-bug for confirmed issues.
model: sonnet
color: orange
---

# Diagnose Runtime Command

## Command Overview

The `/diagnose-runtime` command orchestrates comprehensive runtime diagnostics for the SurveyBot v1.6.2 application using Docker Compose logs. It coordinates two specialist agents:

- **@docker-log-analyzer**: Analyzes Docker container logs to identify errors, warnings, and patterns
- **@codebase-analyzer**: Traces errors from logs to specific code locations and components

This command focuses on **diagnosis only** - it does not make code changes. It generates a detailed diagnostic report and recommends next steps, including using `/analyze-bug` if bugs are confirmed.

**How $ARGUMENTS is used**: The command captures the initial runtime issue description from $ARGUMENTS (symptoms, error messages, timing, affected features) and uses it throughout the workflow to guide log analysis, error tracing, and diagnostic report generation.

---

## When to Use This Command

Use `/diagnose-runtime` in these situations:

- Application is throwing errors or exceptions at runtime
- Users report unexpected behavior or crashes
- Telegram bot is not responding or behaving incorrectly
- API endpoints are returning 500 errors or timing out
- Database connection issues or query performance problems
- Performance degradation or high resource usage
- Intermittent failures that are hard to reproduce
- After deploying new changes to verify runtime behavior
- When error logs show warnings or exceptions
- To trace request flows across multiple containers (API, Bot, PostgreSQL)

---

## Agents and Responsibilities

### Primary Agent: @docker-log-analyzer

**Responsibilities**:
- Analyzes Docker Compose logs from all containers (API, Bot, PostgreSQL, pgAdmin)
- Identifies error patterns, exceptions, and warnings
- Traces request flows across container boundaries
- Detects performance issues and resource constraints
- Extracts relevant log entries for specific time windows or error types
- Provides log-based evidence for diagnostic conclusions

### Secondary Agent: @codebase-analyzer

**Responsibilities**:
- Traces errors from log messages to specific code files and line numbers
- Identifies affected components and layers (Core, Infrastructure, Bot, API)
- Analyzes error context (stack traces, method calls, database queries)
- Determines root cause categories (configuration, logic error, data issue)
- Provides code context for error locations
- Recommends which code files need investigation

---

## Interaction Workflow

### Step 1: Initial Issue Description

Parse the runtime issue description from **$ARGUMENTS**:
- What symptoms are being observed?
- Are there specific error messages or exceptions?
- When did the issue start? (timing, frequency)
- Which features or flows are affected?
- Is the issue reproducible or intermittent?

### Step 2: Clarifying Questions (First Batch)

Ask 3-5 focused questions to understand the runtime issue:

**Example Questions**:
1. **Symptoms**: "What specific behavior are you observing? (errors, crashes, timeouts, incorrect responses, performance issues)"
2. **Scope**: "Which component is affected? (Telegram bot, API endpoints, database, specific feature)"
3. **Timing**: "When does the issue occur? (always, intermittently, during specific operations, after specific actions)"
4. **Reproducibility**: "Can you reproduce the issue consistently? If so, what are the exact steps?"
5. **Recent Changes**: "Were there recent code changes, deployments, or configuration updates before the issue started?"

### Step 3: Docker Log Analysis

Call **@docker-log-analyzer** to analyze container logs:
- Provide runtime issue context from $ARGUMENTS and user answers
- Specify which containers to analyze (surveybot-api, surveybot-postgres, or all)
- Request error pattern identification
- Ask for request flow tracing if relevant
- Specify time window if issue is time-specific

**Analysis Focus Areas**:
- Exception stack traces and error messages
- Warning messages that may indicate underlying issues
- Database query errors or connection issues
- Request/response flows across containers
- Performance metrics (response times, resource usage)
- Temporal patterns (errors at specific times, frequency)

**Output**: Log analysis report containing:
- Relevant log entries with timestamps
- Identified error patterns and exceptions
- Request flow traces
- Performance observations
- Temporal analysis (when errors occur)

### Step 4: Code Tracing

Call **@codebase-analyzer** to trace errors to code:
- Provide log analysis report from Step 3
- Request mapping of errors to code files and line numbers
- Ask for affected component identification
- Request root cause category analysis
- Get code context for error locations

**Analysis Focus Areas**:
- Map stack traces to code files (absolute paths)
- Identify affected layers (Core, Infrastructure, Bot, API)
- Trace error propagation paths
- Analyze error handling logic
- Identify missing validations or edge cases
- Determine if issue is code, configuration, or data

**Output**: Code tracing report containing:
- Absolute file paths to affected code
- Line numbers and method names
- Affected layers and components
- Root cause category (logic error, configuration, data issue, missing validation)
- Code context around error locations

### Step 5: Diagnostic Report Generation

Synthesize findings into comprehensive diagnostic report:

**Report Structure**:
1. **Issue Summary**: Description of runtime issue from $ARGUMENTS
2. **Symptoms**: Observable behavior and error messages
3. **Log Analysis Findings**: Key findings from @docker-log-analyzer
4. **Code Tracing Results**: Affected code locations from @codebase-analyzer
5. **Root Cause Assessment**: Likely root cause category and explanation
6. **Affected Components**: Layers and components impacted
7. **Severity**: Critical, High, Medium, Low
8. **Reproducibility**: Consistent, Intermittent, Rare
9. **Recommended Actions**: Next steps for resolution

**Report Location**: `C:\Users\User\Desktop\SurveyBot\.claude\agents\out\RUNTIME_DIAGNOSTIC_REPORT.md`

### Step 6: Recommendations and Next Steps

Provide actionable recommendations based on diagnostic findings:

**If Bug Confirmed**:
- "Confirmed bug in [component]. Recommend running `/analyze-bug [issue description]` to create fix plan."
- Provide specific file paths and line numbers for bug fix

**If Configuration Issue**:
- "Issue appears to be configuration-related. Check [specific config file or setting]."
- Provide recommended configuration changes

**If Data Issue**:
- "Issue is caused by invalid data in database. Recommend data cleanup or validation."
- Provide specific data records or tables to investigate

**If Performance Issue**:
- "Performance bottleneck identified in [component]. Recommend optimization."
- Provide specific queries or code sections to optimize

**If Needs More Information**:
- "Unable to determine root cause from logs. Recommend enabling detailed logging or adding instrumentation."
- Provide specific logging additions needed

---

## Clarifying Questions Strategy

### First Batch (3-5 questions)

Focus on **symptoms and context**:
- What specific behavior is observed?
- Which component/feature is affected?
- When does the issue occur? (timing, frequency)
- Is it reproducible? If so, what are the steps?
- Were there recent changes before the issue started?

### Second Batch (if needed, 2-3 questions)

Focus on **details and environment**:
- What is the error message or exception type?
- Are there any specific user actions that trigger it?
- Is the issue environment-specific? (development, production)
- Are there any workarounds that avoid the issue?

### Fast Path Option

If user says "analyze all logs" or "find the error":
- Analyze logs from last 24 hours for all containers
- Focus on ERROR and WARN level messages
- Trace all exceptions to code
- Make conservative assumptions about scope
- Clearly state assumptions in diagnostic report

---

## Out of Scope

This command **does not**:

- Modify code or fix bugs (use `/analyze-bug` or `/task-execution-agent` for fixes)
- Change configuration files (only recommends changes)
- Restart containers or services
- Deploy fixes to production
- Run automated tests (use testing agents for that)
- Modify database data (only identifies data issues)
- Implement performance optimizations (only identifies bottlenecks)

This command is **read-only diagnostics**. For fixes, use the recommended follow-up commands.

---

## Example Invocations

### Example 1: Bot Not Responding

```
/diagnose-runtime Telegram bot is not responding to /start command - users see no reply
```

**Expected Flow**:
1. Parse context: Bot not responding to /start command
2. Ask clarifying questions:
   - When did this start?
   - Is it affecting all users or specific users?
   - Are other commands working?
3. Call @docker-log-analyzer to analyze surveybot-api logs for bot-related errors
4. Call @codebase-analyzer to trace errors to StartCommandHandler code
5. Generate diagnostic report with findings
6. Recommend action: "Configuration issue detected - bot token may be invalid. Check appsettings.Development.json BotConfiguration.BotToken"

### Example 2: API 500 Errors

```
/diagnose-runtime API endpoint POST /api/surveys returning 500 Internal Server Error
```

**Expected Flow**:
1. Parse context: API 500 error on specific endpoint
2. Ask clarifying questions:
   - What request payload is being sent?
   - Is this happening for all POST /api/surveys or specific cases?
   - When did this start?
3. Call @docker-log-analyzer to analyze API logs for exception stack traces
4. Call @codebase-analyzer to trace exception to SurveyController and SurveyService
5. Generate diagnostic report with stack trace and code location
6. Recommend action: "Bug confirmed in SurveyService.CreateSurveyAsync - validation error. Run `/analyze-bug Survey creation fails with NullReferenceException in validation`"

### Example 3: Database Connection Issues

```
/diagnose-runtime Application logs show "connection pool exhausted" errors intermittently
```

**Expected Flow**:
1. Parse context: Database connection pool exhaustion
2. Ask clarifying questions:
   - How frequently does this occur?
   - Are there specific operations that trigger it?
   - What is the current connection pool configuration?
3. Call @docker-log-analyzer to analyze PostgreSQL and API logs for connection patterns
4. Call @codebase-analyzer to check DbContext configuration and disposal patterns
5. Generate diagnostic report with connection pool metrics
6. Recommend action: "Configuration issue - MaxPoolSize too low for current load. Recommend increasing MaxPoolSize in connection string from 100 to 200"

### Example 4: Performance Degradation

```
/diagnose-runtime Survey response submission is taking 10+ seconds, used to be instant
```

**Expected Flow**:
1. Parse context: Performance degradation in survey response submission
2. Ask clarifying questions:
   - When did performance degrade?
   - Is it affecting all surveys or specific ones?
   - How many questions are in the affected surveys?
3. Call @docker-log-analyzer to analyze response times and database query logs
4. Call @codebase-analyzer to analyze ResponseService and database query patterns
5. Generate diagnostic report with performance bottleneck identification
6. Recommend action: "Performance issue - missing database index on responses.survey_id. Recommend adding index or running optimization"

### Example 5: Intermittent Failures

```
/diagnose-runtime Users report bot sometimes shows "Failed to save your answer" but it works other times
```

**Expected Flow**:
1. Parse context: Intermittent save failures in bot
2. Ask clarifying questions:
   - What question types are affected?
   - Is there a pattern to when it fails (time of day, specific surveys)?
   - Do you have example survey IDs where this occurs?
3. Call @docker-log-analyzer to analyze bot logs for save failures and patterns
4. Call @codebase-analyzer to trace save logic in SurveyResponseHandler
5. Generate diagnostic report with intermittent failure analysis
6. Recommend action: "Bug confirmed - race condition in cache invalidation. Run `/analyze-bug Intermittent answer save failures due to cache race condition in SurveyResponseHandler`"

---

## Diagnostic Report Template

The generated diagnostic report follows this structure:

```markdown
# Runtime Diagnostic Report

**Generated**: 2025-12-06 10:30:00
**Issue**: [Brief description from $ARGUMENTS]
**Severity**: [Critical|High|Medium|Low]
**Reproducibility**: [Consistent|Intermittent|Rare]

---

## 1. Issue Summary

[Detailed description of runtime issue from $ARGUMENTS and user clarifications]

---

## 2. Symptoms

- [Observable behavior 1]
- [Observable behavior 2]
- [Error messages or exceptions observed]

---

## 3. Log Analysis Findings

**Analyzed Containers**: [surveybot-api, surveybot-postgres, etc.]
**Time Window**: [e.g., Last 24 hours, 2025-12-06 08:00 to 10:00]

### Key Log Entries

```
[Timestamp] [Container] [Level] [Message]
[Relevant log entries from @docker-log-analyzer]
```

### Error Patterns Identified

- [Pattern 1]: Occurs [frequency] - [description]
- [Pattern 2]: Occurs [frequency] - [description]

### Request Flow Trace

[If applicable, trace of request flow across containers]

---

## 4. Code Tracing Results

**Affected Files**:
- `C:\Users\User\Desktop\SurveyBot\[file path]` (Line [X])
- `C:\Users\User\Desktop\SurveyBot\[file path]` (Line [Y])

**Affected Layers**:
- [Core|Infrastructure|Bot|API]

**Stack Trace Analysis**:
[Code context from @codebase-analyzer]

---

## 5. Root Cause Assessment

**Category**: [Logic Error|Configuration Issue|Data Issue|Performance Bottleneck|Missing Validation]

**Root Cause**:
[Detailed explanation of likely root cause based on log analysis and code tracing]

**Evidence**:
- [Evidence 1 from logs]
- [Evidence 2 from code analysis]

---

## 6. Affected Components

- **Layer**: [Core|Infrastructure|Bot|API]
- **Component**: [Specific service, controller, handler]
- **Dependencies**: [Related components that may be affected]

---

## 7. Recommended Actions

### Immediate Actions

1. [Action 1 - e.g., "Run `/analyze-bug [description]` to create fix plan"]
2. [Action 2 - e.g., "Update configuration in appsettings.Development.json"]

### Follow-Up Actions

1. [Action 1 - e.g., "Add monitoring for connection pool usage"]
2. [Action 2 - e.g., "Add unit tests for edge case"]

### Related Commands

- `/analyze-bug [description]` - If bug fix is needed
- `/update-docs` - After fix is implemented
- `/task-execution-agent` - For complex fixes requiring multiple changes

---

## 8. Additional Notes

[Any additional context, observations, or caveats]

---

**Diagnostic Command**: `/diagnose-runtime`
**Report Location**: `C:\Users\User\Desktop\SurveyBot\.claude\agents\out\RUNTIME_DIAGNOSTIC_REPORT.md`
```

---

## Assumptions (when minimal questions requested)

If user requests minimal questions or fast path:

1. **Time Window**: Analyze logs from last 24 hours
2. **Containers**: Analyze all containers (API, Bot, PostgreSQL)
3. **Log Level**: Focus on ERROR and WARN levels, include INFO for context
4. **Scope**: Assume issue affects primary application flow (not edge cases)
5. **Environment**: Assume development environment unless specified
6. **Reproducibility**: Assume consistent unless intermittent pattern is obvious
7. **Severity**: Assess based on error frequency and impact
8. **Code Tracing**: Trace all exceptions found in logs to code

These assumptions will be clearly stated in the diagnostic report.

---

## Success Criteria

Runtime diagnostics is successful when:

1. ✅ Docker logs are analyzed for relevant time window and containers
2. ✅ Error patterns and exceptions are identified
3. ✅ Errors are traced to specific code files and line numbers
4. ✅ Root cause category is determined (logic error, config, data, performance)
5. ✅ Affected components and layers are identified
6. ✅ Diagnostic report is generated and saved
7. ✅ Actionable recommendations are provided
8. ✅ User understands next steps (follow-up commands or configuration changes)

---

## Integration with Other Commands

### Before This Command
- Issue is reported or observed at runtime
- Application is running in Docker containers
- Logs are being generated

### After This Command
- **If bug confirmed**: Run `/analyze-bug [description]` to create fix plan
- **If fix implemented**: Run `/update-docs` to update documentation
- **If configuration change**: Update relevant configuration files manually
- **If data issue**: Investigate database records or run data cleanup

### Related Commands
- `/analyze-bug` - Create fix plan for confirmed bugs
- `/task-execution-agent` - Implement complex fixes
- `/orchestrator-agent` - Coordinate multi-step fixes
- `/update-docs` - Document fixes and behavior changes

---

## Docker Log Analysis Tips

### Accessing Docker Logs

```bash
# View logs for specific container
docker logs surveybot-api
docker logs surveybot-postgres

# View logs with timestamps
docker logs --timestamps surveybot-api

# View logs for last hour
docker logs --since 1h surveybot-api

# Follow logs in real-time
docker logs -f surveybot-api

# View all container logs
docker-compose logs

# View logs for specific time window
docker logs --since "2025-12-06T08:00:00" --until "2025-12-06T10:00:00" surveybot-api
```

### Log Patterns to Look For

**Errors and Exceptions**:
- `[ERR]` or `ERROR` level messages
- Stack traces with exception types
- `NullReferenceException`, `InvalidOperationException`, `DbUpdateException`

**Performance Issues**:
- `Executed DbCommand` with high duration (>1000ms)
- Connection pool warnings
- Timeout messages

**Configuration Issues**:
- "Configuration not found"
- "Invalid connection string"
- "Bot token invalid"

**Database Issues**:
- "Cannot connect to database"
- "Foreign key constraint violation"
- "Deadlock detected"

---

## Notes

- **Read-only command** - does not modify code, configuration, or data
- **Always use absolute paths** in diagnostic reports
- **Focus on evidence** - base conclusions on log entries and code analysis
- **Recommend follow-up** - always provide next steps for resolution
- **SurveyBot-specific** - tailored for v1.6.2 architecture and Docker Compose setup
- **Time-sensitive** - log analysis is most effective soon after issue occurs
- **Complement to `/analyze-bug`** - use this for diagnosis, `/analyze-bug` for fixes
