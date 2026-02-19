# Contento CMS

A modern, minimalist content management system built with .NET 10.0, inspired by the Japanese aesthetic philosophy of Wabi-Sabi — finding beauty in simplicity and imperfection.

Contento combines the power of a full-featured CMS with the elegance of a thoughtfully restrained design. Built on the Noundry ecosystem, it delivers a fast, secure, and extensible platform for content creators who value substance over spectacle.

## Features

- **Wabi-Sabi Design** — Three built-in themes inspired by Japanese nature aesthetics (Shizen, Koyo, Yuki)
- **Plugin System** — Sandboxed JavaScript plugins via Jint with defined lifecycle hooks
- **Full REST API** — Complete API at `/api/v1/` for posts, categories, comments, media, and more
- **WordPress Migration** — Import your existing WordPress content via WXR format
- **Dark Mode** — Automatic dark mode support via CSS custom properties
- **RSS & Sitemap** — Built-in feed at `/feed.xml` and sitemap at `/sitemap.xml`
- **Media Management** — Upload, organize, and serve images and files
- **Layout System** — Flexible page layouts with customizable regions
- **Search** — Built-in full-text search across all content

## Tech Stack

| Component       | Technology                  |
|-----------------|-----------------------------|
| Framework       | .NET 10.0 Minimal API       |
| Pages           | Razor Pages                 |
| Frontend        | Alpine.js + TailBreeze CSS  |
| Database        | PostgreSQL 17               |
| Cache           | Redis 7                     |
| ORM             | Noundry Tuxedo              |
| Migrations      | Noundry Bowtie              |
| Auth            | Noundry AuthNZ + BCrypt.Net-Next |
| UI Components   | Noundry UI                  |
| Plugins         | Jint JavaScript Sandbox     |
| Containers      | Docker + Docker Compose     |

## Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 17](https://www.postgresql.org/download/)
- [Redis 7](https://redis.io/download/)
- (Optional) [Docker](https://www.docker.com/get-started/) and Docker Compose

### Option A: Docker (Recommended)

The fastest way to get Contento running locally:

```bash
# Clone the repository
git clone https://github.com/your-org/contento.git
cd contento

# Start all services (PostgreSQL, Redis, and Contento)
docker compose up -d

# Contento is now running at http://localhost:5000
```

Docker Compose will automatically:
- Start PostgreSQL 17 and Redis 7
- Run database migrations
- Seed the default admin account
- Launch the Contento application

### Option B: Bare Metal

1. **Configure the database**

   Create a PostgreSQL database for Contento:

   ```sql
   CREATE DATABASE contento;
   CREATE USER contento_user WITH PASSWORD 'your_secure_password';
   GRANT ALL PRIVILEGES ON DATABASE contento TO contento_user;
   ```

2. **Configure the application**

   Update `appsettings.json` (or use environment variables):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=contento;Username=contento_user;Password=your_secure_password",
       "Redis": "localhost:6379"
     }
   }
   ```

   Or set environment variables:

   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=contento;Username=contento_user;Password=your_secure_password"
   export ConnectionStrings__Redis="localhost:6379"
   ```

3. **Run database migrations**

   ```bash
   dotnet run -- migrate
   ```

4. **Start the application**

   ```bash
   dotnet run
   ```

   Contento will be available at `http://localhost:5000`.

### First Login

After starting Contento, sign in with the default administrator credentials:

- **URL:** `http://localhost:5000/admin`
- **Email:** `admin@contento.local`
- **Password:** Set via `Admin:DefaultPassword` in `appsettings.Development.json` or `CONTENTO_ADMIN_PASSWORD` env var

**Important:** Change the default password immediately after your first login.

### Exploring the Application

| URL               | Description                        |
|-------------------|------------------------------------|
| `/`               | Public-facing site                 |
| `/admin`          | Administration dashboard           |
| `/feed.xml`       | RSS feed                           |
| `/sitemap.xml`    | XML sitemap for search engines     |
| `/api/v1/`        | REST API base URL                  |

## Built-in Themes

Contento ships with three themes, each reflecting a different aspect of Japanese nature:

- **Shizen** (自然 — Nature) — Warm earth tones and organic textures. The default theme.
- **Koyo** (紅葉 — Autumn Leaves) — Rich amber and crimson palette inspired by fall foliage.
- **Yuki** (雪 — Snow) — Clean whites and cool grays evoking a winter landscape.

Switch themes from the admin dashboard under **Settings > Appearance**.

## Project Structure

```
contento/
├── Controllers/          # API controllers
├── Pages/                # Razor Pages (admin + public)
├── Models/               # Domain models
├── Services/             # Business logic
├── Migrations/           # Noundry Bowtie database migrations
├── Plugins/              # Plugin system + built-in plugins
│   ├── seo-meta/         # SEO metadata injection
│   ├── social-share/     # Social sharing buttons
│   └── reading-progress/ # Reading progress indicator
├── Themes/               # Theme definitions and assets
│   ├── shizen/
│   ├── koyo/
│   └── yuki/
├── wwwroot/              # Static assets
├── docs/                 # Documentation
├── Dockerfile            # Production container image
├── docker-compose.yml    # Local development stack
└── appsettings.json      # Application configuration
```

## Documentation

- [Plugin API Reference](docs/plugin-api.md) — Build custom plugins with the Jint sandbox
- [Theme Creation Guide](docs/theme-guide.md) — Design and package your own themes
- [Deployment Guide](docs/deployment.md) — Production deployment with Docker or bare metal
- [WordPress Migration Guide](docs/migration.md) — Import content from WordPress

## API Overview

Contento exposes a full REST API under `/api/v1/`. All endpoints return JSON.

```bash
# List published posts
curl http://localhost:5000/api/v1/posts

# Get a single post by slug
curl http://localhost:5000/api/v1/posts/my-first-post

# Search content
curl http://localhost:5000/api/v1/search?q=minimalism

# List categories
curl http://localhost:5000/api/v1/categories
```

Authenticated endpoints require a Bearer token obtained through the auth flow. See the full [API documentation](docs/plugin-api.md) for details.

## Configuration Reference

Contento is configured through `appsettings.json` or environment variables. Environment variables use double-underscore (`__`) as the section separator.

| Setting                               | Default               | Description                        |
|---------------------------------------|-----------------------|------------------------------------|
| `ConnectionStrings:DefaultConnection` | —                     | PostgreSQL connection string       |
| `ConnectionStrings:Redis`             | `localhost:6379`      | Redis connection string            |
| `Contento:SiteName`                   | `Contento`            | Site display name                  |
| `Contento:SiteUrl`                    | `http://localhost:5000` | Public URL of the site           |
| `Contento:Theme`                      | `shizen`              | Active theme name                  |
| `Contento:MaxUploadSizeMb`           | `10`                  | Maximum file upload size           |
| `Contento:Plugins:MemoryLimitMb`     | `64`                  | Jint sandbox memory limit          |
| `Contento:Plugins:TimeoutSeconds`    | `5`                   | Jint sandbox execution timeout     |
| `Contento:Plugins:MaxStatements`     | `100000`              | Jint sandbox max statement count   |

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Make your changes and add tests
4. Run the test suite (`dotnet test`)
5. Submit a pull request

## License

See [LICENSE](LICENSE) for details.
