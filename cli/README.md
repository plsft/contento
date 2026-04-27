# contento

CLI for the [Contento](https://contentocms.com) pSEO platform. Designed for both humans (rich terminal UI) and AI agents (`--json` mode).

## Installation

```bash
npm install -g contento
```

Requires Node.js >= 18.

## Quick Start

```bash
# Authenticate
contento login

# List your projects
contento projects list

# Create a new pSEO project
contento projects create --name "My Blog" --domain example.com --subdomain blog

# Browse niche templates
contento niches list --search "saas"

# Fork a niche
contento niches fork <niche-id> --name "My SaaS Niche"

# Create a collection
contento collections create \
  --project <project-id> \
  --schema blog-post \
  --niches id1,id2,id3 \
  --url-pattern "/blog/{slug}" \
  --title-template "{keyword} - Complete Guide"

# Generate content (streams progress)
contento collections generate <collection-id>

# Publish
contento publish <collection-id> --mode immediate

# Check analytics
contento analytics summary <project-id> --days 30
contento analytics top-pages <project-id>
```

## Commands

| Command | Description |
|---------|-------------|
| `contento login` | Authenticate with API key |
| `contento logout` | Remove stored credentials |
| `contento whoami` | Show authentication status |
| `contento projects list` | List all projects |
| `contento projects create` | Create a new project |
| `contento projects status <id>` | Get project details |
| `contento niches list` | Browse niche templates |
| `contento niches fork <id>` | Fork a niche template |
| `contento schemas list` | List content schemas |
| `contento collections list` | List collections |
| `contento collections create` | Create a collection |
| `contento collections generate <id>` | Generate content (SSE streaming) |
| `contento publish <id>` | Publish a collection |
| `contento analytics summary <id>` | Analytics overview |
| `contento analytics top-pages <id>` | Top performing pages |
| `contento analytics zero-traffic <id>` | Pages with no traffic |
| `contento analytics export <id>` | Export analytics as CSV |
| `contento domains add` | Add a custom domain |
| `contento domains verify <id>` | Verify DNS configuration |
| `contento domains status <id>` | Check domain/SSL status |

## Global Flags

| Flag | Description |
|------|-------------|
| `--json` | Machine-readable JSON output |
| `--api-url <url>` | Override the API base URL |
| `--no-color` | Disable colored output |
| `-v, --verbose` | Enable verbose logging |
| `-V, --version` | Show version number |
| `-h, --help` | Show help |

## Agent / CI Integration

The CLI is designed to be used by AI agents and in CI pipelines. Use `--json` for structured output:

```bash
# Non-interactive login
contento login --api-key "$CONTENTO_API_KEY"

# All commands support --json
contento projects list --json
contento collections generate <id> --json --no-wait

# JSON errors go to stderr, data goes to stdout
contento analytics summary <id> --json 2>/dev/null | jq '.totalClicks'
```

### JSON Output Format

**Success responses** are written to stdout:
```json
{
  "success": true,
  "message": "Project created",
  "data": { "id": "proj_abc123" }
}
```

**Errors** are written to stderr:
```json
{
  "error": true,
  "message": "Not authenticated"
}
```

**Progress events** (SSE streaming) emit NDJSON lines:
```json
{"type":"progress","label":"Generating","current":5,"total":100}
{"type":"progress","label":"Generating","current":100,"total":100,"done":true}
```

### Table output in JSON mode

Commands that render tables will output a JSON array of objects instead:
```json
[
  { "ID": "proj_1", "Name": "My Blog", "Status": "active" },
  { "ID": "proj_2", "Name": "Docs Site", "Status": "active" }
]
```

## Configuration

Credentials are stored in `~/.contento/config.json`:

```json
{
  "apiKey": "cnt_...",
  "baseUrl": "https://api.contentocms.com/v1"
}
```

## Development

```bash
# Install dependencies
npm install

# Build
npm run build

# Watch mode
npm run dev

# Run locally
node dist/bin/contento.js --help
```

## License

MIT
