# SurveyBot - Docker Deployment Guide

## Overview

This guide explains how to run the entire SurveyBot application (API + PostgreSQL + pgAdmin) using Docker Compose.

## Architecture

The dockerized setup includes:
- **API**: ASP.NET Core 8.0 application
- **PostgreSQL**: Database server (version 15)
- **pgAdmin**: Database management UI (optional)

All services run in a Docker bridge network (`surveybot-network`) and can communicate using service names.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

## Quick Start

### 1. Build and Start All Services

```bash
# Build and start all containers
docker-compose up -d --build

# View logs
docker-compose logs -f

# View API logs only
docker-compose logs -f api

# View PostgreSQL logs only
docker-compose logs -f postgres
```

### 2. Verify Services Are Running

```bash
# Check container status
docker ps

# Expected output:
# - surveybot-api (running on port 5000)
# - surveybot-postgres (running on port 5432)
# - surveybot-pgadmin (running on port 5050)
```

### 3. Access the Application

- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health/db
- **pgAdmin**: http://localhost:5050
  - Email: `admin@example.com`
  - Password: `admin123`

## Key Features

### 1. Automatic Database Migrations

The API automatically applies pending database migrations on startup. You don't need to run `dotnet ef database update` manually.

**How it works**:
- On startup, the API checks for pending migrations
- If found, it applies them automatically
- If the database isn't ready, it retries up to 10 times (30 seconds total)
- Logs all migration activities

**Benefits**:
- No manual migration steps
- Production-ready deployment
- Works seamlessly with Docker

### 2. Docker Networking

The connection string uses the PostgreSQL service name instead of `localhost`:

```
Host=postgres;Port=5432;Database=surveybot_db;...
```

**Why?** When both API and PostgreSQL run in Docker, they communicate via the Docker network. The service name `postgres` is resolved to the container's IP address automatically.

### 3. Health Checks

Both API and PostgreSQL have health checks:

**PostgreSQL**:
- Command: `pg_isready -U surveybot_user -d surveybot_db`
- Interval: 10 seconds
- Retries: 5

**API**:
- Command: `curl --fail http://localhost:8080/health/db`
- Interval: 30 seconds
- Start period: 40 seconds (gives time for migrations)
- Retries: 3

### 4. Dependency Management

The API waits for PostgreSQL to be healthy before starting:

```yaml
depends_on:
  postgres:
    condition: service_healthy
```

This ensures migrations run successfully.

## Configuration

### Environment Variables

The API configuration is set via environment variables in `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:8080
  - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
```

**To add Telegram bot token**:

Edit `docker-compose.yml` and add:

```yaml
environment:
  - BotConfiguration__BotToken=YOUR_BOT_TOKEN_HERE
  - BotConfiguration__BotUsername=@YourBotUsername
```

### Volumes

**Persistent Storage**:
- `postgres_data`: PostgreSQL database files
- `./logs`: API log files (mounted from host)

**Data Persistence**: Even if you stop/remove containers, the database data is preserved in the `postgres_data` volume.

## Common Commands

### Starting Services

```bash
# Start all services (detached mode)
docker-compose up -d

# Start with rebuild (after code changes)
docker-compose up -d --build

# Start and view logs
docker-compose up
```

### Stopping Services

```bash
# Stop all services
docker-compose stop

# Stop and remove containers (keeps volumes)
docker-compose down

# Stop and remove containers + volumes (WARNING: deletes database!)
docker-compose down -v
```

### Viewing Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f postgres

# Last 100 lines
docker-compose logs --tail=100 api
```

### Rebuilding API

```bash
# After code changes
docker-compose up -d --build api

# Force rebuild (no cache)
docker-compose build --no-cache api
docker-compose up -d api
```

### Database Operations

```bash
# Access PostgreSQL CLI
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db

# Backup database
docker exec surveybot-postgres pg_dump -U surveybot_user surveybot_db > backup.sql

# Restore database
docker exec -i surveybot-postgres psql -U surveybot_user -d surveybot_db < backup.sql
```

### Inspecting Containers

```bash
# Container details
docker inspect surveybot-api

# Resource usage
docker stats

# Execute command in container
docker exec -it surveybot-api bash

