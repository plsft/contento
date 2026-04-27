# Deployment Guide

This guide covers production deployment of Contento pSEO using Docker Compose (recommended). The stack consists of the Contento application, PostgreSQL 17, and Redis 7.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Docker Compose Deployment](#docker-compose-deployment)
- [Environment Variables](#environment-variables)
- [Cloudflare Configuration](#cloudflare-configuration)
- [Nginx Reverse Proxy](#nginx-reverse-proxy)
- [Database](#database)
- [Health Check](#health-check)
- [Scaling](#scaling)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

| Component  | Minimum | Recommended |
|------------|---------|-------------|
| CPU        | 1 core  | 2+ cores    |
| RAM        | 512 MB  | 1 GB+       |
| Disk       | 2 GB    | 20 GB+      |
| PostgreSQL | 15      | 17          |
| Redis      | 6       | 7           |
| Docker     | 24+     | Latest      |

## Docker Compose Deployment

### Quick start

```bash
git clone https://github.com/your-org/contento.git /opt/contento
cd /opt/contento
cp .env.example .env
```

Edit `.env` with production values (see [Environment Variables](#environment-variables) below), then start the stack:

```bash
docker compose up -d
```

### Reference `docker-compose.yml`

```yaml
services:
  contento:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - DATABASE_CONNECTION_STRING=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      - REDIS_CONNECTION_STRING=redis:6379,password=${REDIS_PASSWORD}
      - CONTENTO_ADMIN_PASSWORD=${CONTENTO_ADMIN_PASSWORD}
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    env_file:
      - .env
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 5s
      retries: 3

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
```

### Verify the deployment

```bash
docker compose ps
docker compose logs contento
curl -f http://localhost:5000/health
```

## Environment Variables

### Required

| Variable                     | Description                                              |
|------------------------------|----------------------------------------------------------|
| `DATABASE_CONNECTION_STRING` | PostgreSQL connection string                             |
| `REDIS_CONNECTION_STRING`    | Redis connection string                                  |
| `CONTENTO_ADMIN_PASSWORD`    | Initial admin password (change after first login)        |
| `ASPNETCORE_ENVIRONMENT`     | Set to `Production`                                      |

### Google Search Console

| Variable            | Description                                         |
|---------------------|-----------------------------------------------------|
| `GSC_CLIENT_ID`     | Google Search Console OAuth client ID               |
| `GSC_CLIENT_SECRET` | Google Search Console OAuth client secret            |

### Google OAuth (user login)

| Variable              | Description                                       |
|------------------------|---------------------------------------------------|
| `GOOGLE_CLIENT_ID`     | Google OAuth 2.0 client ID                        |
| `GOOGLE_CLIENT_SECRET` | Google OAuth 2.0 client secret                    |

### SMTP (email delivery)

| Variable         | Description                              |
|------------------|------------------------------------------|
| `SMTP_HOST`      | SMTP server hostname                     |
| `SMTP_PORT`      | SMTP server port (typically `587`)       |
| `SMTP_USERNAME`  | SMTP authentication username             |
| `SMTP_PASSWORD`  | SMTP authentication password             |
| `SMTP_FROM`      | Sender email address                     |
| `SMTP_FROM_NAME` | Sender display name                      |

### S3-compatible storage

| Variable           | Description                                    |
|--------------------|------------------------------------------------|
| `S3_ENDPOINT`      | S3-compatible endpoint URL                     |
| `S3_ACCESS_KEY`    | Access key ID                                  |
| `S3_SECRET_KEY`    | Secret access key                              |
| `S3_BUCKET`        | Bucket name                                    |
| `S3_REGION`        | Region (e.g., `us-east-1`)                     |
| `S3_PUBLIC_URL`    | Public-facing URL for stored assets            |

### Application

| Variable                   | Default                 | Description                          |
|----------------------------|-------------------------|--------------------------------------|
| `CONTENTO_SITE_URL`        | `http://localhost:5000` | Public URL (include `https://`)      |
| `CONTENTO_MAX_UPLOAD_MB`   | `10`                    | Max file upload size in MB           |

## Cloudflare Configuration

Contento pSEO projects are served on subdomains (e.g., `plumbers-chicago.yourdomain.com`). Cloudflare manages DNS, SSL, and caching for both the main app and pSEO project subdomains.

### DNS setup

**Main application domain:**

| Type  | Name             | Content          | Proxy  |
|-------|------------------|------------------|--------|
| A     | `yourdomain.com` | `<server-ip>`    | Proxied |
| CNAME | `www`            | `yourdomain.com` | Proxied |

**pSEO project subdomains** (wildcard or per-project):

| Type  | Name | Content          | Proxy  |
|-------|------|------------------|--------|
| CNAME | `*`  | `yourdomain.com` | Proxied |

If wildcard DNS is not available on your Cloudflare plan, create individual CNAME records per project:

```
plumbers-chicago.yourdomain.com  CNAME  yourdomain.com  (Proxied)
dentists-miami.yourdomain.com    CNAME  yourdomain.com  (Proxied)
```

### SSL configuration

- **SSL/TLS mode:** Full (strict)
- **Edge Certificates:** Enable Universal SSL (covers wildcard on paid plans)
- **Always Use HTTPS:** On
- **Minimum TLS Version:** 1.2

### Recommended Cloudflare settings

- **Caching:** Cache static assets aggressively. Use a Page Rule or Cache Rule to bypass cache for `/admin/*` and `/api/*` paths.
- **Brotli compression:** On
- **HTTP/2 and HTTP/3:** On
- **Browser Integrity Check:** On

## Nginx Reverse Proxy

Run Contento behind Nginx for TLS termination, compression, and SSE support (used for generation progress streaming).

```nginx
upstream contento {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 80;
    server_name yourdomain.com *.yourdomain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com *.yourdomain.com;

    ssl_certificate     /etc/ssl/certs/yourdomain.com.pem;
    ssl_certificate_key /etc/ssl/private/yourdomain.com.key;
    ssl_protocols       TLSv1.2 TLSv1.3;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains" always;

    # Gzip
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml text/javascript image/svg+xml;

    # SSE support for generation progress
    # Disabling proxy buffering is required for Server-Sent Events
    location /api/ {
        proxy_pass http://contento;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Connection '';
        proxy_buffering off;
        proxy_cache off;
        proxy_read_timeout 300s;
        chunked_transfer_encoding off;
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

        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    client_max_body_size 50M;
}
```

Key points:

- The `/api/` location block disables buffering and sets `Connection ''` to support SSE streams (generation progress events).
- The wildcard `server_name` (`*.yourdomain.com`) handles all pSEO project subdomains.
- `client_max_body_size` is set to `50M` to support bulk CSV/JSON imports.

## Database

### Auto-migrations

Contento uses Noundry Bowtie for database migrations. Migrations run automatically on application startup — no manual migration step is needed.

On first boot, Bowtie creates all tables and seeds the database with:

- **150 niche taxonomies** (local services, e-commerce, SaaS, real estate, etc.)
- **10 content schemas** (local-business, product-review, comparison, how-to, etc.)
- Default admin user (using `CONTENTO_ADMIN_PASSWORD`)

### Manual migration (optional)

```bash
# With Docker
docker compose exec contento dotnet Contento.dll migrate

# Check migration status
docker compose exec contento dotnet Contento.dll migrate --status
```

### PostgreSQL tuning

Recommended `postgresql.conf` settings for Contento:

```ini
shared_buffers = 256MB
work_mem = 16MB
maintenance_work_mem = 128MB
effective_cache_size = 768MB
max_connections = 100
log_min_duration_statement = 1000
```

## Health Check

Contento exposes a health check endpoint at `GET /health`. It returns `200 OK` when the application, database, and Redis are all reachable.

```bash
curl -f http://localhost:5000/health
```

Use this endpoint for:

- Docker `HEALTHCHECK`
- Load balancer health probes
- Uptime monitoring (Uptime Kuma, Pingdom, etc.)

## Scaling

### Vertical scaling

A single Contento instance handles significant traffic. For most pSEO projects, vertical scaling is sufficient.

| Traffic level     | Recommended resources |
|-------------------|-----------------------|
| < 50K pages/day   | 1 CPU, 512 MB RAM    |
| 50-500K pages/day  | 2 CPU, 1 GB RAM     |
| 500K+ pages/day    | 4 CPU, 2 GB RAM     |

### Horizontal scaling

For high-availability deployments, run multiple Contento instances behind a load balancer. Redis provides shared state across instances.

```
                    +-------------------+
                    |  Load Balancer    |
                    |  (Nginx / ALB)    |
                    +---------+---------+
                              |
               +--------------+--------------+
               |              |              |
         +-----+-----+ +-----+-----+ +-----+-----+
         | Contento  | | Contento  | | Contento  |
         | Instance 1| | Instance 2| | Instance 3|
         +-----+-----+ +-----+-----+ +-----+-----+
               |              |              |
               +--------------+--------------+
                              |
                    +---------+---------+
                    |                   |
              +-----+-----+     +------+-----+
              | PostgreSQL |     |   Redis    |
              |  Primary   |     |  (shared)  |
              +------------+     +------------+
```

Requirements:

- **Shared Redis** — All instances connect to the same Redis for cache coherence and generation job coordination.
- **Shared storage** — Use S3-compatible storage (MinIO, AWS S3, Cloudflare R2) for generated page assets.
- **Database connection pooling** — Use PgBouncer for connection management at scale.

## Troubleshooting

### Application does not start

```bash
docker compose logs contento
```

| Symptom                               | Cause                     | Fix                                              |
|---------------------------------------|---------------------------|--------------------------------------------------|
| Connection refused on port 5432       | PostgreSQL not running    | `docker compose up postgres`                     |
| Connection refused on port 6379       | Redis not running         | `docker compose up redis`                        |
| `FATAL: password authentication failed` | Wrong DB credentials   | Check `DATABASE_CONNECTION_STRING`               |
| Port already in use                   | Conflicting process       | Change port mapping in `docker-compose.yml`      |

### Database migration failures

```bash
docker compose exec contento dotnet Contento.dll migrate --status
```

If migrations are stuck, check for advisory locks:

```sql
SELECT * FROM pg_locks WHERE locktype = 'advisory';
```

### Generation jobs not progressing

- Verify Redis is reachable: `docker compose exec redis redis-cli -a $REDIS_PASSWORD ping`
- Check SSE connection in browser DevTools (Network tab, filter by EventSource)
- Ensure Nginx is not buffering the `/api/` path (see [Nginx config](#nginx-reverse-proxy))
