# Contento Deployment Guide

This guide covers production deployment of Contento CMS using Docker (recommended) and bare metal installations. Both approaches are production-tested and fully supported.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Docker Deployment (Recommended)](#docker-deployment-recommended)
- [Bare Metal Deployment](#bare-metal-deployment)
- [Database Setup](#database-setup)
- [Redis Configuration](#redis-configuration)
- [Reverse Proxy Configuration](#reverse-proxy-configuration)
- [TLS/SSL Certificates](#tlsssl-certificates)
- [Environment Variables](#environment-variables)
- [Security Hardening](#security-hardening)
- [Monitoring and Logging](#monitoring-and-logging)
- [Backup and Recovery](#backup-and-recovery)
- [Scaling](#scaling)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

Regardless of deployment method, you need:

| Component        | Minimum          | Recommended        |
|------------------|------------------|--------------------|
| CPU              | 1 core           | 2+ cores           |
| RAM              | 512 MB           | 1 GB+              |
| Disk             | 1 GB             | 10 GB+ (for media) |
| PostgreSQL       | 15               | 17                 |
| Redis            | 6                | 7                  |
| .NET Runtime     | 10.0 (bare metal only) | 10.0          |

## Docker Deployment (Recommended)

Docker is the recommended deployment method. It packages Contento with all its dependencies and provides consistent behavior across environments.

### Quick Start with Docker Compose

The repository includes a production-ready `docker-compose.yml`.

**1. Clone the repository on your server:**

```bash
git clone https://github.com/your-org/contento.git /opt/contento
cd /opt/contento
```

**2. Create the environment file:**

```bash
cp .env.example .env
```

Edit `.env` with your production values:

```bash
# .env
POSTGRES_USER=contento
POSTGRES_PASSWORD=<generate-a-strong-password>
POSTGRES_DB=contento

REDIS_PASSWORD=<generate-a-strong-password>

CONTENTO_SITE_URL=https://your-domain.com
CONTENTO_SITE_NAME=Your Site Name

# Change this immediately after first login
CONTENTO_ADMIN_EMAIL=admin@contento.local
CONTENTO_ADMIN_PASSWORD=<your-secure-password>
```

Generate strong passwords:

```bash
openssl rand -base64 32
```

**3. Build and start the services:**

```bash
docker compose -f docker-compose.yml up -d
```

**4. Verify the deployment:**

```bash
# Check all containers are running
docker compose ps

# Check application logs
docker compose logs contento

# Test connectivity
curl -I http://localhost:5000
```

### Docker Compose Configuration

Here is a reference `docker-compose.yml` for production:

```yaml
services:
  contento:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      - ConnectionStrings__Redis=redis:6379,password=${REDIS_PASSWORD}
      - Contento__SiteUrl=${CONTENTO_SITE_URL}
      - Contento__SiteName=${CONTENTO_SITE_NAME}
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped
    volumes:
      - contento-media:/app/wwwroot/media
      - contento-plugins:/app/plugins

  postgres:
    image: postgres:17-alpine
    environment:
      - POSTGRES_USER=${POSTGRES_USER}
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=${POSTGRES_DB}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD} --maxmemory 128mb --maxmemory-policy allkeys-lru
    volumes:
      - redis-data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD}", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

volumes:
  postgres-data:
  redis-data:
  contento-media:
  contento-plugins:
```

### Dockerfile Reference

The included `Dockerfile` uses multi-stage builds for minimal image size:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Contento.dll"]
```

### Updating with Docker

```bash
cd /opt/contento

# Pull latest code
git pull origin main

# Rebuild and restart (zero-downtime with health checks)
docker compose up -d --build

# Verify the update
docker compose logs -f contento
```

### Docker Resource Limits

Add resource constraints for production stability:

```yaml
services:
  contento:
    deploy:
      resources:
        limits:
          cpus: "2.0"
          memory: 1G
        reservations:
          cpus: "0.5"
          memory: 256M
```

## Bare Metal Deployment

For environments where Docker is not available or desired.

### 1. Install the .NET 10.0 Runtime

**Ubuntu/Debian:**

```bash
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-10.0
```

**RHEL/CentOS/Fedora:**

```bash
sudo dnf install aspnetcore-runtime-10.0
```

**Windows:**

Download and install the [ASP.NET Core 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) from Microsoft.

### 2. Build the Application

On your build machine (with the .NET SDK installed):

```bash
dotnet publish -c Release -o ./publish
```

Transfer the `publish/` directory to your server:

```bash
scp -r ./publish/ user@server:/opt/contento/
```

### 3. Create a System Service

**Linux (systemd):**

Create `/etc/systemd/system/contento.service`:

```ini
[Unit]
Description=Contento CMS
After=network.target postgresql.service redis.service

[Service]
Type=notify
User=contento
Group=contento
WorkingDirectory=/opt/contento
ExecStart=/usr/bin/dotnet /opt/contento/Contento.dll
Restart=on-failure
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=contento
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=contento;Username=contento_user;Password=your_password
Environment=ConnectionStrings__Redis=localhost:6379

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
# Create a dedicated user
sudo useradd --system --no-create-home contento

# Set permissions
sudo chown -R contento:contento /opt/contento

# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable contento
sudo systemctl start contento

# Check status
sudo systemctl status contento
```

**Windows (as a Windows Service):**

Install as a Windows Service using the .NET Worker Service model, or use a tool like [NSSM](https://nssm.cc/):

```cmd
nssm install Contento "C:\Program Files\dotnet\dotnet.exe" "C:\contento\Contento.dll"
nssm set Contento AppDirectory "C:\contento"
nssm set Contento AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production" "ASPNETCORE_URLS=http://localhost:5000"
nssm start Contento
```

### 4. Verify the Installation

```bash
# Check the service is running
curl http://localhost:5000/api/v1/site

# Check logs
sudo journalctl -u contento -f
```

## Database Setup

### PostgreSQL 17 Configuration

**Create the database and user:**

```sql
-- Connect as postgres superuser
CREATE USER contento_user WITH PASSWORD 'your_secure_password';
CREATE DATABASE contento OWNER contento_user;

-- Grant permissions
\c contento
GRANT ALL ON SCHEMA public TO contento_user;
```

**Recommended `postgresql.conf` settings for Contento:**

```ini
# Memory (adjust based on available RAM)
shared_buffers = 256MB
work_mem = 16MB
maintenance_work_mem = 128MB
effective_cache_size = 768MB

# Write performance
wal_buffers = 16MB
checkpoint_completion_target = 0.9

# Connection limits
max_connections = 100

# Logging
log_min_duration_statement = 1000   # Log slow queries (>1s)
```

**Run database migrations:**

Contento uses Noundry Bowtie for migrations. They run automatically on application startup, or you can run them manually:

```bash
# With dotnet CLI
dotnet run -- migrate

# With Docker
docker compose exec contento dotnet Contento.dll migrate
```

## Redis Configuration

### Recommended Settings

Create or edit `/etc/redis/redis.conf`:

```conf
# Require authentication
requirepass your_redis_password

# Memory management
maxmemory 128mb
maxmemory-policy allkeys-lru

# Persistence (for cache recovery after restart)
save 900 1
save 300 10
save 60 10000

# Security
bind 127.0.0.1
protected-mode yes

# Performance
tcp-keepalive 300
```

Contento uses Redis for:
- Page output caching
- Session storage
- Rate limiting
- Plugin configuration caching

## Reverse Proxy Configuration

In production, always run Contento behind a reverse proxy that handles TLS termination, compression, and static file serving.

### Nginx

```nginx
upstream contento {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;

    # SSL hardening
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 1d;
    ssl_session_tickets off;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains" always;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript image/svg+xml;

    # Static files (served directly by Nginx)
    location /css/ {
        root /opt/contento/wwwroot;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    location /js/ {
        root /opt/contento/wwwroot;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    location /media/ {
        root /opt/contento/wwwroot;
        expires 7d;
        add_header Cache-Control "public";
    }

    location /img/ {
        root /opt/contento/wwwroot;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    # Application
    location / {
        proxy_pass http://contento;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_buffering off;
        proxy_request_buffering off;

        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Upload size limit (match Contento's MaxUploadSizeMb)
    client_max_body_size 10M;
}
```

### Caddy

Caddy provides automatic TLS via Let's Encrypt with minimal configuration:

```
your-domain.com {
    reverse_proxy localhost:5000

    header {
        X-Frame-Options "SAMEORIGIN"
        X-Content-Type-Options "nosniff"
        Referrer-Policy "strict-origin-when-cross-origin"
        Strict-Transport-Security "max-age=63072000; includeSubDomains"
    }

    @static {
        path /css/* /js/* /img/* /media/*
    }
    header @static Cache-Control "public, max-age=2592000"

    encode gzip

    file_server /media/* {
        root /opt/contento/wwwroot
    }
}
```

## TLS/SSL Certificates

### Let's Encrypt with Certbot

```bash
# Install certbot
sudo apt install certbot python3-certbot-nginx

# Obtain certificate (Nginx)
sudo certbot --nginx -d your-domain.com

# Auto-renewal is configured automatically
# Verify with:
sudo certbot renew --dry-run
```

### Let's Encrypt with Docker

Use the `nginx-proxy` and `acme-companion` containers, or add certbot to your compose stack:

```yaml
services:
  certbot:
    image: certbot/certbot
    volumes:
      - certbot-certs:/etc/letsencrypt
      - certbot-www:/var/www/certbot
    entrypoint: "/bin/sh -c 'trap exit TERM; while :; do certbot renew; sleep 12h & wait $${!}; done;'"
```

## Environment Variables

All configuration can be set via environment variables using double-underscore (`__`) notation for nested keys.

### Required Variables

| Variable                                | Description                              |
|-----------------------------------------|------------------------------------------|
| `ConnectionStrings__DefaultConnection`  | PostgreSQL connection string             |
| `ConnectionStrings__Redis`              | Redis connection string                  |
| `ASPNETCORE_ENVIRONMENT`                | Set to `Production`                      |

### Recommended Variables

| Variable                                | Default                  | Description                     |
|-----------------------------------------|--------------------------|---------------------------------|
| `Contento__SiteUrl`                     | `http://localhost:5000`  | Public URL (include https://)   |
| `Contento__SiteName`                    | `Contento`               | Display name                    |
| `Contento__Theme`                       | `shizen`                 | Active theme                    |
| `Contento__MaxUploadSizeMb`            | `10`                     | Max upload size                 |
| `Contento__Plugins__MemoryLimitMb`     | `64`                     | Plugin memory limit             |
| `Contento__Plugins__TimeoutSeconds`    | `5`                      | Plugin execution timeout        |
| `Contento__Plugins__MaxStatements`     | `100000`                 | Plugin statement limit          |

### Sensitive Variables

Never commit these to source control. Use environment variables, Docker secrets, or a vault.

| Variable                          | Description                              |
|-----------------------------------|------------------------------------------|
| `POSTGRES_PASSWORD`               | Database password                        |
| `REDIS_PASSWORD`                  | Redis password                           |
| `Contento__AdminPassword`        | Initial admin password                   |

## Security Hardening

### Application Level

1. **Change the default admin password immediately** after first deployment.

2. **Use HTTPS everywhere.** Set `Contento__SiteUrl` with the `https://` scheme.

3. **Set secure headers** via your reverse proxy (see Nginx/Caddy configs above).

4. **Restrict admin access** by IP if possible:

   ```nginx
   location /admin {
       allow 10.0.0.0/8;
       allow 192.168.0.0/16;
       deny all;
       proxy_pass http://contento;
   }
   ```

5. **Disable directory listing** in your reverse proxy.

### Network Level

1. **Firewall rules** — Only expose ports 80 and 443. PostgreSQL (5432) and Redis (6379) should only be accessible from the application server.

   ```bash
   # UFW example
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw allow 22/tcp
   sudo ufw enable
   ```

2. **Bind services to localhost** — PostgreSQL and Redis should listen on `127.0.0.1` only (or the Docker internal network).

3. **Use strong passwords** — Generate random passwords for all services. At least 32 characters.

### Docker Level

1. **Run containers as non-root:**

   ```dockerfile
   RUN addgroup --system contento && adduser --system --ingroup contento contento
   USER contento
   ```

2. **Use read-only filesystem where possible:**

   ```yaml
   services:
     contento:
       read_only: true
       tmpfs:
         - /tmp
       volumes:
         - contento-media:/app/wwwroot/media
   ```

3. **Scan images for vulnerabilities:**

   ```bash
   docker scout cves contento:latest
   ```

## Monitoring and Logging

### Application Logs

Contento logs to stdout by default, which Docker captures automatically.

```bash
# Docker
docker compose logs -f contento
docker compose logs --since 1h contento

# Systemd
sudo journalctl -u contento -f
sudo journalctl -u contento --since "1 hour ago"
```

### Health Check Endpoint

Contento exposes a health check at `/api/v1/site` that returns the site status. Use this for load balancer health checks and uptime monitoring.

```bash
curl -f http://localhost:5000/api/v1/site || echo "Contento is down"
```

### Monitoring with Prometheus (Optional)

If you use Prometheus, add a scrape config:

```yaml
scrape_configs:
  - job_name: 'contento'
    scrape_interval: 15s
    static_configs:
      - targets: ['localhost:5000']
```

### Log Aggregation

For production, forward logs to a centralized system:

```yaml
services:
  contento:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "5"
```

Or use syslog for integration with Datadog, Splunk, ELK, or similar:

```yaml
    logging:
      driver: syslog
      options:
        syslog-address: "tcp://loghost:514"
        tag: "contento"
```

## Backup and Recovery

### Database Backups

**Automated daily backups with pg_dump:**

Create `/opt/contento/backup.sh`:

```bash
#!/bin/bash
set -euo pipefail

BACKUP_DIR="/opt/backups/contento"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30

mkdir -p "$BACKUP_DIR"

# Dump the database
pg_dump -h localhost -U contento_user contento \
  | gzip > "$BACKUP_DIR/contento_${TIMESTAMP}.sql.gz"

# Remove old backups
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +${RETENTION_DAYS} -delete

echo "Backup completed: contento_${TIMESTAMP}.sql.gz"
```

**With Docker:**

```bash
docker compose exec postgres pg_dump -U contento contento \
  | gzip > /opt/backups/contento_$(date +%Y%m%d).sql.gz
```

**Schedule with cron:**

```bash
# Daily at 2 AM
0 2 * * * /opt/contento/backup.sh >> /var/log/contento-backup.log 2>&1
```

### Media Backups

Back up the media upload directory:

```bash
# Rsync to backup server
rsync -avz /opt/contento/wwwroot/media/ backup-server:/backups/contento-media/

# Or with Docker volumes
docker run --rm \
  -v contento-media:/data \
  -v /opt/backups:/backup \
  alpine tar czf /backup/media_$(date +%Y%m%d).tar.gz -C /data .
```

### Recovery

**Restore the database:**

```bash
# Stop Contento
sudo systemctl stop contento

# Restore from backup
gunzip < /opt/backups/contento_20260218.sql.gz | psql -h localhost -U contento_user contento

# Restart
sudo systemctl start contento
```

**With Docker:**

```bash
docker compose stop contento

gunzip < /opt/backups/contento_20260218.sql.gz \
  | docker compose exec -T postgres psql -U contento contento

docker compose start contento
```

## Scaling

### Vertical Scaling

For most Contento installations, vertical scaling (adding more CPU/RAM) is sufficient. Contento is efficient and a single instance can handle significant traffic.

| Traffic Level    | Recommended Resources          |
|------------------|---------------------------------|
| < 10K views/day  | 1 CPU, 512 MB RAM              |
| 10-100K views/day| 2 CPU, 1 GB RAM                |
| 100K+ views/day  | 4 CPU, 2 GB RAM                |

### Horizontal Scaling

For high-availability deployments, run multiple Contento instances behind a load balancer. Redis provides shared session and cache state.

```
                    ┌─────────────────┐
                    │  Load Balancer  │
                    │  (Nginx/HAProxy)│
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
        ┌─────▼─────┐ ┌─────▼─────┐ ┌─────▼─────┐
        │ Contento  │ │ Contento  │ │ Contento  │
        │ Instance 1│ │ Instance 2│ │ Instance 3│
        └─────┬─────┘ └─────┬─────┘ └─────┬─────┘
              │              │              │
              └──────────────┼──────────────┘
                             │
                    ┌────────┴────────┐
                    │                 │
              ┌─────▼─────┐   ┌──────▼─────┐
              │ PostgreSQL│   │   Redis     │
              │  Primary  │   │  (shared)   │
              └───────────┘   └────────────┘
```

Requirements for horizontal scaling:

- **Shared media storage** — Use a shared filesystem (NFS), object storage (S3/MinIO), or a CDN.
- **Shared Redis** — All instances must connect to the same Redis instance for session and cache coherence.
- **Database connection pooling** — Use PgBouncer to manage database connections across multiple instances.
- **Sticky sessions** (optional) — If not using shared sessions via Redis, configure sticky sessions on the load balancer.

## Troubleshooting

### Application will not start

**Check logs:**

```bash
# Docker
docker compose logs contento

# Systemd
sudo journalctl -u contento --no-pager -n 50
```

**Common causes:**

| Symptom                        | Likely Cause                          | Fix                                            |
|--------------------------------|---------------------------------------|-------------------------------------------------|
| Connection refused on port 5432| PostgreSQL not running                | `sudo systemctl start postgresql`               |
| Connection refused on port 6379| Redis not running                     | `sudo systemctl start redis`                    |
| `FATAL: password authentication failed` | Wrong database credentials  | Check `ConnectionStrings__DefaultConnection`    |
| `Could not load file or assembly` | Missing .NET runtime              | Install `aspnetcore-runtime-10.0`               |
| Port already in use            | Another process on port 5000          | Change `ASPNETCORE_URLS` or stop conflicting process |

### Database migration failures

```bash
# Check migration status
dotnet run -- migrate --status

# If stuck, check for lock:
psql -h localhost -U contento_user contento -c "SELECT * FROM __bowtie_migrations ORDER BY id DESC LIMIT 5;"
```

### High memory usage

- Check plugin memory limits: ensure `Contento__Plugins__MemoryLimitMb` is set appropriately.
- Review Redis `maxmemory` configuration.
- Monitor with: `docker stats` or `htop`.

### Slow response times

- Enable PostgreSQL slow query logging (`log_min_duration_statement = 500`).
- Check Redis latency: `redis-cli --latency`.
- Review Nginx access logs for large response times.
- Ensure static files are served by the reverse proxy, not the application.
