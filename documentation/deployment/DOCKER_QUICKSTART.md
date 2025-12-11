# Docker Quick Start - SurveyBot

**Run the entire SurveyBot application with a single command**

---

## 1️⃣ Prerequisites (2 minutes)

✅ **Install Docker Desktop**: https://www.docker.com/products/docker-desktop

✅ **Verify Installation**:
```bash
docker --version
docker-compose --version
```

✅ **Get Telegram Bot Token**:
- Open [@BotFather](https://t.me/botfather) in Telegram
- Send `/newbot` and follow prompts
- Copy your token (e.g., `1234567890:ABCdefGHI...`)

---

## 2️⃣ Configure Bot Token (1 minute)

Edit `docker-compose.yml` line 37:

```yaml
- BotConfiguration__BotToken=YOUR_BOT_TOKEN_HERE
```

Replace `YOUR_BOT_TOKEN_HERE` with your actual token.

---

## 3️⃣ Start Everything (2 minutes)

From the project root directory:

```bash
docker-compose up -d
```

**Wait for build to complete** (first time: ~5-10 minutes)

---

## 4️⃣ Access the Application

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | Use Telegram login |
| **pgAdmin** | http://localhost:5050 | admin@example.com / admin123 |

---

## 5️⃣ Verify Everything Works

```bash
# Check all services are running
docker-compose ps

# Expected output:
# surveybot-postgres   Up (healthy)
# surveybot-api        Up (healthy)
# surveybot-frontend   Up (healthy)
# surveybot-pgadmin    Up
```

**View logs** (if something fails):
```bash
docker-compose logs -f
```

---

## Common Commands

```bash
# Stop all services (keeps data)
docker-compose down

# Restart everything
docker-compose restart

# View logs
docker-compose logs -f

# Rebuild after code changes
docker-compose up -d --build
```

---

## Troubleshooting

**Port already in use**:
```yaml
# In docker-compose.yml, change port:
frontend:
  ports:
    - "3001:80"  # Changed from 3000 to 3001
```

**Container won't start**:
```bash
# Check logs for the specific service
docker-compose logs api
docker-compose logs frontend
```

**Database connection failed**:
```bash
# Restart PostgreSQL
docker-compose restart postgres

# Wait 30 seconds, then restart API
docker-compose restart api
```

---

## What's Running?

- **Frontend** (localhost:3000): React admin panel with Nginx
- **API**: .NET backend (not exposed, accessed via /api proxy)
- **PostgreSQL** (localhost:5432): Database
- **pgAdmin** (localhost:5050): Database management

All services communicate via internal Docker network.

---

## Next Steps

✅ Open http://localhost:3000

✅ Login with Telegram credentials

✅ Create your first survey

✅ Send `/start` to your Telegram bot

---

**Need more details?** See [DOCKER_SETUP.md](DOCKER_SETUP.md) for complete guide.
