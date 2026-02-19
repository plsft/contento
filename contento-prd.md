# Contento — Product Requirements Document

> **"Content, made happy."** A next-generation content management system that doesn't feel like a CMS.
> Contento (Spanish for "happy") is a writer-first, security-first, performance-first platform designed to kill WordPress.

---

## Document Metadata

| Field | Value |
|---|---|
| **Product** | Contento CMS Platform |
| **Version** | 1.0.0 |
| **Author** | George / Plurral |
| **Stack** | C# / .NET 10.0 / Razor Pages / Alpine.js for reactivity / PostgreSQL / Redis / TypeScript plugins |
| **Design System** | Noundry.UI + Tailwind v4 + TailBreeze |
| **ORM** | Tuxedo |
| **Auth** | AuthNZ |
| **UI** | Noundry.UI | 
| **CSS+Styling** | Tailwind V4, via Noundry Tailbreeze | 
| **Validation** | Assertive + Guardian |
| **Testing** | NUnit |
| **Aesthetic** | Minimalist Japanese (Wabi-Sabi) — clean, quiet, intentional |

---

## PART 1 — VISION & PRINCIPLES

### 1.1 Product Vision

Contento is what happens when you strip away every piece of CMS friction and rebuild from the writer's perspective. The admin experience should feel like opening a beautiful notebook — not logging into a dashboard. Writing a post should feel like Medium. Designing a layout should feel like Figma. Publishing should feel like pressing "send."

WordPress won the CMS war by being extensible. Contento wins the next war by being **delightful, secure, and fast** while remaining extensible.

### 1.2 Core Principles

1. **Writer UX is sacred.** Every pixel in the authoring experience serves the writer. No clutter. No cognitive load. The interface disappears and the words remain.

2. **Performance is non-negotiable.** Sub-100ms server responses. Redis-cached everything. Pre-rendered static output where possible. No page load should ever make a visitor wait.

3. **Security is foundational, not bolted on.** Every plugin runs sandboxed. Every input is validated. Every route is authorized. CSP headers, rate limiting, and audit logging are defaults, not options.

4. **Extensibility through discipline.** The plugin system is powerful but constrained. Plugins cannot access the database directly. They communicate through a defined API surface. Security review is built into the plugin lifecycle.

5. **Beauty through restraint.** The Japanese aesthetic principle of Ma (間) — negative space as a design element. Every UI element earns its place.

### 1.3 Design Language — Wabi-Sabi Minimalism

The entire admin system follows a Japanese-inspired minimalist aesthetic:

