# Quick Start - Database Setup

## 30-Second Setup

1. **Start Docker Desktop** (wait for it to fully start)

2. **Run this command**:
   ```bash
   cd C:\Users\User\Desktop\SurveyBot
   docker-compose up -d
   ```

3. **Done!** Your database is ready.

## Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| PostgreSQL | localhost:5432 | User: surveybot_user<br>Password: surveybot_dev_password<br>Database: surveybot_db |
| pgAdmin | http://localhost:5050 | Email: admin@surveybot.local<br>Password: admin123 |

## Connection String (for .NET)

```
Server=localhost;Port=5432;Database=surveybot_db;User Id=surveybot_user;Password=surveybot_dev_password;
```

Already configured in:
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\appsettings.json`
- `C:\Users\User\Desktop\SurveyBot\src\SurveyBot.API\appsettings.Development.json`

## Common Commands

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose stop

# View logs
docker-compose logs -f

# Check status
docker-compose ps

# Restart services
docker-compose restart

# Remove everything (including data)
docker-compose down -v
```

## Connect to Database via Command Line

```bash
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db
```

## That's It!

For detailed information, see:
- `README-DOCKER.md` - Complete Docker documentation
- `DOCKER-STARTUP-GUIDE.md` - Step-by-step guide
- `TASK-002-SUMMARY.md` - Full implementation details
