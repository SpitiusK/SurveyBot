# SurveyBot Docker Setup Guide

Complete guide for running the entire SurveyBot application (frontend + backend + database) with a single Docker Compose command.

**Last Updated**: 2025-12-11 | **Version**: 1.6.2

---

## Quick Start (5 Minutes)

### 1. Prerequisites

**Required**:
- Docker Desktop 20.10+ ([Download](https://www.docker.com/products/docker-desktop))
- Docker Compose 2.0+ (included with Docker Desktop)
- 4GB+ available RAM
- 10GB+ available disk space

**Verify Installation**:
```bash
docker --version          # Should show 20.10+
docker-compose --version  # Should show 2.0+
```

### 2. Configure Bot Token

Edit `docker-compose.yml` and replace the bot token:

```yaml
services:
  api:
    environment:
      - BotConfiguration__BotToken=YOUR_BOT_TOKEN_HERE  # Line 37
```

**How to get a bot token**:
1. Open Telegram → [@BotFather](https://t.me/botfather)
2. Send `/newbot`
3. Follow the prompts to create your bot
4. Copy the token (format: `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`)

### 3. Start All Services

From the project root directory:

```bash
docker-compose up -d
```

**What this does**:
- Builds frontend React app with Vite
- Builds backend .NET API
- Starts PostgreSQL database
- Starts pgAdmin (database management)
- Creates Docker network for internal communication

**Expected output**:
```
[+] Running 4/4
 ✔ Container surveybot-postgres   Started
 ✔ Container surveybot-api         Started
 ✔ Container surveybot-frontend    Started
 ✔ Container surveybot-pgadmin     Started
```

### 4. Access the Application

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | React Admin Panel |
| **API** | http://localhost:3000/api | REST API (proxied via Nginx) |
| **Swagger** | Not exposed | Only available inside Docker network |
| **pgAdmin** | http://localhost:5050 | Database management |
| **PostgreSQL** | localhost:5432 | Direct database access |

**Frontend Login**:
- Open http://localhost:3000
- Login with your Telegram credentials

**pgAdmin Login**:
- Email: `admin@example.com`
- Password: `admin123`

### 5. Verify Everything is Running

```bash
# Check all containers are running
docker-compose ps

# Should show 4 services: postgres, api, frontend, pgadmin
# All should have "Up" status

# Check logs for errors
docker-compose logs -f frontend
docker-compose logs -f api
```

---

## Architecture Overview

### Docker Network Configuration

```
┌─────────────────────────────────────────────────────────────┐
│                    Host Machine                              │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         surveybot-network (Docker Bridge)           │   │
│  │                                                      │   │
│  │  ┌──────────────┐      ┌──────────────┐            │   │
│  │  │   Frontend   │      │     API      │            │   │
│  │  │ (Nginx:80)   │─────▶│ (.NET:8080)  │            │   │
│  │  │              │      │              │            │   │
│  │  └──────┬───────┘      └──────┬───────┘            │   │
│  │         │                     │                     │   │
│  │         │  /api/* requests    │  DB queries         │   │
│  │         │                     │                     │   │
│  │         │                     ▼                     │   │
│  │         │              ┌──────────────┐            │   │
│  │         │              │  PostgreSQL  │            │   │
│  │         │              │  (Port:5432) │            │   │
│  │         │              └──────────────┘            │   │
│  │         │                                           │   │
│  └─────────┼───────────────────────────────────────────┘   │
│            │                                                │
│            ▼                                                │
│    Port Mapping:                                           │
│    - Frontend: 3000:80   (localhost:3000 → Nginx)          │
│    - PostgreSQL: 5432:5432                                 │
│    - pgAdmin: 5050:80                                      │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Decisions

**1. Nginx Reverse Proxy**:
- Frontend makes API requests to `/api/*` (relative path)
- Nginx proxies requests to `http://surveybot-api:8080/api/`
- No CORS issues (same origin from browser perspective)
- Single port exposure for security

**2. Internal Docker Networking**:
- Services communicate via service names (surveybot-api, surveybot-postgres)
- API is **not exposed** to host machine directly
- Only frontend port (3000) and database ports are exposed

**3. Health Checks**:
- API waits for PostgreSQL to be ready (health check)
- Frontend waits for API to be ready (health check)
- Ensures correct startup order

**4. Build-Time Configuration**:
- Frontend environment variables are baked into JavaScript bundle during build
- API URL is set to `/api` (relative path)
- Nginx configuration is part of the image

---

## Configuration Details

### Port Mapping

| Container Port | Host Port | Service | Notes |
|----------------|-----------|---------|-------|
| 80 | 3000 | Frontend | Nginx serves React app |
| 8080 | (none) | API | Only accessible via Nginx proxy |
| 5432 | 5432 | PostgreSQL | Direct database access |
| 80 | 5050 | pgAdmin | Database management UI |

**Why API is not exposed**:
- Security: API only accessible through frontend proxy
- Simplifies CORS configuration
- Single entry point for the application

### Environment Variables

**API Container** (`docker-compose.yml` lines 33-42):
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:8080
  - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
  - BotConfiguration__BotToken=YOUR_TOKEN_HERE
  - BotConfiguration__UseWebhook=false
  - BotConfiguration__ApiBaseUrl=http://localhost:3000
```

**Frontend Container** (`docker-compose.yml` lines 63-67):
```yaml
build:
  args:
    - VITE_API_BASE_URL=/api
    - VITE_APP_NAME=SurveyBot Admin Panel
    - VITE_APP_VERSION=1.0.0
```

### Volumes

| Volume | Purpose | Persistence |
|--------|---------|-------------|
| `postgres_data` | Database files | Persistent across restarts |
| `pgadmin_data` | pgAdmin settings | Persistent across restarts |
| `./logs` | API logs | Mounted from host |

**Data Persistence**:
- Database data survives container restarts
- To wipe database: `docker-compose down -v` (removes volumes)

---

## Common Commands

### Startup & Shutdown

```bash
# Start all services (detached mode)
docker-compose up -d

# Start and view logs
docker-compose up

# Stop all services (keeps data)
docker-compose down

# Stop and remove volumes (DELETES DATABASE)
docker-compose down -v

# Restart a specific service
docker-compose restart frontend
docker-compose restart api
```

### Logs & Debugging

```bash
# View logs for all services
docker-compose logs

# Follow logs in real-time
docker-compose logs -f

# View logs for specific service
docker-compose logs frontend
docker-compose logs api
docker-compose logs postgres

# Last 100 lines
docker-compose logs --tail=100 api
```

### Rebuilding

```bash
# Rebuild all images and restart
docker-compose up -d --build

# Rebuild specific service
docker-compose build frontend
docker-compose up -d frontend

# Force rebuild (no cache)
docker-compose build --no-cache
```

### Inspecting Containers

```bash
# List all containers
docker-compose ps

# Container details
docker inspect surveybot-frontend
docker inspect surveybot-api

# Execute command inside container
docker exec -it surveybot-api /bin/bash
docker exec -it surveybot-frontend /bin/sh

# View container resource usage
docker stats surveybot-frontend surveybot-api
```

### Database Management

```bash
# Connect to PostgreSQL
docker exec -it surveybot-postgres psql -U surveybot_user -d surveybot_db

# Backup database
docker exec surveybot-postgres pg_dump -U surveybot_user surveybot_db > backup.sql

# Restore database
docker exec -i surveybot-postgres psql -U surveybot_user -d surveybot_db < backup.sql
```

---

## Troubleshooting

### Container Won't Start

**Problem**: Container exits immediately after starting

**Solutions**:

1. **Check logs**:
   ```bash
   docker-compose logs api
   docker-compose logs frontend
   ```

2. **Common issues**:
   - **API**: Database connection failed → Check PostgreSQL is running
   - **Frontend**: Build failed → Check Node.js dependencies
   - **Port conflict**: Port already in use → Change port in docker-compose.yml

3. **Verify build**:
   ```bash
   # Rebuild with no cache
   docker-compose build --no-cache frontend
   ```

### Frontend Shows "Network Error"

**Problem**: Frontend can't connect to API

**Symptoms**:
- Login fails with network error
- API requests return 404 or timeout

**Solutions**:

1. **Check API is running**:
   ```bash
   docker-compose ps
   # api should show "Up" status

   docker-compose logs api
   # Should see "Now listening on: http://+:8080"
   ```

2. **Test API health inside container**:
   ```bash
   docker exec surveybot-api curl http://localhost:8080/health/db
   # Should return 200 OK
   ```

3. **Check Nginx proxy configuration**:
   ```bash
   # View Nginx config
   docker exec surveybot-frontend cat /etc/nginx/conf.d/nginx.conf

   # Check Nginx logs
   docker-compose logs frontend | grep error
   ```

4. **Verify network connectivity**:
   ```bash
   # Test from frontend to API
   docker exec surveybot-frontend wget -O- http://surveybot-api:8080/health/db
   ```

### Database Connection Failed

**Problem**: API can't connect to PostgreSQL

**Symptoms**:
- API logs show "Connection refused"
- API health check fails

**Solutions**:

1. **Check PostgreSQL is ready**:
   ```bash
   docker-compose ps postgres
   # Should show "Up (healthy)"

   docker-compose logs postgres
   # Should see "database system is ready to accept connections"
   ```

2. **Verify connection string**:
   ```yaml
   # In docker-compose.yml
   ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;...
   # Host should be "postgres" (service name), not "localhost"
   ```

3. **Test connection manually**:
   ```bash
   docker exec -it surveybot-api /bin/bash
   # Inside container:
   apt-get update && apt-get install -y postgresql-client
   psql -h postgres -U surveybot_user -d surveybot_db
   ```

### Port Already in Use

**Problem**: Docker says port is already allocated

**Error**:
```
Error: bind: address already in use
```

**Solutions**:

1. **Find process using the port**:
   ```bash
   # Windows
   netstat -ano | findstr :3000

   # Linux/Mac
   lsof -i :3000
   ```

2. **Stop the conflicting process** or **change the port**:
   ```yaml
   # In docker-compose.yml
   frontend:
     ports:
       - "3001:80"  # Change 3000 to 3001
   ```

### Build Fails with "No Space Left"

**Problem**: Docker build fails with disk space error

**Solutions**:

1. **Clean up Docker resources**:
   ```bash
   # Remove unused containers, networks, images
   docker system prune -a

   # Remove unused volumes (WARNING: deletes data)
   docker volume prune
   ```

2. **Check Docker disk usage**:
   ```bash
   docker system df
   ```

3. **Increase Docker Desktop resources**:
   - Docker Desktop → Settings → Resources
   - Increase disk image size

### Frontend Build Takes Forever

**Problem**: `docker-compose up` hangs on frontend build

**Cause**: npm install downloading dependencies

**Solutions**:

1. **Use build cache** (default behavior):
   - Docker caches npm install layer
   - Only reinstalls if package.json changes

2. **Monitor build progress**:
   ```bash
   docker-compose build frontend
   # Shows each build step
   ```

3. **Pre-build images**:
   ```bash
   docker-compose build
   # Builds all images first

   docker-compose up -d
   # Starts pre-built images
   ```

---

## Advanced Configuration

### Using Custom Domains (localhost alternatives)

Add to `C:\Windows\System32\drivers\etc\hosts` (Windows) or `/etc/hosts` (Linux/Mac):

```
127.0.0.1 surveybot.local
127.0.0.1 api.surveybot.local
```

Then access:
- Frontend: http://surveybot.local:3000
- API: http://surveybot.local:3000/api

### Changing Default Ports

Edit `docker-compose.yml`:

```yaml
frontend:
  ports:
    - "8080:80"  # Access on localhost:8080

pgadmin:
  ports:
    - "8888:80"  # Access on localhost:8888

postgres:
  ports:
    - "5433:5432"  # Access on localhost:5433
```

**Note**: Internal container ports (right side) should NOT change.

### Production Mode

For production deployment:

1. **Change environment**:
   ```yaml
   api:
     environment:
       - ASPNETCORE_ENVIRONMENT=Production
   ```

2. **Use secrets** (not plaintext passwords):
   ```yaml
   api:
     environment:
       - ConnectionStrings__DefaultConnection=${DATABASE_URL}
       - BotConfiguration__BotToken=${BOT_TOKEN}
   ```

3. **Enable HTTPS**:
   - Add SSL certificates
   - Configure Nginx for HTTPS
   - Use reverse proxy (Nginx, Traefik)

4. **Remove development services**:
   - Comment out pgAdmin
   - Don't expose PostgreSQL port

### Scaling Services

```bash
# Run multiple frontend instances (requires load balancer)
docker-compose up -d --scale frontend=3

# Run multiple API instances
docker-compose up -d --scale api=2
```

**Note**: Requires additional configuration for load balancing.

---

## Security Considerations

### Default Credentials (CHANGE IN PRODUCTION)

| Service | Default Credentials | Security Level |
|---------|-------------------|----------------|
| PostgreSQL | `surveybot_user` / `surveybot_dev_password` | ⚠️ Development Only |
| pgAdmin | `admin@example.com` / `admin123` | ⚠️ Development Only |
| JWT Secret | From appsettings.json | ⚠️ Development Only |

### Production Checklist

- [ ] Change all default passwords
- [ ] Use environment variables for secrets
- [ ] Don't expose PostgreSQL port (remove `ports:` section)
- [ ] Don't expose pgAdmin (remove entire service)
- [ ] Enable HTTPS
- [ ] Set strong JWT secret key (≥32 characters)
- [ ] Enable Docker secrets or use external secret management
- [ ] Configure firewall rules
- [ ] Enable API rate limiting
- [ ] Set up monitoring and logging

---

## Performance Optimization

### Build Performance

**Use BuildKit** (faster builds):
```bash
# Enable BuildKit
export DOCKER_BUILDKIT=1

# Build with BuildKit
docker-compose build
```

**Layer Caching Tips**:
- `COPY package.json` before `COPY .` (npm install layer cached)
- Multi-stage builds (frontend Dockerfile already uses this)

### Runtime Performance

**Docker Desktop Settings**:
- CPU: 4+ cores recommended
- Memory: 4GB+ recommended
- Swap: 1GB minimum

**Container Resource Limits**:
```yaml
frontend:
  deploy:
    resources:
      limits:
        cpus: '1.0'
        memory: 512M
      reservations:
        memory: 256M
```

---

## Maintenance

### Updating Dependencies

**Frontend**:
```bash
# Update package.json dependencies
cd frontend
npm update

# Rebuild image
docker-compose build frontend
docker-compose up -d frontend
```

**Backend**:
```bash
# Update NuGet packages
cd src/SurveyBot.API
dotnet restore
dotnet build

# Rebuild image
docker-compose build api
docker-compose up -d api
```

### Database Migrations

```bash
# Apply pending migrations
docker exec surveybot-api dotnet ef database update

# Create new migration (inside container)
docker exec -it surveybot-api /bin/bash
cd /app
dotnet ef migrations add MigrationName
```

### Backup Strategy

**Automated Backup Script**:
```bash
#!/bin/bash
# backup.sh

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="./backups"

mkdir -p $BACKUP_DIR

# Backup PostgreSQL
docker exec surveybot-postgres pg_dump -U surveybot_user surveybot_db \
  > "$BACKUP_DIR/surveybot_db_$DATE.sql"

echo "Backup completed: $BACKUP_DIR/surveybot_db_$DATE.sql"
```

**Schedule with cron** (Linux/Mac):
```bash
# Run daily at 2 AM
0 2 * * * /path/to/backup.sh
```

---

## Next Steps

After successful Docker setup:

1. **Login to Frontend**: http://localhost:3000
2. **Create Your First Survey**: Use the Survey Builder wizard
3. **Test Telegram Bot**: Send `/start` to your bot
4. **Explore API**: Check pgAdmin for database structure
5. **Read Documentation**: See [Main CLAUDE.md](CLAUDE.md) for feature details

---

## Related Documentation

- [Main Documentation](CLAUDE.md) - Project overview and architecture
- [Frontend Documentation](frontend/CLAUDE.md) - React admin panel details
- [API Documentation](src/SurveyBot.API/CLAUDE.md) - REST API reference
- [Bot Documentation](src/SurveyBot.Bot/CLAUDE.md) - Telegram bot implementation
- [Troubleshooting Guide](documentation/TROUBLESHOOTING.md) - Common issues

---

## Support

**Issues**:
- Docker not starting: Check Docker Desktop is running
- Build failures: Check logs with `docker-compose logs`
- Network errors: Verify container connectivity

**Resources**:
- Docker Documentation: https://docs.docker.com
- Docker Compose Reference: https://docs.docker.com/compose/compose-file

---

**Last Updated**: 2025-12-11 | **Version**: 1.6.2
