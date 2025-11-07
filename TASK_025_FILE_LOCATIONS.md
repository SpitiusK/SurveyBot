# TASK-025: File Locations Quick Reference

## Command Handlers

### StartCommandHandler.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\Commands\StartCommandHandler.cs`
**Status:** UPDATED (Added "Find Surveys" button)
**Command:** `/start`

### HelpCommandHandler.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\Commands\HelpCommandHandler.cs`
**Status:** EXISTING (No changes)
**Command:** `/help`

### SurveysCommandHandler.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\Commands\SurveysCommandHandler.cs`
**Status:** NEWLY CREATED
**Command:** `/surveys`

### MySurveysCommandHandler.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Handlers\Commands\MySurveysCommandHandler.cs`
**Status:** EXISTING (No changes)
**Command:** `/mysurveys`

## Services

### CommandRouter.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Services\CommandRouter.cs`
**Status:** EXISTING (No changes)
**Purpose:** Routes commands to appropriate handlers

### UpdateHandler.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Services\UpdateHandler.cs`
**Status:** FIXED (Corrected callback command handling)
**Purpose:** Processes Telegram updates

### BotService.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Services\BotService.cs`
**Status:** EXISTING (No changes)
**Purpose:** Manages Telegram bot client

## Interfaces

### ICommandHandler.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Interfaces\ICommandHandler.cs`
**Status:** EXISTING (No changes)
**Purpose:** Interface for all command handlers

### IBotService.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Interfaces\IBotService.cs`
**Status:** EXISTING (No changes)
**Purpose:** Interface for bot service

### IUpdateHandler.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Interfaces\IUpdateHandler.cs`
**Status:** EXISTING (No changes)
**Purpose:** Interface for update handler

## Extensions

### BotServiceExtensions.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Extensions\BotServiceExtensions.cs`
**Status:** UPDATED (Added SurveysCommandHandler registration)
**Purpose:** DI registration for bot handlers

### ServiceCollectionExtensions.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.Bot\Extensions\ServiceCollectionExtensions.cs`
**Status:** EXISTING (No changes)
**Purpose:** DI registration for bot services

## Configuration

### Program.cs
**Location:** `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\Program.cs`
**Status:** UPDATED (Added bot service registration and initialization)
**Purpose:** Application startup and configuration

## Documentation

### Testing Instructions
**Location:** `C:\Users\User\Desktop\SurveyBot\TESTING_INSTRUCTIONS_TASK_025.md`
**Status:** NEWLY CREATED
**Purpose:** Comprehensive testing guide for all command handlers

### Implementation Summary
**Location:** `C:\Users\User\Desktop\SurveyBot\TASK_025_IMPLEMENTATION_SUMMARY.md`
**Status:** NEWLY CREATED
**Purpose:** Complete overview of implementation details

### File Locations Reference
**Location:** `C:\Users\User\Desktop\SurveyBot\TASK_025_FILE_LOCATIONS.md`
**Status:** NEWLY CREATED (This file)
**Purpose:** Quick reference for all file locations

## Project Structure

```
C:\Users\User\Desktop\SurveyBot\
├── src\
│   ├── SurveyBot.Bot\
│   │   ├── Handlers\
│   │   │   └── Commands\
│   │   │       ├── StartCommandHandler.cs (UPDATED)
│   │   │       ├── HelpCommandHandler.cs (EXISTING)
│   │   │       ├── SurveysCommandHandler.cs (NEW)
│   │   │       └── MySurveysCommandHandler.cs (EXISTING)
│   │   ├── Services\
│   │   │   ├── CommandRouter.cs (EXISTING)
│   │   │   ├── UpdateHandler.cs (FIXED)
│   │   │   └── BotService.cs (EXISTING)
│   │   ├── Interfaces\
│   │   │   ├── ICommandHandler.cs (EXISTING)
│   │   │   ├── IBotService.cs (EXISTING)
│   │   │   └── IUpdateHandler.cs (EXISTING)
│   │   ├── Extensions\
│   │   │   ├── BotServiceExtensions.cs (UPDATED)
│   │   │   └── ServiceCollectionExtensions.cs (EXISTING)
│   │   └── Configuration\
│   │       └── BotConfiguration.cs (EXISTING)
│   └── SurveyBot.API\
│       └── Program.cs (UPDATED)
├── TESTING_INSTRUCTIONS_TASK_025.md (NEW)
├── TASK_025_IMPLEMENTATION_SUMMARY.md (NEW)
└── TASK_025_FILE_LOCATIONS.md (NEW - This file)
```

## Summary of Changes

### Files Created (3)
1. `SurveysCommandHandler.cs` - New command handler
2. `TESTING_INSTRUCTIONS_TASK_025.md` - Testing guide
3. `TASK_025_IMPLEMENTATION_SUMMARY.md` - Implementation summary

### Files Modified (4)
1. `StartCommandHandler.cs` - Added "Find Surveys" button
2. `UpdateHandler.cs` - Fixed callback command handling
3. `BotServiceExtensions.cs` - Added SurveysCommandHandler registration
4. `Program.cs` - Added bot service registration and initialization

### Files Referenced (No Changes) (8)
1. `HelpCommandHandler.cs`
2. `MySurveysCommandHandler.cs`
3. `CommandRouter.cs`
4. `BotService.cs`
5. `ICommandHandler.cs`
6. `IBotService.cs`
7. `IUpdateHandler.cs`
8. `ServiceCollectionExtensions.cs`

---

**Total Files Affected:** 15
**New Files:** 3
**Modified Files:** 4
**Referenced Files:** 8
