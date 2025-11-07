# Developer Onboarding Guide
## Telegram Survey Bot MVP

Welcome to the Telegram Survey Bot project! This guide will help you set up your development environment and get you productive quickly.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Getting the Code](#getting-the-code)
3. [Environment Setup](#environment-setup)
4. [Database Setup](#database-setup)
5. [Running the Application](#running-the-application)
6. [Development Workflow](#development-workflow)
7. [Testing](#testing)
8. [Debugging](#debugging)
9. [Common Tasks](#common-tasks)
10. [Tips and Tricks](#tips-and-tricks)

---

## Prerequisites

Before you begin, install the following tools:

### Required Software

1. **NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version` (should show 8.0.x)

2. **Docker Desktop**
   - Download: https://www.docker.com/products/docker-desktop
   - Required for PostgreSQL database
   - Verify: `docker --version` and `docker-compose --version`

3. **Git**
   - Download: https://git-scm.com/downloads
   - Verify: `git --version`

4. **IDE (Choose One)**
   - **Visual Studio 2022** (Windows/Mac) - Recommended for Windows
     - Community Edition is free
     - Includes all .NET tools
     - Great debugging experience

   - **Visual Studio Code** (All platforms) - Lightweight alternative
     - Install C# extension by Microsoft
     - Install C# Dev Kit extension
     - Good for cross-platform development

### Optional Tools

- **pgAdmin** - PostgreSQL database management (included in docker-compose)
- **Postman** or **Insomnia** - API testing (Swagger UI is built-in)
- **Git GUI** - GitKraken, SourceTree, or GitHub Desktop

### Telegram Bot Token

You'll need a bot token from Telegram:
1. Open Telegram and find [@BotFather](https://t.me/botfather)
2. Send `/newbot` command
3. Follow instructions to create your bot
4. Save the API token (format: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)
5. Don't share this token publicly!

---

## Getting the Code

### Clone the Repository

```bash
# Navigate to your workspace
cd ~/workspace  # or C:\workspace on Windows

# Clone the repository
git clone <repository-url>
cd SurveyBot

# Verify the structure
ls -la  # or dir on Windows
```

You should see:
```
SurveyBot/
├── src/
├── tests/
├── documentation/
├── docker-compose.yml
├── SurveyBot.sln
└── README.md
```

---

## Environment Setup

### Step 1: Configure Environment Variables

Create a `.env` file in the project root:

```bash
# Copy the example file
cp .env.example .env  # or copy on Windows

# Edit the .env file
# Replace YOUR_BOT_TOKEN_HERE with your actual bot token
```

Your `.env` file should look like this:
```bash
# Telegram Bot Configuration
TELEGRAM_BOT_TOKEN=123456789:ABCdefGHIjklMNOpqrsTUVwxyz
TELEGRAM_WEBHOOK_URL=http://localhost:5000/api/telegram/webhook

# Database Configuration
DATABASE_CONNECTION=Host=localhost;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000

# Logging
SERILOG_MINIMUM_LEVEL=Information
```

### Step 2: Install .NET Tools

Install Entity Framework Core tools globally:

```bash
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version
# Should show: Entity Framework Core .NET Command-line Tools 8.0.x
```

### Step 3: Restore NuGet Packages

```bash
# From the project root
dotnet restore

# This downloads all required NuGet packages
# Takes 1-2 minutes on first run
```

---

## Database Setup

### Option 1: Using Docker (Recommended)

This is the easiest way to get started.

#### Start PostgreSQL

```bash
# From project root
docker-compose up -d

# Verify containers are running
docker-compose ps

# You should see:
# - surveybot-postgres (port 5432)
# - surveybot-pgadmin (port 5050)
```

#### Apply Database Migrations

```bash
# Navigate to API project
cd src/SurveyBot.API

# Apply migrations to create database schema
dotnet ef database update

# You should see output like:
# Applying migration '20231106_InitialCreate'
# Done.
```

#### Verify Database

Access pgAdmin to verify:
1. Open browser: http://localhost:5050
2. Login: `admin@surveybot.local` / `admin123`
3. Add server:
   - Name: SurveyBot Local
   - Host: postgres
   - Port: 5432
   - Database: surveybot_db
   - Username: surveybot_user
   - Password: surveybot_dev_password
4. Expand: Servers > SurveyBot Local > Databases > surveybot_db > Schemas > public > Tables
5. You should see: users, surveys, questions, responses, answers

### Option 2: Local PostgreSQL Installation

If you prefer not to use Docker:

#### Install PostgreSQL

1. Download PostgreSQL 15+ from https://www.postgresql.org/download/
2. Run installer (default settings are fine)
3. Remember the password you set for the postgres user

#### Create Database

```bash
# Using psql command-line
psql -U postgres

# In psql:
CREATE DATABASE surveybot_db;
CREATE USER surveybot_user WITH PASSWORD 'surveybot_dev_password';
GRANT ALL PRIVILEGES ON DATABASE surveybot_db TO surveybot_user;
\q
```

#### Update Connection String

Edit `src/SurveyBot.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=surveybot_db;Username=surveybot_user;Password=surveybot_dev_password"
  }
}
```

#### Apply Migrations

```bash
cd src/SurveyBot.API
dotnet ef database update
```

---

## Running the Application

### Start the API

#### Using Command Line

```bash
# From project root
cd src/SurveyBot.API

# Run the application
dotnet run

# You should see:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5000
# info: Microsoft.Hosting.Lifetime[0]
#       Application started. Press Ctrl+C to shut down.
```

#### Using Visual Studio

1. Open `SurveyBot.sln`
2. Set `SurveyBot.API` as startup project (right-click > Set as Startup Project)
3. Press F5 or click "Start Debugging"

#### Using VS Code

1. Open project folder
2. Press F5 (will prompt to create launch.json)
3. Select ".NET Core Launch (web)"
4. Press F5 again to start

### Verify Application is Running

1. **API Health Check**: http://localhost:5000/api/health
   - Should return: `{"status":"Healthy","database":"Connected"}`

2. **Swagger UI**: http://localhost:5000/swagger
   - Interactive API documentation
   - Test endpoints directly from browser

3. **pgAdmin**: http://localhost:5050
   - Database management interface

### Test API Endpoints

Using Swagger UI (http://localhost:5000/swagger):

1. Expand `GET /api/users`
2. Click "Try it out"
3. Click "Execute"
4. Should return 200 OK with empty array (no users yet)

---

## Development Workflow

### Typical Day-to-Day Workflow

```bash
# 1. Start your day
git pull origin main  # Get latest changes
docker-compose up -d  # Start database

# 2. Create a feature branch
git checkout -b feature/your-feature-name

# 3. Make changes to code
# ... edit files ...

# 4. Test your changes
cd src/SurveyBot.API
dotnet run
# Test manually or with automated tests

# 5. Run tests
cd ../..  # Back to root
dotnet test

# 6. Commit your changes
git add .
git commit -m "Add feature: your feature description"

# 7. Push to remote
git push origin feature/your-feature-name

# 8. Create pull request (on GitHub/GitLab)

# 9. End of day cleanup
docker-compose down  # Stop database
```

### Code Style Guidelines

- Use C# naming conventions (PascalCase for public, camelCase for private)
- Add XML documentation to public APIs
- Keep methods small and focused
- Follow Clean Architecture principles
- Write tests for business logic

---

## Testing

### Run All Tests

```bash
# From project root
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# With code coverage (requires coverlet)
dotnet test /p:CollectCoverage=true /p:CoverageReporter=html
```

### Run Specific Tests

```bash
# Run tests in specific project
dotnet test tests/SurveyBot.Tests/SurveyBot.Tests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~SurveyBot.Tests.SurveyRepositoryTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~SurveyBot.Tests.SurveyRepositoryTests.CreateSurvey_ShouldReturnNewSurvey"
```

### Write New Tests

Example test structure:

```csharp
public class SurveyRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SurveyRepository _repository;

    public SurveyRepositoryTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new SurveyRepository(_context);
    }

    [Fact]
    public async Task CreateSurvey_ShouldReturnNewSurvey()
    {
        // Arrange
        var survey = new Survey
        {
            Title = "Test Survey",
            Description = "Test Description",
            CreatorId = 1,
            IsActive = true
        };

        // Act
        var result = await _repository.CreateAsync(survey);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Test Survey", result.Title);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

---

## Debugging

### Visual Studio Debugging

1. Set breakpoints by clicking left margin (red dot appears)
2. Press F5 to start debugging
3. Use debugging windows:
   - Locals (Alt+4) - view local variables
   - Watch (Alt+3) - watch specific expressions
   - Call Stack (Alt+7) - view call stack

### VS Code Debugging

1. Set breakpoints by clicking left margin
2. Press F5 to start debugging
3. Use Debug sidebar (Ctrl+Shift+D)

### Debugging Tips

**Log Database Queries**

Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Debug API Requests**

Check logs in console output:
```
info: SurveyBot.API.Middleware.RequestLoggingMiddleware[0]
      HTTP GET /api/surveys responded 200 in 45.3ms
```

**Debug Bot Messages**

All bot interactions are logged:
```
info: SurveyBot.Bot.Handlers.StartCommandHandler[0]
      User 123456789 executed /start command
```

### Common Debug Scenarios

**Database connection fails**
- Check docker-compose is running: `docker-compose ps`
- Verify connection string in appsettings.json
- Check database exists: connect via pgAdmin

**Migrations not applying**
- Ensure you're in src/SurveyBot.API directory
- Delete Migrations folder and start fresh
- Verify EF tools installed: `dotnet ef --version`

**API returns 500 error**
- Check application logs in console
- Look at GlobalExceptionMiddleware output
- Verify database is accessible

---

## Common Tasks

### Add a New Database Entity

1. **Create entity in Core**
   ```csharp
   // src/SurveyBot.Core/Entities/NewEntity.cs
   public class NewEntity
   {
       public int Id { get; set; }
       public string Name { get; set; }
       public DateTime CreatedAt { get; set; }
   }
   ```

2. **Add to DbContext**
   ```csharp
   // src/SurveyBot.Infrastructure/Data/ApplicationDbContext.cs
   public DbSet<NewEntity> NewEntities { get; set; }
   ```

3. **Create migration**
   ```bash
   cd src/SurveyBot.API
   dotnet ef migrations add AddNewEntity
   ```

4. **Apply migration**
   ```bash
   dotnet ef database update
   ```

5. **Create repository interface and implementation**

### Add a New API Endpoint

1. **Create/update controller**
   ```csharp
   // src/SurveyBot.API/Controllers/NewController.cs
   [ApiController]
   [Route("api/[controller]")]
   public class NewController : ControllerBase
   {
       [HttpGet]
       public async Task<ActionResult<ApiResponse<List<NewEntity>>>> GetAll()
       {
           // Implementation
       }
   }
   ```

2. **Test with Swagger**
   - Run app: `dotnet run`
   - Open: http://localhost:5000/swagger
   - Test your new endpoint

### Reset Database

```bash
# Delete database
cd src/SurveyBot.API
dotnet ef database drop

# Recreate database
dotnet ef database update
```

### View Database Schema

```bash
# Generate SQL script
cd src/SurveyBot.API
dotnet ef migrations script > schema.sql

# View the schema.sql file
```

---

## Tips and Tricks

### Speed Up Development

1. **Use watch mode** for auto-restart:
   ```bash
   cd src/SurveyBot.API
   dotnet watch run
   ```

2. **Use Hot Reload** in Visual Studio
   - Edit code while debugging
   - Changes apply without restart (for most changes)

3. **Use Swagger for API testing**
   - No need for Postman for simple tests
   - http://localhost:5000/swagger

### IDE Productivity

**Visual Studio Shortcuts**
- F5 - Start debugging
- Ctrl+F5 - Run without debugging
- F9 - Toggle breakpoint
- F10 - Step over
- F11 - Step into
- Ctrl+K, Ctrl+D - Format document
- Ctrl+. - Quick actions (show fixes)

**VS Code Shortcuts**
- F5 - Start debugging
- F9 - Toggle breakpoint
- F10 - Step over
- F11 - Step into
- Shift+Alt+F - Format document
- Ctrl+. - Quick fix

### Database Tips

1. **Quick database reset**
   ```bash
   docker-compose down -v  # Removes volumes
   docker-compose up -d
   cd src/SurveyBot.API
   dotnet ef database update
   ```

2. **View query results quickly**
   - Use pgAdmin: http://localhost:5050
   - Run SQL directly in Tables > Query Tool

3. **Seed test data**
   - Create a SQL script in `documentation/database/seed.sql`
   - Run via pgAdmin or psql

### Git Workflow Tips

1. **Commit frequently** with clear messages
2. **Pull before you push** to avoid conflicts
3. **Use feature branches** for new features
4. **Review your changes** before committing
   ```bash
   git status
   git diff
   ```

---

## Project Structure Quick Reference

```
SurveyBot/
├── src/
│   ├── SurveyBot.Core/          # Domain entities and interfaces
│   │   ├── Entities/            # Your domain models go here
│   │   └── Interfaces/          # Repository interfaces go here
│   │
│   ├── SurveyBot.Infrastructure/  # Data access
│   │   ├── Data/                # DbContext goes here
│   │   └── Repositories/        # Repository implementations go here
│   │
│   ├── SurveyBot.Bot/           # Telegram bot logic
│   │   └── Handlers/            # Bot message handlers go here
│   │
│   └── SurveyBot.API/           # Web API
│       ├── Controllers/         # API endpoints go here
│       ├── Middleware/          # Custom middleware
│       └── Models/              # DTOs go here
│
├── tests/
│   └── SurveyBot.Tests/         # All tests go here
│
└── documentation/               # Documentation (you're here!)
    ├── database/                # Database docs
    ├── architecture/            # Architecture docs
    └── DEVELOPER_ONBOARDING.md  # This file
```

---

## Getting Help

### Documentation

- [README.md](../README.md) - Project overview
- [Architecture](architecture/ARCHITECTURE.md) - System design
- [Database](database/README.md) - Database schema
- [Troubleshooting](TROUBLESHOOTING.md) - Common issues

### Useful Commands Reference

```bash
# Build
dotnet build
dotnet build -c Release

# Run
dotnet run
dotnet watch run  # Auto-restart

# Test
dotnet test
dotnet test --logger "console;verbosity=detailed"

# Database
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database drop
dotnet ef migrations remove

# Docker
docker-compose up -d
docker-compose down
docker-compose ps
docker-compose logs -f postgres

# Git
git status
git add .
git commit -m "message"
git push origin branch-name
git pull origin main
```

---

## Next Steps

Now that your environment is set up:

1. Read the [Architecture Documentation](architecture/ARCHITECTURE.md)
2. Explore the codebase starting with `Program.cs`
3. Review existing tests in `tests/SurveyBot.Tests/`
4. Try making a small change and running tests
5. Pick up your first task from the project board

Welcome to the team! If you have questions, don't hesitate to ask.

---

**Happy Coding!**

Last Updated: 2025-11-06