# View API files
docker exec surveybot-api ls -la /app
```

## Troubleshooting

### API Cannot Connect to Database

**Symptoms**:
- API logs show "Cannot connect to database"
- Health check fails

**Solutions**:
1. Check if PostgreSQL is healthy:
   ```bash
   docker ps
   # Look for "healthy" status next to postgres container
   ```

2. Check PostgreSQL logs:
   ```bash
   docker-compose logs postgres
   ```

3. Verify connection string uses `postgres` (service name), not `localhost`:
   ```bash
   docker-compose config
   ```

4. Restart services:
   ```bash
   docker-compose restart
   ```

### Migrations Not Applied

**Symptoms**:
- API logs don't show "Database migrations applied successfully"
- Database tables don't exist

**Solutions**:
1. Check API startup logs:
   ```bash
   docker-compose logs api | grep -i migration
   ```

2. Manually apply migrations (temporary):
   ```bash
   docker exec -it surveybot-api dotnet ef database update
   ```

3. Check if database exists:
   ```bash
   docker exec -it surveybot-postgres psql -U surveybot_user -l
   ```

### Port Already in Use

**Symptoms**:
- Error: "Bind for 0.0.0.0:5000 failed: port is already allocated"

**Solutions**:
1. Change port in `docker-compose.yml`:
   ```yaml
   ports:
     - "5001:8080"  # Use port 5001 instead
   ```

2. Or stop the service using the port:
   ```bash
   # On Linux/Mac
   lsof -i :5000
   kill -9 <PID>

   # On Windows
   netstat -ano | findstr :5000
   taskkill /PID <PID> /F
   ```

### API Container Keeps Restarting

**Symptoms**:
- `docker ps` shows API restarting
- API logs show errors

**Solutions**:
1. View crash logs:
   ```bash
   docker-compose logs --tail=100 api
   ```

2. Check health status:
   ```bash
   docker inspect surveybot-api | grep Health -A 10
   ```

3. Disable health check temporarily (edit `docker-compose.yml`):
   ```yaml
   # Comment out healthcheck section
   ```

4. Run API in foreground to see errors:
   ```bash
   docker-compose up api
   ```

### Database Connection Timeout

**Symptoms**:
- API logs: "Timeout expired. The timeout period elapsed..."

**Solutions**:
1. Increase timeout in connection string (in `docker-compose.yml`):
   ```
   Timeout=60;CommandTimeout=60;
   ```

2. Check database load:
   ```bash
   docker stats surveybot-postgres
   ```

### Cannot Access Swagger UI

**Symptoms**:
- http://localhost:5000/swagger returns 404

**Solutions**:
1. Swagger is only enabled in Development. Set environment:
   ```yaml
   environment:
     - ASPNETCORE_ENVIRONMENT=Development
   ```

2. Or access API endpoints directly:
   ```bash
   curl http://localhost:5000/health/db
   ```

## Production Considerations

### 1. Security

**Current setup is for DEVELOPMENT only**. For production:

1. **Use secrets management**:
   ```yaml
   secrets:
     db_password:
       external: true
   ```

2. **Remove exposed ports** (except API):
   ```yaml
   # Don't expose PostgreSQL port 5432 to host
   # Remove: - "5432:5432"
   ```

3. **Enable HTTPS**:
   - Use reverse proxy (nginx, Traefik)
   - Add SSL certificates
   - Update `ASPNETCORE_URLS=https://+:443`

4. **Change default passwords**:
   - PostgreSQL password
   - pgAdmin password
   - JWT secret key

### 2. Performance

1. **Adjust connection pool**:
   ```
   MinPoolSize=10;MaxPoolSize=200;
   ```

2. **Add resource limits**:
   ```yaml
   deploy:
     resources:
       limits:
         cpus: '2'
         memory: 2G
       reservations:
         cpus: '1'
         memory: 1G
   ```

3. **Use production-optimized image**:
   - Multi-stage build (already implemented)
   - No debug symbols
   - Minimal base image

### 3. Monitoring

1. **Add logging service** (ELK, Seq):
   ```yaml
   seq:
     image: datalust/seq:latest
     ports:
       - "5341:80"
   ```

2. **Add metrics** (Prometheus):
   ```yaml
   prometheus:
     image: prom/prometheus:latest
     volumes:
       - ./prometheus.yml:/etc/prometheus/prometheus.yml
   ```

3. **Add container monitoring**:
   - Docker Stats
   - cAdvisor
   - Grafana

## Advanced Usage

### Custom Network Configuration

```bash
# Create custom network
docker network create surveybot-custom

# Update docker-compose.yml
networks:
  surveybot-network:
    external: true
    name: surveybot-custom
```

### Database Backup Automation

```bash
# Create backup script
cat > backup.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec surveybot-postgres pg_dump -U surveybot_user surveybot_db > backup_$DATE.sql
EOF

chmod +x backup.sh

# Schedule with cron
crontab -e
# Add: 0 2 * * * /path/to/backup.sh
```

### Scaling (Future)

```bash
# Scale API instances
docker-compose up -d --scale api=3

# Requires load balancer (nginx, Traefik)
```

## Summary

**Key Points**:
1. ✅ All services run in Docker containers
2. ✅ Automatic database migrations on startup
3. ✅ Docker networking with service names
4. ✅ Health checks for reliability
5. ✅ Persistent data storage
6. ✅ Easy logs access
7. ✅ Production-ready (with additional security measures)

**Next Steps**:
1. Add Telegram bot token to environment variables
2. Configure webhook URL for production
3. Set up SSL/HTTPS for production deployment
4. Implement backup strategy
5. Add monitoring and logging

For more information, see the main [CLAUDE.md](CLAUDE.md) documentation.
