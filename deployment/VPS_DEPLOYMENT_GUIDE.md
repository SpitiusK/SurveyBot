# SurveyBot VPS Deployment Guide

This guide walks you through deploying SurveyBot on a Ubuntu 24.04 VPS server.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Get a Domain (Free with DuckDNS)](#get-a-domain-free-with-duckdns)
3. [VPS Initial Setup](#vps-initial-setup)
4. [Deploy SurveyBot](#deploy-surveybot)
5. [SSL Certificate Setup](#ssl-certificate-setup)
6. [Verify Deployment](#verify-deployment)
7. [Maintenance](#maintenance)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before starting, you need:

- **VPS Server**: Ubuntu 24.04 with at least 1GB RAM, 10GB disk
- **SSH Access**: Ability to connect to your VPS via SSH
- **Domain or DuckDNS**: A domain pointing to your VPS IP (see next section)
- **Telegram Bot Token**: From @BotFather

---

## Get a Domain (Free with DuckDNS)

### Option A: DuckDNS (Free - Recommended for Testing)

1. Go to https://www.duckdns.org
2. Login with Google, GitHub, Twitter, or Reddit
3. In the "sub domain" field, enter your desired name (e.g., `mysurveybot`)
4. Click "add domain"
5. Enter your VPS IP address in the "current ip" field
6. Click "update ip"

**Result**: You now have `mysurveybot.duckdns.org` pointing to your VPS!

### Option B: Cheap Domain (~$1-3/year)

1. Go to https://porkbun.com or https://www.namecheap.com
2. Search for cheap domains: `.xyz`, `.site`, `.online`, `.click`
3. Purchase domain (often $1-3 for first year)
4. In DNS settings, add an A record:
   - **Type**: A
   - **Host**: @ (or leave empty)
   - **Value**: Your VPS IP address
   - **TTL**: 300

### Option C: No Domain (IP Only - HTTP Only)

You can deploy without a domain using just your VPS IP address:
- **Pros**: No domain setup needed
- **Cons**: No HTTPS (Telegram webhooks may be unreliable), less professional

---

## VPS Initial Setup

### Step 1: Connect to Your VPS

```bash
ssh root@YOUR_VPS_IP
# Or with a user:
ssh username@YOUR_VPS_IP
```

### Step 2: Update System

```bash
sudo apt update && sudo apt upgrade -y
```

### Step 3: Install Docker

Run the automated setup script:

```bash
# Download and run setup script
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add your user to docker group (so you don't need sudo)
sudo usermod -aG docker $USER

# Install Docker Compose plugin
sudo apt install -y docker-compose-plugin

# Log out and back in for group changes to take effect
exit
```

Reconnect and verify:

```bash
ssh username@YOUR_VPS_IP
docker --version
docker compose version
```

### Step 4: Install Nginx

```bash
sudo apt install -y nginx
sudo systemctl enable nginx
sudo systemctl start nginx
```

### Step 5: Configure Firewall

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status
```

---

## Deploy SurveyBot

### Step 1: Clone Repository

```bash
cd ~
git clone YOUR_REPOSITORY_URL surveybot
cd surveybot
```

Or upload files via SCP/SFTP.

### Step 2: Configure Environment

```bash
# Copy and edit environment file
cp .env.example .env.production
nano .env.production
```

Edit `.env.production`:

```env
# Telegram Bot Configuration
BOT_TOKEN=your_telegram_bot_token_here
WEBHOOK_SECRET=generate_a_random_secret_here

# Domain Configuration
DOMAIN=mysurveybot.duckdns.org
# Or for IP-only: DOMAIN=YOUR_VPS_IP

# Database (change password for production!)
POSTGRES_PASSWORD=your_secure_password_here

# Environment
ASPNETCORE_ENVIRONMENT=Production
```

Generate a secure webhook secret:
```bash
openssl rand -hex 32
```

### Step 3: Deploy with Docker Compose

```bash
# Use production compose file
docker compose -f docker-compose.production.yml up -d

# Check status
docker compose -f docker-compose.production.yml ps

# View logs
docker compose -f docker-compose.production.yml logs -f
```

### Step 4: Configure Nginx

```bash
# Copy Nginx configuration
sudo cp deployment/nginx/surveybot.conf /etc/nginx/sites-available/surveybot

# Edit to set your domain
sudo nano /etc/nginx/sites-available/surveybot
# Change: server_name YOUR_DOMAIN; to your actual domain

# Enable site
sudo ln -s /etc/nginx/sites-available/surveybot /etc/nginx/sites-enabled/

# Remove default site
sudo rm /etc/nginx/sites-enabled/default

# Test configuration
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx
```

---

## SSL Certificate Setup

### Option A: With Domain (Recommended)

Install Certbot and get free SSL certificate:

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Get SSL certificate (replace with your domain)
sudo certbot --nginx -d mysurveybot.duckdns.org

# Follow prompts:
# - Enter email for renewal notifications
# - Agree to terms
# - Choose whether to redirect HTTP to HTTPS (recommended: yes)
```

Certbot automatically:
- Gets SSL certificate from Let's Encrypt
- Configures Nginx for HTTPS
- Sets up auto-renewal

Verify auto-renewal:
```bash
sudo certbot renew --dry-run
```

### Option B: Without Domain (Self-Signed - Not Recommended)

```bash
# Generate self-signed certificate (for testing only)
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/surveybot.key \
  -out /etc/ssl/certs/surveybot.crt

# Update Nginx config to use these certificates
```

**Warning**: Self-signed certificates will show browser warnings and may not work with Telegram webhooks.

---

## Verify Deployment

### Check Services

```bash
# Docker containers
docker compose -f docker-compose.production.yml ps

# All should show "Up" status:
# - surveybot-api
# - surveybot-frontend
# - surveybot-postgres
```

### Test Endpoints

```bash
# Health check
curl http://localhost:8080/health/db

# Via Nginx (HTTP)
curl http://YOUR_DOMAIN/api/health/db

# Via Nginx (HTTPS, after SSL setup)
curl https://YOUR_DOMAIN/api/health/db
```

### Test Telegram Bot

1. Open Telegram
2. Find your bot (@YourBotName)
3. Send `/start`
4. Bot should respond with welcome message

### Check Webhook Status

```bash
curl "https://api.telegram.org/botYOUR_BOT_TOKEN/getWebhookInfo"
```

Should show:
- `url`: Your webhook URL
- `pending_update_count`: 0 (or small number)
- `last_error_message`: Empty or null

### Access Admin Panel

Open in browser: `https://YOUR_DOMAIN`

---

## Maintenance

### View Logs

```bash
# All services
docker compose -f docker-compose.production.yml logs -f

# Specific service
docker compose -f docker-compose.production.yml logs -f api

# Nginx logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### Update Application

```bash
cd ~/surveybot

# Pull latest changes
git pull

# Rebuild and restart
docker compose -f docker-compose.production.yml down
docker compose -f docker-compose.production.yml build
docker compose -f docker-compose.production.yml up -d
```

### Backup Database

```bash
# Create backup
docker exec surveybot-postgres pg_dump -U surveybot_user surveybot_db > backup_$(date +%Y%m%d).sql

# Restore backup
cat backup_20251211.sql | docker exec -i surveybot-postgres psql -U surveybot_user surveybot_db
```

### SSL Certificate Renewal

Certbot automatically renews certificates. To manually renew:

```bash
sudo certbot renew
sudo systemctl reload nginx
```

---

## Troubleshooting

### Bot Not Responding

1. **Check webhook registration**:
   ```bash
   curl "https://api.telegram.org/botYOUR_TOKEN/getWebhookInfo"
   ```

2. **Check API logs**:
   ```bash
   docker compose -f docker-compose.production.yml logs api
   ```

3. **Verify SSL is working** (Telegram requires valid HTTPS):
   ```bash
   curl -I https://YOUR_DOMAIN/api/bot/webhook
   ```

### Database Connection Issues

```bash
# Check PostgreSQL is running
docker compose -f docker-compose.production.yml ps postgres

# Check logs
docker compose -f docker-compose.production.yml logs postgres

# Connect to database
docker exec -it surveybot-postgres psql -U surveybot_user surveybot_db
```

### Nginx Issues

```bash
# Test configuration
sudo nginx -t

# Check error logs
sudo tail -f /var/log/nginx/error.log

# Restart Nginx
sudo systemctl restart nginx
```

### Container Won't Start

```bash
# Check logs
docker compose -f docker-compose.production.yml logs api

# Check disk space
df -h

# Check memory
free -m
```

### SSL Certificate Issues

```bash
# Check certificate status
sudo certbot certificates

# Force renewal
sudo certbot renew --force-renewal

# Check Nginx SSL config
sudo nginx -t
```

---

## Security Recommendations

1. **Change default passwords** in `.env.production`
2. **Use strong webhook secret** (32+ characters)
3. **Keep system updated**: `sudo apt update && sudo apt upgrade`
4. **Enable automatic security updates**:
   ```bash
   sudo apt install unattended-upgrades
   sudo dpkg-reconfigure unattended-upgrades
   ```
5. **Consider fail2ban** for SSH protection:
   ```bash
   sudo apt install fail2ban
   ```

---

## Quick Reference

| Service | Internal URL | External URL |
|---------|-------------|--------------|
| API | http://surveybot-api:8080 | https://YOUR_DOMAIN/api |
| Frontend | http://surveybot-frontend:80 | https://YOUR_DOMAIN |
| PostgreSQL | postgres:5432 | Not exposed |
| Swagger | - | https://YOUR_DOMAIN/api/swagger |
| Webhook | - | https://YOUR_DOMAIN/api/bot/webhook |

---

**Last Updated**: 2025-12-11
