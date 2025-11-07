# Docker Setup for SurveyBot

## Prerequisites
- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

## Services

### PostgreSQL Database
- **Image**: postgres:15-alpine
- **Container Name**: surveybot-postgres
- **Port**: 5432
- **Database**: surveybot_db
- **User**: surveybot_user
- **Password**: surveybot_dev_password

### pgAdmin (Database Management)
- **Image**: dpage/pgadmin4:latest
- **Container Name**: surveybot-pgadmin
- **Port**: 5050
- **Email**: admin@surveybot.local
- **Password**: admin123

## Quick Start

### Start all services
```bash
docker-compose up -d
```

### Stop all services
```bash
docker-compose down
```

### View logs
```bash
docker-compose logs -f
```

### Check service status
```bash
docker-compose ps
```

## Access URLs

- **PostgreSQL**: localhost:5432
- **pgAdmin**: http://localhost:5050

## Connection String

Use this connection string in your application:
```
Server=localhost;Port=5432;Database=surveybot_db;User Id=surveybot_user;Password=surveybot_dev_password;
```

## pgAdmin Setup

1. Open http://localhost:5050 in your browser
2. Login with:
   - Email: admin@surveybot.local
   - Password: admin123
3. Add a new server:
   - Name: SurveyBot Local
   - Host: postgres (use the service name, NOT localhost)
   - Port: 5432
   - Database: surveybot_db
   - Username: surveybot_user
   - Password: surveybot_dev_password

## Data Persistence

Data is persisted in Docker volumes:
- `postgres_data`: Database files
- `pgadmin_data`: pgAdmin configuration

To completely remove data:
```bash
docker-compose down -v
```

## Troubleshooting

### Port already in use
If port 5432 or 5050 is already in use, modify the port mapping in docker-compose.yml:
```yaml
ports:
  - "5433:5432"  # Use port 5433 on host instead
```

### Container won't start
Check logs:
```bash
docker-compose logs postgres
docker-compose logs pgadmin
```

### Reset everything
```bash
docker-compose down -v
docker-compose up -d
```
