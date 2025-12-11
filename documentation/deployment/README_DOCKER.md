# 🚀 SurveyBot - Docker Edition

Run the entire SurveyBot application (frontend + backend + database) with **one command**.

---

## ⚡ Quick Start (5 Minutes)

### 1. Install Docker

Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 2. Configure Bot Token

Edit `docker-compose.yml` (line 37):
```yaml
- BotConfiguration__BotToken=YOUR_BOT_TOKEN_HERE
```

Get your token from [@BotFather](https://t.me/botfather) on Telegram.

### 3. Run

```bash
docker-compose up -d
```

### 4. Access

Open http://localhost:3000 in your browser.

---

## 📖 Documentation

- **[Quick Start Guide](DOCKER_QUICKSTART.md)** - 5-step setup (start here!)
- **[Complete Setup Guide](DOCKER_SETUP.md)** - Detailed documentation
- **[Implementation Summary](DOCKER_IMPLEMENTATION_SUMMARY.md)** - Technical details

---

## 🌐 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| Frontend | http://localhost:3000 | Telegram login |
| API | http://localhost:3000/api | (via proxy) |
| pgAdmin | http://localhost:5050 | admin@example.com / admin123 |

---

## 🛠 Common Commands

```bash
# Start everything
docker-compose up -d

# View logs
docker-compose logs -f

# Stop everything
docker-compose down

# Rebuild after changes
docker-compose up -d --build
```

---

## ❓ Troubleshooting

**Port already in use?**
- Edit `docker-compose.yml` and change `3000:80` to `3001:80`

**Container won't start?**
```bash
docker-compose logs api
docker-compose logs frontend
```

**Need help?** See [DOCKER_SETUP.md](DOCKER_SETUP.md) for detailed troubleshooting.

---

## 📦 What's Included?

- ✅ React Frontend (with Nginx)
- ✅ .NET API Backend
- ✅ PostgreSQL Database
- ✅ pgAdmin (database management)
- ✅ Automatic health checks
- ✅ Internal networking
- ✅ Production-ready configuration

---

## 🏗 Architecture

```
Browser → Frontend (Nginx) → API (.NET) → PostgreSQL
           ↓
      Static Files + /api Proxy
```

- Frontend serves React app and proxies `/api/*` to backend
- All services communicate via internal Docker network
- Only frontend port (3000) is exposed to host

---

## 📚 Full Documentation

For complete project documentation, see [CLAUDE.md](CLAUDE.md)

---

**Version**: 1.6.2 | **Last Updated**: 2025-12-11