- **Color palette:** Near-white backgrounds (#FAFAF8), warm stone grays (#6B6B6B), ink black (#1A1A1A), and a single accent — deep indigo (#3D5A80) used sparingly like a hanko seal stamp
- **Typography:** A refined serif for headings (Noto Serif JP or similar), a clean sans-serif for body (Noto Sans JP), monospace for code blocks
- **Spacing:** Generous whitespace everywhere. Content breathes. Padding is measured in multiples of 8px
- **Borders:** Hairline (1px) borders in warm gray. No heavy outlines. No box shadows except on floating elements (and those are subtle, diffused)
- **Transitions:** Gentle easing (ease-out, 200-300ms). Nothing snaps. Everything glides
- **Icons:** Thin-stroke line icons. No filled icons. No color on icons except the indigo accent on active states
- **Empty states:** Beautiful. A single line of text, centered, in light gray. Perhaps a minimal ink-wash illustration
- **Reference implementation:** Study `C:/work/phoenix/src/phoenix.web` for the tone and feel. Match that sensibility

---

## PART 2 — ARCHITECTURE OVERVIEW

### 2.1 Solution Structure

```
C:/work/contento/
├── src/
│   ├── Contento.Core/              # Domain models, interfaces, enums, constants
│   ├── Contento.Data/              # Tuxedo ORM mappings, repositories, migrations
│   ├── Contento.Services/          # Business logic, content pipeline, caching
│   ├── Contento.Plugins/           # Plugin host, sandbox, API surface
│   ├── Contento.Web/               # Razor Pages admin UI + API controllers
│   ├── Contento.Public/            # Public-facing rendered site (visitor experience)
│   └── Contento.Tests/             # NUnit test project
├── plugins/
│   ├── contento-plugin-sdk/        # TypeScript SDK for plugin authors
│   └── examples/                   # Example plugins (SEO, analytics, social sharing)
├── tools/
│   └── contento-cli/               # CLI for scaffolding, plugin management
├── docs/
│   ├── plugin-api.md
│   ├── theme-guide.md
│   └── deployment.md
├── Contento.sln
└── README.md
```

### 2.2 Prerequisites — Read Before Building

**CRITICAL: Before writing ANY code, Claude Code MUST read and internalize these local resources:**

```
1. C:/work/noundry/llm/                    — Full Noundry LLM design guide. Read ALL files.
2. C:/work/noundry/noundryStarter/          — Noundry app starter template. Use as the foundation
                                              for project setup, NuGet references, and conventions.
3. C:/work/phoenix/src/phoenix.web/         — Reference for minimalist Japanese aesthetic in Razor.
                                              Study the layouts, partials, CSS approach, and tone.
```

**NuGet packages to reference (from Noundry ecosystem):**

| Package | Purpose |
|---|---|
| **Noundry.UI** | UI component library — use for all Razor components and layouts |
| **Tuxedo** | ORM for PostgreSQL — use for ALL database access. No raw SQL except for migrations |
| **Bowtie** | Service layer patterns, dependency injection helpers |
| **AuthNZ** | Authentication and authorization — handles login, roles, permissions, JWT, API keys |
| **TailBreeze** | Tailwind CSS v4 integration for .NET — handles build pipeline |
| **Assertive** | Input validation library — use for ALL user input validation |
| **Guardian** | Security middleware — CSP, rate limiting, CORS, anti-forgery, audit logging |

### 2.3 Technology Decisions

| Layer | Technology | Rationale |
|---|---|---|
| **Backend API** | ASP.NET Core 10 Minimal APIs + Razor Pages | Razor for admin UI, Minimal APIs for plugin/public API |
| **Database** | PostgreSQL 17+ | JSONB for flexible content, full-text search, proven scale |
| **Cache** | Redis 7+ | Session, rendered page cache, plugin data cache, rate limiting |
| **Admin Frontend** | Razor Pages + Tailwind v4 + Alpine.js | Server-rendered, fast, progressive enhancement |
| **Markdown Engine** | Markdig (C#) | Extensible, fast, supports custom extensions |
| **Editor Frontend** | Custom Markdown editor (vanilla JS/TS) | Medium-like WYSIWYG feel with markdown underneath. Must be performant. Can be custom built. |
| **Plugin Runtime** | V8 via Jint or similar JS engine | Sandboxed JavaScript/TypeScript execution. Open to suggestions on this. Deep reseach as I want a flexible arch. C# will be considered in addition to JS/TS |
| **Search** | PostgreSQL tsvector + trigram | Built-in, no external dependency |
| **File Storage** | S3-compatible abstraction via SharpGrip.net/S3 adapter | Images, media, plugin assets |


---

## PART 3 — DATA MODEL

### 3.1 Core Entities (Tuxedo ORM Mappings)

All entities inherit from Noundry base classes per the starter template conventions. Use Tuxedo for all mappings. Use V7 Guids for UUIDs. Always pass UUIDs as strings in paramaters. Always specify length of strings. 
Ensure string lengths in entity models in C# are correct for purpose. Ensure VERY long strings are stored in postgres as TEXT. 

#### Sites

```
Table: sites
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
name            VARCHAR(200) NOT NULL
slug            VARCHAR(200) NOT NULL UNIQUE
domain          VARCHAR(255)
tagline         VARCHAR(1024)
locale          VARCHAR(16) DEFAULT 'en-US'
timezone        VARCHAR(50) DEFAULT 'UTC'
settings        JSONB DEFAULT '{}'
theme_id        UUID FK -> themes.id
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
created_by      UUID FK -> users.id
```

#### Users (extends AuthNZ user)

```
Table: users (extends AuthNZ base user table)
─────────────────────────────────────
id              UUID PK
display_name    VARCHAR(300) NOT NULL
bio             TEXT
avatar_url      VARCHAR(500)
role            VARCHAR(50) NOT NULL  -- 'owner', 'admin', 'editor', 'author', 'viewer'
preferences     JSONB DEFAULT '{}'
```

#### Posts

```
Table: posts
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
site_id         UUID FK -> sites.id NOT NULL
title           VARCHAR(500) NOT NULL
slug            VARCHAR(500) NOT NULL
subtitle        VARCHAR(500)
excerpt         TEXT
body_markdown   TEXT NOT NULL
body_html       TEXT                    -- Pre-rendered HTML from markdown
cover_image_url VARCHAR(750)
author_id       UUID FK -> users.id NOT NULL
status          VARCHAR(20) NOT NULL DEFAULT 'draft'
                -- 'draft', 'review', 'scheduled', 'published', 'archived', 'unlisted'
visibility      VARCHAR(20) NOT NULL DEFAULT 'public'
                -- 'public', 'unlisted', 'members_only', 'password_protected'
password_hash   VARCHAR(200)            -- For password_protected visibility
published_at    TIMESTAMPTZ
scheduled_at    TIMESTAMPTZ
featured        BOOLEAN DEFAULT FALSE
reading_time_minutes INT
word_count      INT
meta_title      VARCHAR(400)
meta_description VARCHAR(1000)
og_image_url    VARCHAR(1000)
canonical_url   VARCHAR(900)
schema_markup   JSONB                   -- JSON-LD structured data
tags            TEXT[]                  -- PostgreSQL array for fast filtering
category_id     UUID FK -> categories.id
layout_id       UUID FK -> layouts.id   -- Which layout template to use
custom_css      TEXT                    -- Per-post CSS overrides
custom_js       TEXT                    -- Per-post JS (sandboxed)
settings        JSONB DEFAULT '{}'      -- Extensible settings (comments on/off, etc.)
version         INT NOT NULL DEFAULT 1
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()

INDEXES:
  - idx_posts_site_status ON (site_id, status)
  - idx_posts_site_slug ON (site_id, slug) UNIQUE
  - idx_posts_published ON (site_id, published_at DESC) WHERE status = 'published'
  - idx_posts_tags ON tags USING GIN
  - idx_posts_fts ON to_tsvector('english', title || ' ' || coalesce(body_markdown, '')) USING GIN
```

#### Post Versions (Full revision history)

```
Table: post_versions
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
post_id         UUID FK -> posts.id NOT NULL
version         INT NOT NULL
title           VARCHAR(900)
body_markdown   TEXT NOT NULL
body_html       TEXT
change_summary  TEXT
changed_by      UUID FK -> users.id NOT NULL
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()

UNIQUE(post_id, version)
```

#### Categories

```
Table: categories
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
site_id         UUID FK -> sites.id NOT NULL
name            VARCHAR(200) NOT NULL
slug            VARCHAR(200) NOT NULL
description     TEXT
parent_id       UUID FK -> categories.id  -- Hierarchical
sort_order      INT DEFAULT 0
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()

UNIQUE(site_id, slug)
```

#### Comments (Threaded)

```
Table: comments
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
post_id         UUID FK -> posts.id NOT NULL
parent_id       UUID FK -> comments.id   -- NULL = top-level, non-null = reply
author_name     VARCHAR(200) NOT NULL
author_email    VARCHAR(300)
author_url      VARCHAR(500)
author_user_id  UUID FK -> users.id      -- If authenticated commenter
body_markdown   TEXT NOT NULL
body_html       TEXT
status          VARCHAR(20) NOT NULL DEFAULT 'pending'
                -- 'pending', 'approved', 'spam', 'trash'
ip_address      INET
user_agent      VARCHAR(500)
likes_count     INT DEFAULT 0
depth           INT NOT NULL DEFAULT 0   -- Precomputed nesting depth (max 3)
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()

INDEXES:
  - idx_comments_post ON (post_id, status, created_at)
  - idx_comments_parent ON (parent_id)
```

#### Layouts (Page Layout Templates)

```
Table: layouts
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
site_id         UUID FK -> sites.id NOT NULL
name            VARCHAR(200) NOT NULL
slug            VARCHAR(100) NOT NULL
description     TEXT
is_default      BOOLEAN DEFAULT FALSE
structure       JSONB NOT NULL          -- Layout component tree (see Section 5)
head_content    TEXT                    -- Custom <head> injections
custom_css      TEXT
custom_js       TEXT
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()

UNIQUE(site_id, slug)
```

#### Layout Components

```
Table: layout_components
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
layout_id       UUID FK -> layouts.id NOT NULL
region          VARCHAR(50) NOT NULL
                -- 'header', 'head', 'menu', 'meta', 'left_nav', 'right_nav', 'body', 'footer'
content_type    VARCHAR(30) NOT NULL
                -- 'markdown', 'html', 'widget', 'plugin', 'navigation', 'dynamic'
content         TEXT                    -- Markdown or HTML content for the region
settings        JSONB DEFAULT '{}'      -- Component-specific config
sort_order      INT DEFAULT 0
is_visible      BOOLEAN DEFAULT TRUE
css_classes     TEXT            -- Additional Tailwind classes
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
```

#### Themes

```
Table: themes
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
name            VARCHAR(400) NOT NULL
slug            VARCHAR(200) NOT NULL UNIQUE
description     TEXT
version         VARCHAR(100)
author          VARCHAR(200)
css_variables   JSONB                   -- CSS custom properties
base_layout_id  UUID FK -> layouts.id
thumbnail_url   VARCHAR(500)
is_active       BOOLEAN DEFAULT FALSE
settings        JSONB DEFAULT '{}'
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
```

#### Traffic Metrics

```
Table: page_views
─────────────────────────────────────
id              BIGSERIAL PK
post_id         UUID FK -> posts.id
session_id      VARCHAR(100)
ip_address      INET
country_code    VARCHAR(2)
referrer        VARCHAR(1000)
user_agent      TEXT
device_type     VARCHAR(20)             -- 'desktop', 'mobile', 'tablet'
utm_source      VARCHAR(500)
utm_medium      VARCHAR(500)
utm_campaign    VARCHAR(500)
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()

-- Partitioned by month for performance
PARTITION BY RANGE (created_at)

INDEXES:
  - idx_pageviews_post_date ON (post_id, created_at DESC)
```

#### Traffic Aggregates (Materialized, refreshed hourly)

```
Table: traffic_daily
─────────────────────────────────────
post_id         UUID NOT NULL
date            DATE NOT NULL
views           INT DEFAULT 0
unique_visitors INT DEFAULT 0
avg_time_on_page_seconds INT
bounce_rate     DECIMAL(5,2)
top_referrers   JSONB
top_countries   JSONB

PRIMARY KEY (post_id, date)
```

#### Plugins

```
Table: installed_plugins
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
site_id         UUID FK -> sites.id NOT NULL
name            VARCHAR(200) NOT NULL
slug            VARCHAR(100) NOT NULL
version         VARCHAR(20) NOT NULL
author          VARCHAR(200)
description     TEXT
entry_point     VARCHAR(500) NOT NULL   -- Path to main JS/TS file
permissions     TEXT[]                  -- Declared permissions
settings        JSONB DEFAULT '{}'
is_enabled      BOOLEAN DEFAULT TRUE
installed_at    TIMESTAMPTZ NOT NULL DEFAULT now()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()

UNIQUE(site_id, slug)
```

#### Media Library

```
Table: media
─────────────────────────────────────
id              UUID PK DEFAULT uuidv7()
site_id         UUID FK -> sites.id NOT NULL
filename        VARCHAR(500) NOT NULL
original_name   VARCHAR(500) NOT NULL
mime_type       VARCHAR(100) NOT NULL
file_size       BIGINT NOT NULL
width           INT
height          INT
alt_text        VARCHAR(500)
caption         TEXT
storage_path    VARCHAR(1000) NOT NULL
thumbnail_path  VARCHAR(1000)
uploaded_by     UUID FK -> users.id NOT NULL
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
```

#### Audit Log

```
Table: audit_log
─────────────────────────────────────
id              BIGSERIAL PK
site_id         UUID FK -> sites.id
user_id         UUID FK -> users.id
action          VARCHAR(100) NOT NULL   -- 'post.publish', 'user.login', 'plugin.install', etc.
entity_type     VARCHAR(50)
entity_id       UUID
details         JSONB
ip_address      INET
created_at      TIMESTAMPTZ NOT NULL DEFAULT now()

-- Partitioned by month
PARTITION BY RANGE (created_at)
```

### 3.2 Redis Cache Strategy

```
Cache Keys:
─────────────────────────────────────
site:{siteId}:config                    -- Site configuration (TTL: 1 hour)
site:{siteId}:layout:{layoutId}         -- Rendered layout shell (TTL: 30 min)
site:{siteId}:post:{slug}              -- Rendered post HTML (TTL: 15 min, invalidate on edit)
site:{siteId}:post:{slug}:meta         -- Post metadata for listings (TTL: 15 min)
site:{siteId}:menu                      -- Navigation menus (TTL: 1 hour)
site:{siteId}:feed:recent              -- Recent posts feed (TTL: 5 min)
site:{siteId}:feed:category:{slug}     -- Category feeds (TTL: 5 min)
post:{postId}:comments                  -- Comment tree (TTL: 5 min, invalidate on new comment)
post:{postId}:stats                     -- Traffic summary (TTL: 1 min)
user:{userId}:session                   -- User session data (TTL: per session config)
ratelimit:{ip}:{endpoint}              -- Rate limiting counters (TTL: window size)
plugin:{pluginId}:data:{key}           -- Plugin-scoped cached data (TTL: plugin-defined)
```

---

## PART 4 — ADMIN UI / WRITER EXPERIENCE

### 4.1 Admin Routes

```
/admin                          → Dashboard (writing stats, recent drafts, traffic overview)
/admin/posts                    → Post list with status tabs (draft, published, scheduled, archived)
/admin/posts/new                → New post editor (the core writing experience)
/admin/posts/{id}/edit          → Edit existing post
/admin/posts/{id}/preview       → Full preview in layout context
/admin/posts/{id}/settings      → Post metadata, SEO, scheduling, visibility
/admin/posts/{id}/history       → Version history with diff view
/admin/posts/{id}/metrics       → Traffic analytics for this post
/admin/media                    → Media library (grid view, drag-drop upload)
/admin/comments                 → Comment moderation queue
/admin/layouts                  → Layout designer
/admin/layouts/{id}/edit        → Visual layout editor
/admin/themes                   → Theme browser and settings
/admin/categories               → Category management
/admin/plugins                  → Plugin marketplace and management
/admin/plugins/{id}/settings    → Plugin configuration
/admin/settings                 → Site settings (general, SEO, security, integrations)
/admin/settings/security        → Security settings, API keys, rate limits
/admin/users                    → User management and roles
/admin/audit                    → Audit log viewer
```

### 4.2 The Writing Experience — "Like Medium, but yours"

This is the MOST IMPORTANT part of Contento. The post editor must be transcendent. Use Alpine.js natively for all reactivity. Make it BEAUTIFUL

#### Editor Design Specifications

**Overall feel:** A blank page. Warm off-white background (#FAFAF8). A gently pulsing cursor. Nothing else until the writer starts typing. The toolbar is invisible until text is selected or the writer hovers near the top.

**Layout:**
- Full-width centered content column (max-width: 720px)
- Title field: Large, clean, no border. Placeholder text: "Title" in light gray. Renders as an `<h1>` automatically
- Subtitle field: Appears on Enter after title. Smaller, lighter. Placeholder: "Tell your story..."
- Body: Markdown-powered but WYSIWYG-rendered. The writer types markdown but sees the rendered result immediately
- No visible mode toggle — the preview IS the editor

**Toolbar behavior:**
- **Floating toolbar:** Appears when text is selected. Options: Bold, Italic, H2, H3, Quote, Link, Code. Styled as a floating pill with hairline border and subtle shadow
- **Side toolbar (+):** Appears on empty lines (left margin). Click to reveal: Image upload, Embed, Code block, Divider, Table. Animated reveal (scale + fade)
- **Keyboard shortcuts:** Cmd/Ctrl+B (bold), Cmd/Ctrl+I (italic), Cmd/Ctrl+K (link), Cmd/Ctrl+Shift+H (heading cycle), Tab in code blocks

**Markdown features:**
- GitHub-flavored markdown full support
- Syntax-highlighted code blocks with language selector
- Image upload with drag-drop, paste from clipboard, or URL. Images render inline with subtle fade-in animation
- Embedded content: YouTube, Twitter/X, CodePen via URL paste (auto-detected)
- Tables with visual editor overlay
- Footnotes
- Custom callout blocks: `> [!note]`, `> [!warning]`, `> [!tip]`
- Table of contents auto-generated from headings (toggleable)
- Math/LaTeX rendering (KaTeX)

**Autosave:** Every 30 seconds or on pause in typing (debounced 3 seconds). Visual indicator: tiny dot in top-right that briefly pulses green on save. No disruptive "Saved!" banners.

**Word count & reading time:** Bottom-left, subtle gray text. Updates in real-time.

**Cover image:** Click area above title. Drag-drop or click to upload. Shows as full-bleed banner in preview.

#### Post Settings Panel

Slides in from the right (like a drawer, 400px wide). Sections:

1. **Publication** — Status dropdown, scheduled date/time picker, visibility toggle
2. **SEO** — Meta title (with character count), meta description (with character count), OG image, canonical URL
3. **Taxonomy** — Category selector, tag input (typeahead with existing tags)
4. **Layout** — Layout template selector with thumbnail previews
5. **URL** — Slug editor with auto-generation from title
6. **Comments** — Toggle comments on/off, default moderation setting
7. **Schema** — JSON-LD structured data editor (with templates for Article, BlogPosting, etc.)
8. **Advanced** — Custom CSS, custom JS, featured toggle

### 4.3 Dashboard

The dashboard is the admin landing page. It should feel calm and informative, not overwhelming.

**Layout:**
- **Top section:** Greeting with writer's name and current date. Quick-action button: "New Post" (large, indigo, centered)
- **Recent drafts:** Compact list of last 5 drafts with title, last edited timestamp, word count
- **Published this week/month:** Simple count with sparkline trend
- **Traffic overview:** Minimal chart (last 30 days). Total views, unique visitors, top post
- **Recent comments:** Last 5 pending comments requiring moderation

### 4.4 Comment System

**Visitor-facing:**
- Clean comment form at bottom of each post (if enabled)
- Name, email (optional), website (optional), comment body (markdown supported)
- Threaded replies up to 3 levels deep. Visual indentation with thin left-border lines
- Timestamp displayed as relative time ("2 hours ago")
- Honeypot + rate limiting for spam (no CAPTCHA — it's ugly)
- Optional: require email verification for first-time commenters

**Admin moderation:**
- `/admin/comments` — Queue view: pending, approved, spam, trash
- Batch actions: approve, mark spam, delete
- Quick-reply from moderation view
- Per-post comment metrics: total, pending, approved

---

## PART 5 — LAYOUT DESIGN SYSTEM

### 5.1 Core Regions

Every page in Contento is composed of these defined regions, arranged via the layout designer:

| Region | Description | Content Types |
|---|---|---|
| `head` | Injected into `<head>` — meta tags, stylesheets, scripts | HTML, plugin output |
| `header` | Top of page — site branding, logo, tagline | Markdown, HTML, widget |
| `menu` | Navigation — primary site navigation | Navigation (auto from categories/pages), custom markdown |
| `meta` | Post metadata bar — author, date, reading time, tags | Dynamic (auto-populated from post data) |
| `left_nav` | Optional left sidebar — table of contents, category list | Markdown, widget, dynamic |
| `body` | Main content area — the post/page content | Post content (markdown rendered), page content |
| `right_nav` | Optional right sidebar — related posts, newsletter signup, ads | Markdown, widget, plugin |
| `footer` | Bottom of page — copyright, links, social icons | Markdown, HTML, widget |

### 5.2 Layout Designer

**Route:** `/admin/layouts/{id}/edit`

A visual drag-and-drop layout editor. NOT a full page builder (that's scope creep) — it's a region arranger and content populator.

**Interface:**
- **Canvas area:** Shows the page structure as a grid of regions. Each region is a labeled box that can be resized (column spans in a 12-column grid)
- **Region palette:** Left sidebar listing all available regions. Drag onto canvas to add
- **Region editor:** Click a region to open its editor panel (right sidebar). Configure:
  - Content type (markdown, HTML, widget, navigation, dynamic, plugin)
  - Content (markdown editor for static content, widget picker for widgets)
  - Visibility rules (show/hide on mobile, show only on certain post types)
  - CSS classes (Tailwind utility classes)
  - Padding and margin (preset sizes: none, sm, md, lg, xl)
- **Preview button:** Opens a live preview in a new tab
- **Responsive toggle:** Switch between desktop/tablet/mobile views

**Layout structure stored as JSONB:**

```json
{
  "grid": "12-col",
  "rows": [
    {
      "regions": [
        { "region": "header", "cols": 12 }
      ]
    },
    {
      "regions": [
        { "region": "menu", "cols": 12 }
      ]
    },
    {
      "regions": [
        { "region": "meta", "cols": 12 }
      ]
    },
    {
      "regions": [
        { "region": "left_nav", "cols": 3, "responsive": { "mobile": "hidden" } },
        { "region": "body", "cols": 6 },
        { "region": "right_nav", "cols": 3, "responsive": { "mobile": "hidden" } }
      ]
    },
    {
      "regions": [
        { "region": "footer", "cols": 12 }
      ]
    }
  ],
  "defaults": {
    "gap": "md",
    "maxWidth": "1280px",
    "padding": "lg"
  }
}
```

### 5.3 Built-in Widgets

Widgets are pre-built components that can be placed in any region:

| Widget | Description |
|---|---|
| `recent-posts` | List of N most recent published posts |
| `category-list` | Hierarchical category tree |
| `tag-cloud` | Visual tag cloud |
| `search-box` | Full-text search input |
| `newsletter-signup` | Email capture form |
| `table-of-contents` | Auto-generated from post headings |
| `author-bio` | Author card with avatar and bio |
| `related-posts` | Posts in same category or with overlapping tags |
| `social-links` | Social media icon links |
| `custom-html` | Raw HTML injection (sanitized) |

---

## PART 6 — PLUGIN ARCHITECTURE

### 6.1 Design Philosophy

Plugins are powerful but caged. They can extend Contento's functionality without compromising security or stability. 
Here, I would like to explore the notion of typescript/javascript based plugins, but am 
willing to also have C# based architecture for plug-ins. Open to suggestions on this topic. 

**Key constraints:**
- Plugins run in a sandboxed JavaScript/TypeScript runtime (no direct file system or database access)
- All data access goes through the Plugin API (HTTP-like interface within the sandbox)
- Plugins declare their required permissions upfront. Users approve permissions on install
- Plugin code is reviewed/signed for the official marketplace (open for self-hosted)
- Plugins CANNOT modify core Contento UI. They can only render into designated plugin slots

### 6.2 Plugin SDK (TypeScript)

```
plugins/contento-plugin-sdk/
├── package.json
├── tsconfig.json
├── src/
│   ├── index.ts                    # Main exports
│   ├── types.ts                    # All type definitions
│   ├── plugin.ts                   # Plugin base class
│   ├── api.ts                      # API client for sandbox
│   ├── hooks.ts                    # Lifecycle hooks
│   ├── ui.ts                       # UI component helpers
│   └── storage.ts                  # Plugin-scoped storage
└── examples/
    ├── seo-plugin/
    ├── analytics-plugin/
    └── social-share-plugin/
```

**Plugin manifest (`contento-plugin.json`):**

```json
{
  "name": "My Plugin",
  "slug": "my-plugin",
  "version": "1.0.0",
  "author": "Author Name",
  "description": "What this plugin does",
  "entry": "dist/index.js",
  "permissions": [
    "posts:read",
    "posts:meta:write",
    "settings:read",
    "ui:sidebar",
    "ui:post-footer",
    "hooks:post:beforePublish",
    "hooks:post:afterPublish",
    "storage:read",
    "storage:write"
  ],
  "settings": [
    {
      "key": "api_key",
      "type": "string",
      "label": "API Key",
      "required": true,
      "sensitive": true
    }
  ]
}
```

**Plugin lifecycle hooks:**

```typescript
import { ContentoPlugin, PostHook, RenderHook } from '@contento/sdk';

export default class MyPlugin extends ContentoPlugin {
  // Called when plugin is first installed
  async onInstall(): Promise<void> { }

  // Called when plugin is enabled
  async onEnable(): Promise<void> { }

  // Called before a post is published — can modify or reject
  async onBeforePublish(post: PostHook): Promise<PostHook | false> {
    // Return false to prevent publishing
    // Return modified post to alter before publish
    return post;
  }

  // Called after a post is published
  async onAfterPublish(post: PostHook): Promise<void> { }

  // Render into a designated UI slot
  async renderSidebar(context: RenderContext): Promise<string> {
    return '<div class="my-plugin-widget">...</div>';
  }

  // Render into post footer slot
  async renderPostFooter(context: RenderContext): Promise<string> {
    const shares = await this.api.get(`/social/shares/${context.postId}`);
    return `<div class="share-counts">${shares.total} shares</div>`;
  }
}
```

### 6.3 Permission Model

| Permission | Scope |
|---|---|
| `posts:read` | Read post content and metadata |
| `posts:meta:write` | Write to post metadata fields (not body) |
| `comments:read` | Read comments |
| `comments:write` | Create/modify comments |
| `settings:read` | Read site settings |
| `storage:read` | Read from plugin-scoped storage |
| `storage:write` | Write to plugin-scoped storage |
| `ui:sidebar` | Render in sidebar slot |
| `ui:post-footer` | Render below post content |
| `ui:post-header` | Render above post content |
| `ui:admin-panel` | Add admin panel page |
| `hooks:post:*` | Subscribe to post lifecycle hooks |
| `hooks:comment:*` | Subscribe to comment lifecycle hooks |
| `http:outbound` | Make external HTTP requests (requires explicit URLs) |

**NEVER GRANTED:**
- Direct database access
- File system access
- User credential access
- Core UI modification
- Admin route hijacking

---

## PART 7 — PUBLIC SITE RENDERING

### 7.1 Rendering Pipeline

```
Request → Route Match → Redis Cache Check
  ├── HIT  → Return cached HTML (< 5ms)
  └── MISS → Load Layout → Load Post → Render Regions
              → Merge CSS/JS → Cache Result → Return HTML
```

### 7.2 Public Routes

```
/                               → Homepage (configurable: recent posts, static page, or custom)
/{post-slug}                    → Individual post
/category/{slug}                → Category archive
/tag/{slug}                     → Tag archive
/author/{slug}                  → Author archive
/page/{slug}                    → Static page
/search?q={query}               → Search results
/feed.xml                       → RSS/Atom feed
/sitemap.xml                    → Auto-generated sitemap
/llm.txt 											  → Configurable llm.txt for EACH page, including content that renders the article contents in native markdown for LLM/AI consumption.
/robots.txt                     → Configurable robots.txt
```

### 7.3 Performance Targets

| Metric | Target |
|---|---|
| Time to First Byte (TTFB) | < 50ms (cached), < 200ms (uncached) |
| Largest Contentful Paint | < 1.5s |
| Total page weight | < 200KB (excluding images) |
| Lighthouse Performance score | > 95 |
| Cache hit ratio | > 90% for published content |

### 7.4 SEO

Every published post automatically generates:
- OpenGraph meta tags
- Twitter Card meta tags
- JSON-LD structured data (Article schema)
- Canonical URL
- Sitemap entry
- RSS feed entry
- Clean semantic HTML5 (`<article>`, `<header>`, `<nav>`, `<aside>`, `<footer>`)

---

## PART 8 — SECURITY ARCHITECTURE

### 8.1 Security Defaults (via Guardian)

All of these are ON by default. Use Guardian middleware for all of them. Ensure that Alpine.js, plug-ins and other assests are permitted to load without fail. 

- **CSP headers:** Strict Content-Security-Policy. No inline scripts (except nonce-based). No eval
- **Rate limiting:** Per-IP rate limits on all API endpoints and form submissions. Configurable per-route
- **CSRF protection:** Anti-forgery tokens on all state-changing forms
- **Input validation:** All user input validated via Assertive before processing. No raw user input touches the database
- **SQL injection prevention:** Tuxedo ORM parameterizes all queries. No string concatenation in queries ever
- **XSS prevention:** All rendered output HTML-encoded by default. Markdown rendering uses allowlisted HTML tags only
- **Authentication:** AuthNZ handles all auth flows. JWT for API, cookie-based sessions for admin UI
- **Authorization:** Role-based access control on every admin route and API endpoint
- **Audit logging:** Every state-changing action logged to audit_log table with user, IP, action, entity
- **File upload security:** MIME type validation, file size limits, virus scanning hook, no executable uploads
- **Comment sanitization:** All comment markdown rendered through strict sanitizer. No raw HTML in comments
- **Plugin sandboxing:** Plugins run in isolated JS runtime. No access to host process, file system, or database

### 8.2 Authentication Flows (AuthNZ)

- **Admin login:** Email + password with optional TOTP 2FA - default admin@contento.local, password set via CONTENTO_ADMIN_PASSWORD env var - must be IP whitelisted in appsettings.
- **API access:** API key (per-site, rotatable, with scope restrictions)
- **Commenter auth:** Optional email verification for first-time commenters
- **OAuth:** Optional social login for commenters (Google, GitHub)

### 8.3 Role-Based Permissions

| Role | Posts | Comments | Layouts | Plugins | Settings | Users |
|---|---|---|---|---|---|---|
| Owner | CRUD + publish | CRUD + moderate | CRUD | Install/remove | All | Manage |
| Admin | CRUD + publish | CRUD + moderate | CRUD | Configure | Most | View |
| Editor | CRUD + publish | CRUD + moderate | View | — | — | — |
| Author | Own CRUD + submit for review | Own CRUD | View | — | — | — |
| Viewer | Read | Read | — | — | — | — |

---

## PART 9 — API DESIGN

### 9.1 REST API (for plugins, integrations, and headless use)

**Base URL:** `/api/v1`

**Authentication:** Bearer token (API key) or session cookie

```
# Posts
GET     /api/v1/posts                   → List posts (paginated, filterable)
GET     /api/v1/posts/{id}              → Get post by ID
GET     /api/v1/posts/by-slug/{slug}    → Get post by slug
POST    /api/v1/posts                   → Create post
PUT     /api/v1/posts/{id}              → Update post
DELETE  /api/v1/posts/{id}              → Delete post (soft delete → archive)
POST    /api/v1/posts/{id}/publish      → Publish post
POST    /api/v1/posts/{id}/unpublish    → Unpublish post
GET     /api/v1/posts/{id}/versions     → List versions
GET     /api/v1/posts/{id}/metrics      → Traffic metrics

# Comments
GET     /api/v1/posts/{id}/comments     → List comments (threaded)
POST    /api/v1/posts/{id}/comments     → Create comment
PUT     /api/v1/comments/{id}           → Update comment
DELETE  /api/v1/comments/{id}           → Delete comment
POST    /api/v1/comments/{id}/approve   → Approve comment
POST    /api/v1/comments/{id}/spam      → Mark as spam

# Categories
GET     /api/v1/categories              → List categories
POST    /api/v1/categories              → Create category
PUT     /api/v1/categories/{id}         → Update category
DELETE  /api/v1/categories/{id}         → Delete category

# Media
GET     /api/v1/media                   → List media
POST    /api/v1/media/upload            → Upload media file
DELETE  /api/v1/media/{id}              → Delete media

# Layouts
GET     /api/v1/layouts                 → List layouts
GET     /api/v1/layouts/{id}            → Get layout with components
POST    /api/v1/layouts                 → Create layout
PUT     /api/v1/layouts/{id}            → Update layout

# Site
GET     /api/v1/site                    → Get site config
PUT     /api/v1/site                    → Update site config
GET     /api/v1/site/stats              → Site-wide traffic stats

# Search
GET     /api/v1/search?q={query}        → Full-text search

# Plugins
GET     /api/v1/plugins                 → List installed plugins
POST    /api/v1/plugins/install         → Install plugin
PUT     /api/v1/plugins/{id}/settings   → Update plugin settings
POST    /api/v1/plugins/{id}/enable     → Enable plugin
POST    /api/v1/plugins/{id}/disable    → Disable plugin
```

### 9.2 Response Format

```json
{
  "data": { },
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 142,
    "totalPages": 8
  },
  "links": {
    "self": "/api/v1/posts?page=1",
    "next": "/api/v1/posts?page=2",
    "prev": null
  }
}
```

**Error format:**

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "One or more fields failed validation.",
    "details": [
      { "field": "title", "message": "Title is required." },
      { "field": "slug", "message": "Slug already exists." }
    ]
  }
}
```

---

## PART 10 — TESTING STRATEGY

### 10.1 Test Coverage Requirements

| Layer | Coverage Target | Framework |
|---|---|---|
| Domain/Core | 95%+ | NUnit |
| Services | 90%+ | NUnit + Moq |
| API Endpoints | 85%+ | NUnit + WebApplicationFactory |
| Repositories | 85%+ | NUnit + Testcontainers (PostgreSQL) |
| Plugin Sandbox | 90%+ | NUnit |
| Security (auth, validation) | 95%+ | NUnit |

### 10.2 Test Categories

```csharp
// Mark all tests with categories for selective execution
[Category("Unit")]          // Pure unit tests, no I/O
[Category("Integration")]   // Tests that touch database or Redis
[Category("Security")]      // Security-specific tests
[Category("Performance")]   // Performance benchmarks
```

### 10.3 Critical Test Scenarios

**Security tests (MUST PASS):**
- Unauthorized access to admin routes returns 401/403
- SQL injection attempts are rejected
- XSS payloads in post content are sanitized
- CSRF tokens are validated on all POST/PUT/DELETE
- Rate limiting kicks in at threshold
- Plugin cannot access resources outside its permissions
- File uploads with spoofed MIME types are rejected
- Comment spam detection works (honeypot field)
- API key scoping is enforced
- Password-protected posts require correct password

**Writer experience tests:**
- Markdown renders correctly for all supported syntax
- Autosave creates versions
- Post slug auto-generates from title
- Scheduled posts publish at correct time
- Draft → Review → Published workflow works
- Cover image upload and display

**Layout tests:**
- All core regions render in correct positions
- Layout JSON validates against schema
- Missing regions gracefully degrade
- Responsive breakpoints work
- Widget rendering in regions

**Performance tests:**
- Cached page response < 50ms
- Uncached page response < 200ms
- Post list query < 100ms for 10K posts
- Comment tree load < 50ms for 100 comments
- Media upload < 2s for 5MB file

---

## PART 11 — CLAUDE CODE TASK LIST

### Phase 0: Foundation & Setup

```
TASK 0.1 — Initialize Solution
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
READ: C:/work/noundry/noundryStarter/ (entire directory) - DO NOT WRITE HERE
READ: C:/work/noundry/llm/ (entire directory — all files) - DO NOT WRITE HERE

ACTION:
  - Create solution at C:/work/contento/Contento.sln
  - Create all projects per Section 2.1 structure
  - Reference NuGet packages: Noundry.UI, Tuxedo, Bowtie, AuthNZ,
    TailBreeze, Assertive, Guardian
  - Follow noundryStarter conventions for project setup, namespaces,
    folder structure, and configuration patterns
  - Setup .editorconfig, global usings, Directory.Build.props

VALIDATE:
  - Solution builds with zero warnings
  - All NuGet packages restore successfully
```

```
TASK 0.2 — Database Setup
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Create all tables from Section 3.1 as Tuxedo migrations
  - Create all indexes as specified
  - Setup partition tables for page_views and audit_log
  - Create seed data: default site, admin user, default layout, default theme
  - Setup Redis connection configuration

VALIDATE:
  - Migrations run cleanly on fresh PostgreSQL database
  - Seed data creates valid records
  - All foreign keys and constraints work
  - NUnit test: Verify all tables exist and have correct columns
```

```
TASK 0.3 — Auth & Security Foundation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Configure AuthNZ for admin authentication (email + password + optional TOTP)
  - Implement role-based authorization per Section 8.3 table
  - Configure Guardian middleware: CSP, rate limiting, CSRF, audit logging
  - Setup API key authentication for REST API
  - Implement audit logging service

VALIDATE:
  - NUnit tests: Unauthorized access returns 401
  - NUnit tests: Wrong role returns 403
  - NUnit tests: Rate limiting blocks after threshold
  - NUnit tests: Audit log records actions correctly
  - NUnit tests: CSP headers present on all responses
```

### Phase 1: Core Content Engine

```
TASK 1.1 — Post Domain Model & Repository
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Create Post entity with all fields from Section 3.1
  - Create PostVersion entity
  - Create Tuxedo repository for CRUD operations
  - Implement post versioning (auto-create version on save)
  - Implement full-text search using PostgreSQL tsvector
  - Implement slug auto-generation with collision handling
  - Implement Redis caching for published posts (Section 3.2)
  - Validate all inputs with Assertive

VALIDATE:
  - NUnit: CRUD operations work
  - NUnit: Version created on each save
  - NUnit: Full-text search returns relevant results
  - NUnit: Slug generation handles duplicates (appends -2, -3, etc.)
  - NUnit: Cache invalidation works on update
  - NUnit: Input validation rejects bad data
```

```
TASK 1.2 — Markdown Rendering Pipeline
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Configure Markdig with extensions: GFM, syntax highlighting, footnotes,
    task lists, tables, auto-links, custom callouts
  - Implement custom callout block extension ([!note], [!warning], [!tip])
  - Implement KaTeX math rendering
  - Implement embed detection (YouTube, Twitter URLs auto-embed)
  - Implement image lazy-loading injection
  - Implement heading ID generation for TOC
  - Implement reading time and word count calculation
  - XSS sanitization on ALL rendered output

VALIDATE:
  - NUnit: All markdown features render correctly
  - NUnit: XSS payloads in markdown are sanitized
  - NUnit: Embeds generate correct iframes
  - NUnit: Code blocks have syntax highlighting classes
  - NUnit: Reading time calculation is accurate
```

```
TASK 1.3 — Category & Tag System
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Create Category entity with hierarchical support (parent_id)
  - Create Tuxedo repository with tree-loading queries
  - Implement tag management (create-on-use, typeahead suggestions)
  - Implement category/tag archive pages

VALIDATE:
  - NUnit: Hierarchical categories work (3 levels deep)
  - NUnit: Tag filtering returns correct posts
  - NUnit: Orphan cleanup works when category deleted
```

```
TASK 1.4 — Media Library
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Create Media entity and repository
  - Implement file upload with:
    - MIME type validation (allowlist: images, PDF, video)
    - File size limits (configurable, default 10MB)
    - Image processing: auto-generate thumbnails, optimize for web
    - Storage abstraction (local filesystem with S3-compatible interface)
  - Implement media browser UI (grid view, search, drag-drop upload)
  - Implement image insertion into markdown editor

VALIDATE:
  - NUnit: Upload succeeds for valid files
  - NUnit: Upload rejects invalid MIME types
  - NUnit: Upload rejects oversized files
  - NUnit: Thumbnails generated correctly
  - NUnit: Spoofed MIME types detected and rejected
```

### Phase 2: Admin UI

```
TASK 2.1 — Admin Layout & Design System
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
READ: C:/work/phoenix/src/phoenix.web/ — Study layout, partials, CSS patterns

ACTION:
  - Create admin Razor layout using Noundry.UI components
  - Implement the Japanese minimalist aesthetic from Section 1.3
  - Use Tailwind v4 via TailBreeze for all styling
  - Create shared components:
    - Admin sidebar navigation (thin, icon-based, expandable)
    - Top bar (minimal: logo, user avatar, notifications bell)
    - Content area with consistent padding
    - Button styles (primary indigo, secondary outline, ghost)
    - Form input styles (hairline borders, warm gray)
    - Card component (subtle border, no shadow by default)
    - Modal/drawer component (slide-in from right)
    - Toast notifications (bottom-right, auto-dismiss)
    - Empty state component
    - Loading states (subtle pulse animation, not spinners)
  - Implement dark mode support (toggle in user preferences)
  - All transitions: ease-out, 200-300ms

VALIDATE:
  - Visual review: Matches Japanese minimalist spec
  - Responsive: Works on desktop (1280px+) and tablet (768px+)
  - Accessibility: All interactive elements have proper ARIA labels
  - Lighthouse accessibility score > 90
```

```
TASK 2.2 — Post Editor (THE CRITICAL TASK)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
This is the heart of Contento. Refer to Section 4.2 for full spec.

ACTION:
  - Build the full-page writing experience
  - Implement centered content column (720px max-width) on warm background
  - Title field: large, borderless, auto-focus
  - Subtitle field: appears on Enter after title
  - Body editor: Custom markdown editor with live WYSIWYG rendering
    - Use contenteditable div with markdown-to-HTML sync
    - Floating toolbar on text selection (bold, italic, H2, H3, quote, link, code)
    - Side toolbar (+) on empty lines (image, embed, code block, divider, table)
    - All keyboard shortcuts per spec
  - Cover image area above title (drag-drop)
  - Autosave with visual indicator (green pulse dot)
  - Word count + reading time (bottom-left, subtle)
  - Post settings drawer (right slide-in, all sections per Section 4.2)
  - Preview mode: full layout context rendering
  - Version history with diff view

VALIDATE:
  - Writer flow: Can write a full blog post without touching the mouse
  - Markdown: All GFM features render correctly in editor
  - Autosave: Versions created automatically
  - Image: Drag-drop, paste from clipboard both work
  - Performance: No lag on posts up to 10,000 words
  - Mobile: Editor works on tablet (simplified toolbar)
```

```
TASK 2.3 — Post List & Management
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Post list page with status tabs (All, Draft, Published, Scheduled, Archived)
  - Each row: title, author, status badge, date, word count, view count
  - Search/filter bar
  - Bulk actions (publish, archive, delete)
  - Quick-edit inline (title, status, category)
  - Sort by: date, title, views, comments

VALIDATE:
  - NUnit: Filtering returns correct results
  - NUnit: Bulk actions work correctly
  - Performance: List loads < 200ms for 1000 posts
```

```
TASK 2.4 — Dashboard
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Implement dashboard per Section 4.3
  - Greeting with writer name + date
  - "New Post" CTA button
  - Recent drafts list
  - Published count with sparkline
  - Traffic overview chart (last 30 days, use Chart.js or similar minimal library)
  - Recent pending comments

VALIDATE:
  - Dashboard loads < 300ms
  - All data sections populated correctly
  - Sparkline renders correctly
```

```
TASK 2.5 — Comment Management
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Comment moderation queue UI per Section 4.4
  - Tabbed view: Pending, Approved, Spam, Trash
  - Batch approve/spam/delete actions
  - Quick-reply from moderation view
  - Comment metrics per post
  - Visitor-facing comment form:
    - Threaded replies (up to 3 levels)
    - Markdown support in comments
    - Honeypot spam prevention
    - Rate limiting per IP
    - Optional email verification for first-time commenters
  - Real-time comment notifications via SignalR

VALIDATE:
  - NUnit: Comment threading works correctly (parent-child)
  - NUnit: Depth limited to 3 levels
  - NUnit: Honeypot catches bots
  - NUnit: Rate limiting blocks rapid submissions
  - NUnit: Comment markdown is sanitized (no XSS)
  - NUnit: Moderation actions update status correctly
```

### Phase 3: Layout System

```
TASK 3.1 — Layout Engine
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Implement Layout entity and LayoutComponent entity
  - Create layout rendering engine:
    - Parse JSONB layout structure
    - Render each region into correct grid positions
    - Support all content types: markdown, html, widget, navigation, dynamic, plugin
    - Implement responsive rules (hide on mobile, etc.)
  - Create default layouts: "Standard Blog", "Full Width", "Two Sidebar", "Magazine"
  - Implement layout-to-HTML renderer with Tailwind grid classes

VALIDATE:
  - NUnit: All default layouts render valid HTML
  - NUnit: Layout JSON schema validation works
  - NUnit: Missing regions degrade gracefully
  - NUnit: Responsive classes applied correctly
```

```
TASK 3.2 — Layout Designer UI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Build visual layout editor per Section 5.2
  - Drag-and-drop region placement on 12-column grid canvas
  - Region palette (left sidebar)
  - Region editor panel (right sidebar) with:
    - Content type selector
    - Markdown editor for static content
    - Widget picker for widget content
    - CSS class input
    - Visibility rules
  - Live preview button
  - Responsive view toggle (desktop/tablet/mobile)
  - Save layout as JSONB

VALIDATE:
  - Drag-drop works smoothly
  - Layout saves and loads correctly
  - Preview matches saved layout
  - Responsive toggle shows correct breakpoints
```

```
TASK 3.3 — Widget System
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Implement all built-in widgets from Section 5.3
  - Widget rendering pipeline (widget type → data fetch → template render → HTML)
  - Widget configuration UI (per-widget settings when placed in a region)
  - Widget caching (Redis, per-widget TTL)

VALIDATE:
  - NUnit: Each widget renders correct HTML
  - NUnit: Widget data fetching works (recent-posts, related-posts, etc.)
  - NUnit: Widget caching works and invalidates correctly
```

### Phase 4: Public Site

```
TASK 4.1 — Public Rendering Engine
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Implement the rendering pipeline per Section 7.1
  - Route matching for all public routes (Section 7.2)
  - Redis cache integration (check cache → render → store in cache)
  - Cache invalidation on content update
  - Implement response compression (gzip/brotli)
  - Generate proper HTML5 semantic markup
  - Implement all SEO features per Section 7.4

VALIDATE:
  - NUnit: All routes return correct content
  - NUnit: Cache hit returns < 50ms
  - NUnit: Uncached render < 200ms
  - NUnit: SEO meta tags present and correct
  - NUnit: RSS feed valid XML
  - NUnit: Sitemap valid XML
  - Lighthouse: Performance > 95, SEO > 95
```

```
TASK 4.2 — Theme System
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Create Theme entity and repository
  - Theme = CSS variables + base layout selection + custom overrides
  - Create 3 default themes:
    1. "Shizen" (Nature) — warm, organic, serif typography
    2. "Kōyō" (Autumn Leaves) — rich earth tones, elegant
    3. "Yuki" (Snow) — stark white, minimal, sans-serif
  - Theme settings UI in admin
  - CSS variable injection into public site rendering

VALIDATE:
  - NUnit: Theme CSS variables render correctly
  - NUnit: Theme switching works without page structure changes
  - Visual: All 3 themes look polished and distinct
```

```
TASK 4.3 — Traffic Analytics
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Implement privacy-respecting page view tracking
    - Track: page, referrer, device type, country (from IP), UTM params
    - Do NOT track: personal identifiers, cookies, fingerprints
    - Hash IP addresses for unique visitor counting, don't store raw
  - Write to page_views table (partitioned by month)
  - Aggregate to traffic_daily materialized view (hourly refresh)
  - Post metrics API endpoint
  - Post metrics UI page in admin:
    - Views over time chart
    - Unique visitors
    - Top referrers
    - Device breakdown
    - Country breakdown
  - Site-wide metrics on dashboard

VALIDATE:
  - NUnit: Page views recorded correctly
  - NUnit: Aggregation produces accurate daily totals
  - NUnit: IP hashing works (same IP = same hash, no reversal)
  - Performance: Tracking adds < 5ms to page load
```

### Phase 5: Plugin System

```
TASK 5.1 — Plugin Runtime & Sandbox
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Setup sandboxed JavaScript runtime (Jint or V8 via ClearScript)
  - Implement Plugin API surface (in-process HTTP-like interface)
  - Implement permission enforcement
  - Implement plugin-scoped storage (key-value in database)
  - Implement lifecycle hooks (onInstall, onEnable, onDisable, onBeforePublish, etc.)
  - Implement UI slot rendering (sidebar, post-header, post-footer, admin-panel)
  - Create plugin loader (reads manifest, validates, loads into sandbox)

VALIDATE:
  - NUnit: Plugin cannot access unauthorized resources
  - NUnit: Plugin API calls work within sandbox
  - NUnit: Lifecycle hooks fire at correct times
  - NUnit: Plugin UI renders in correct slots
  - NUnit: Plugin-scoped storage isolated between plugins
  - NUnit: Malicious plugin code doesn't crash the host
```

```
TASK 5.2 — Plugin SDK
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Create TypeScript SDK package (contento-plugin-sdk)
  - Type definitions for all plugin APIs
  - Plugin base class with lifecycle methods
  - API client for data access within sandbox
  - UI component helpers
  - Documentation with examples
  - Create 3 example plugins:
    1. SEO Analyzer — checks post for SEO best practices, shows score in sidebar
    2. Social Share Counts — displays share counts in post footer
    3. Reading Progress — adds reading progress bar to public posts

VALIDATE:
  - Example plugins install and work correctly
  - SDK types are complete and accurate
  - Documentation covers all APIs
```

```
TASK 5.3 — Plugin Admin UI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Plugin list page (installed, with enable/disable toggles)
  - Plugin install page (upload .zip or browse marketplace placeholder)
  - Plugin settings page (rendered from manifest settings schema)
  - Plugin permissions display (show what each plugin can access)
  - Plugin activity log

VALIDATE:
  - Install/uninstall flow works end-to-end
  - Settings save and load correctly
  - Enable/disable works without restart
```

### Phase 6: Polish & Production Readiness

```
TASK 6.1 — Search
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Implement full-text search using PostgreSQL tsvector + trigram
  - Search posts by title, body, tags, categories
  - Search suggestions (typeahead)
  - Search results page with highlighting
  - Admin search: posts, comments, media, users

VALIDATE:
  - NUnit: Search relevance is reasonable
  - NUnit: Partial matches work (trigram)
  - Performance: Search < 100ms for 10K posts
```

```
TASK 6.2 — RSS, Sitemap, Robots
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - RSS/Atom feed generation (auto-updated on publish)
  - Sitemap.xml generation (all published posts, categories, pages)
  - Configurable robots.txt
  - Ping search engines on new content (optional)

VALIDATE:
  - NUnit: RSS feed is valid XML
  - NUnit: Sitemap is valid XML
  - NUnit: robots.txt respects configuration
```

```
TASK 6.3 — Email Notifications
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - New comment notification to post author
  - Comment reply notification to parent commenter
  - Post published notification to subscribers (future: newsletter)
  - Configurable notification preferences per user
  - Use configurable SMTP (abstracted for future providers)

VALIDATE:
  - NUnit: Notification triggers on correct events
  - NUnit: Email templates render correctly
  - NUnit: Unsubscribe works
```

```
TASK 6.4 — Import/Export
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - WordPress WXR import (posts, pages, categories, tags, comments)
  - Markdown file import (folder of .md files)
  - Full site export (JSON format, includes all content and settings)
  - Post export (individual post as .md or .html)

VALIDATE:
  - NUnit: WordPress import handles real WXR files correctly
  - NUnit: Export/re-import produces identical content
  - NUnit: Large imports (1000+ posts) complete without timeout
```

```
TASK 6.5 — Performance Optimization
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Implement response caching strategy (Section 3.2)
  - Database query optimization (explain analyze on all major queries)
  - Connection pooling (PostgreSQL + Redis)
  - Static asset bundling and minification
  - Image optimization pipeline (WebP conversion, srcset generation)
  - Lazy loading for images and embeds
  - Preconnect/prefetch hints
  - HTTP/2 push for critical assets (if supported)

VALIDATE:
  - Performance targets from Section 7.3 all met
  - Database queries all use indexes (no seq scans on large tables)
  - Memory usage stable under load
```

```
TASK 6.6 — Comprehensive Test Suite
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - Write all tests specified in Section 10.3
  - Organize by category: Unit, Integration, Security, Performance
  - Use Testcontainers for PostgreSQL integration tests
  - Use WebApplicationFactory for API endpoint tests
  - Ensure all security tests pass
  - Ensure all performance benchmarks pass
  - Generate code coverage report

VALIDATE:
  - All tests pass (zero failures)
  - Coverage meets targets from Section 10.1
  - Security test suite is comprehensive
  - Performance benchmarks all green
```

### Phase 7: Documentation & CLI

```
TASK 7.1 — Documentation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - README.md with quick start guide
  - docs/plugin-api.md — Complete plugin API reference
  - docs/theme-guide.md — Theme creation guide
  - docs/deployment.md — Production deployment guide (Docker, bare metal)
  - docs/migration.md — WordPress migration guide
  - Inline XML documentation on all public APIs and services

VALIDATE:
  - All docs are accurate and complete
  - Quick start guide works from zero to running in < 10 minutes
```

```
TASK 7.2 — CLI Tool
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ACTION:
  - contento-cli: .NET global tool
  - Commands:
    - `contento new` — Scaffold new Contento site
    - `contento plugin new` — Scaffold new plugin
    - `contento plugin install <path>` — Install plugin
    - `contento import wordpress <wxr-file>` — Import from WordPress
    - `contento export` — Export site
    - `contento migrate` — Run database migrations
    - `contento seed` — Seed sample data

VALIDATE:
  - All CLI commands work correctly
  - Help text is clear and complete
```

---

## EXECUTION NOTES FOR CLAUDE CODE

### Reading Order (MUST DO FIRST)

1. **Read `C:/work/noundry/llm/` — ALL files.** This is the Noundry LLM design guide. It defines conventions, patterns, and best practices for all Noundry-ecosystem development. Every architectural and coding decision must align with this guide.

2. **Read `C:/work/noundry/noundryStarter/` — ALL files.** This is the starter template. Use it as the foundation for project setup. Copy its structure for NuGet references, configuration, dependency injection patterns, and project organization.

3. **Read `C:/work/phoenix/src/phoenix.web/` — Study thoroughly.** This is the aesthetic reference. The admin UI of Contento must match this sensibility: minimal, Japanese-inspired, clean, warm.

### Development Conventions

- Follow Noundry naming conventions and code style from the LLM guide
- Use Tuxedo for ALL database access — no raw ADO.NET, no EF Core
- Use Assertive for ALL input validation — no manual validation code
- Use Guardian for ALL security middleware — no custom security middleware
- Use AuthNZ for ALL authentication — no custom auth
- Use Noundry.UI for ALL UI components — no Bootstrap, no MUI, no custom component library
- Use TailBreeze for Tailwind v4 integration — follow its pipeline
- No Dapper. Ever.
- No EF Core. Ever. 
- Carefully and thoughtfully review spec, then create. 
- Create a JSON check-list and validate as you move forward, then mark item completed. 
- Use git to add features and commit often with proper comments. 
- Write NUnit tests for EVERY feature before marking it complete
- Use Redis for ALL caching — no in-memory cache

### Quality Bar

This is a WordPress killer. Every feature must be:
- **More pleasant to use** than WordPress
- **Faster** than WordPress
- **More secure** than WordPress
- **Better looking** than WordPress
- **Easier to extend** than WordPress

The writing experience must be **as good as Medium**. The admin must be **as clean as Linear**. The public site must be **as fast as a static site**.

No shortcuts. No "good enough." Ship excellence.

---

*End of PRD — Contento v1.0.0*
