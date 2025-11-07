# Docker Startup Guide

## Important: Docker Desktop Must Be Running

Before running `docker-compose up`, you must start Docker Desktop.

### Steps to Start Docker Services

1. **Start Docker Desktop**
   - Open Docker Desktop application
   - Wait for Docker to fully start (whale icon in system tray will be steady)
   - You should see "Docker Desktop is running" status

2. **Verify Docker is Running**
   ```bash
   docker ps
   ```
   This command should execute without errors.

3. **Start SurveyBot Services**
   Navigate to the project directory and run:
   ```bash
   cd C:\Users\User\Desktop\SurveyBot
   docker-compose up -d
   ```

4. **Verify Services Are Running**
   ```bash
   docker-compose ps
   ```

   You should see two containers running:
   - surveybot-postgres (port 5432)
   - surveybot-pgadmin (port 5050)

5. **Check Service Health**
   ```bash
   docker-compose logs
   ```

## First-Time Setup After Starting

Once containers are running:

### Access PostgreSQL
```bash
# Using psql (if installed locally)
psql -h localhost -p 5432 -U surveybot_user -d surveybot_db
# Password: surveybot_dev_password

# Or use pgAdmin (see below)
```

### Access pgAdmin
1. Open browser: http://localhost:5050
2. Login:
   - Email: admin@surveybot.local
   - Password: admin123
3. Add server connection:
   - Right-click "Servers" > Register > Server
   - General Tab:
     - Name: SurveyBot Local
   - Connection Tab:
     - Host name: postgres (use container name, not localhost)
     - Port: 5432
     - Maintenance database: surveybot_db
     - Username: surveybot_user
     - Password: surveybot_dev_password
   - Save

## Connection String for Application

Add this to your appsettings.json (already configured):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=5432;Database=surveybot_db;User Id=surveybot_user;Password=surveybot_dev_password;"
}
```

## Common Issues

### Issue: "Cannot connect to Docker daemon"
**Solution**: Start Docker Desktop and wait for it to fully initialize.

### Issue: "Port 5432 already in use"
**Solution**: Another PostgreSQL instance is running. Either:
- Stop the other PostgreSQL service
- Or modify docker-compose.yml to use different port (e.g., 5433:5432)

### Issue: "Port 5050 already in use"
**Solution**: Another service is using port 5050. Modify docker-compose.yml to use different port (e.g., 5051:80)

## Development Workflow

### Daily Start
```bash
docker-compose start
```

### Daily Stop
```bash
docker-compose stop
```

### Complete Cleanup (removes data)
```bash
docker-compose down -v
```

### View Real-time Logs
```bash
docker-compose logs -f
```

### Restart Services
```bash
docker-compose restart
```

## Next Steps

After verifying containers are running:
1. Test database connection from your application
2. Run Entity Framework migrations
3. Verify pgAdmin access
4. Start developing
