# Contento pSEO v2.0 — Product Requirements Document

**Product:** Contento CMS — pSEO Engine  
**Version:** 2.0  
**Status:** Draft  
**Author:** George Rios / Plurral Consulting  
**Date:** March 2026

---

## Executive Summary

Contento pSEO is a first-class programmatic SEO engine built natively into the Contento CMS platform. It enables any site owner to generate thousands of structured, AI-enriched, schema-validated pages at scale — and host them on a custom subdomain tied to their primary domain (e.g. `articles.worktale.org`), passing full link equity and domain authority back to their root domain.

This is not a blogging feature. This is not a WordPress competitor. **This is the product.** Contento's core value proposition becomes: *Give any website a programmatic SEO engine in minutes, without building custom infrastructure.*

The closing line of the Byword case study (13,000 pages, 5.7x traffic in 60 days) says it best:

> "Most CMS platforms actually make it very difficult to implement structured, schema-driven programmatic SEO like this without building custom infrastructure."

Contento eliminates that gap entirely.

---

## Problem Statement

Most website owners — SaaS founders, consultants, content creators, agencies — understand that SEO matters but lack the infrastructure to execute pSEO at scale. The traditional options are:

1. **DIY** — Build a custom generation pipeline, niche taxonomy, JSON schema system, renderer components, URL routing, and indexing pipeline. Months of work.
2. **Hire an agency** — Expensive, slow, and you don't own the system.
3. **Use a generic AI writer** — Produces freeform content that fails at scale (inconsistent structure, thin pages, no schema enforcement).

The Byword case study proves the correct architecture: AI fills strict JSON schemas, niche context is injected per page, content and presentation are fully separated, and titles are deterministic (not AI-generated). Contento exposes this as a managed SaaS feature.

---

## The Subdomain Architecture (Core Differentiator)

This is what separates Contento pSEO from every other tool.

### How it works

A user registers their domain with Contento. Contento provisions a dedicated site instance for that domain's pSEO content. The user then adds a CNAME record in their DNS provider pointing a subdomain at Contento's infrastructure.

```
User's DNS:
  articles.worktale.org  →  CNAME  →  pseo.contentocms.com

Contento routes:
  pseo.contentocms.com   →  Worktale pSEO site instance
```

All generated pages are served from `articles.worktale.org` (or `help.`, `resources.`, `tools.`, `guides.`, etc. — user's choice). The user then links from their main site to this subdomain.

### Why this passes link equity

Subdomains are treated by Google as part of the root domain for many purposes, but more importantly, **the links between the main site and the subdomain are cross-domain links that count as backlinks/internal signals** when the subdomain appears in the same Search Console property. The canonical domain authority flows bidirectionally:

- The main site links to `articles.worktale.org` → Google sees the main domain endorsing the subdomain
- The subdomain links back to `worktale.org` on every page → thousands of contextual backlinks to the root domain
- The subdomain's organic traffic and indexed pages appear under the root domain in GSC when verified together
- DR (Domain Rating in Ahrefs) accumulates on the root domain as the subdomain attracts backlinks

### Real-world example

```
worktale.org  (main product site)
  └── articles.worktale.org  (Contento pSEO instance)
        ├── /developer-journaling-for-dotnet-engineers
        ├── /git-history-storytelling-guide
        ├── /worktale-vs-manual-dev-logs
        ├── /developer-portfolio-ideas-2026
        └── 500+ more pages...

Each page contains:
  - "← Back to Worktale" link  (passes equity to root)
  - Canonical: articles.worktale.org/[slug]  (owns its own ranking)
  - Internal links to 3-5 related pages  (pSEO cluster linking)
  - Schema markup (Article, BreadcrumbList, FAQPage)
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                  CONTENTO PSEO ENGINE                │
├──────────────┬──────────────┬───────────────────────┤
│  Niche       │  Schema      │  Generation           │
│  Taxonomy    │  Library     │  Pipeline             │
│  (300+ niches│  (20+ types) │  (concurrent workers) │
│  with context│              │                       │
├──────────────┴──────────────┴───────────────────────┤
│              Validated JSON Content Store            │
├──────────────────────────────────────────────────────┤
│  Purpose-Built Page Renderers (per content type)     │
├──────────────────────────────────────────────────────┤
│  Multi-Site Engine (existing Contento feature)       │
├──────────────────────────────────────────────────────┤
│  Custom Domain / CNAME Router                        │
└──────────────────────────────────────────────────────┘
         ↕ serves                    ↕ links
  articles.worktale.org         worktale.org
```

### Key architectural principle (from Byword)

> AI generates the **data**. The front end handles the **presentation**. These two layers never mix.

- Content = validated JSON stored in PostgreSQL (Contento's existing DB)
- Design = purpose-built Razor/component renderers per content type
- Titles = deterministic templates (never AI-generated)
- SEO metadata = templated, not freeform

---

## Existing Contento Capabilities to Leverage

Based on the Contento docs and API, the following features are already in place and should be wired directly into pSEO:

| Existing Feature | pSEO Usage |
|---|---|
| Multi-site engine (`/api/v1/sites`) | Each pSEO project = a dedicated Site instance |
| Custom domain routing | CNAME subdomain → Site mapping |
| Custom Post Types with JSON field schemas | Each pSEO content type = a Custom Post Type |
| AI API (`/api/v1/ai/complete`) | Schema-driven content generation per page |
| Scheduler / cron (`/api/v1/scheduler`) | Batch generation jobs, progressive publishing |
| SEO service (JSON-LD, sitemap) | Auto-applied to every generated page |
| Redirects API | Slug change handling for generated pages |
| Management API (`/api/v1/posts`) | Bulk create/publish generated pages |
| Headless API | pSEO pages served via existing content delivery |
| MCP-compatible REST API | AI agents can orchestrate generation runs |

The existing Custom Post Type system with JSON field schemas is the most important foundation — each pSEO content type maps directly to a Custom Post Type whose `fieldSchema` IS the content schema.

---

## Feature Specifications

---

### Feature 1: pSEO Project Setup

**Overview:** A user creates a "pSEO Project" which provisions a dedicated Contento site instance and associates it with a custom subdomain.

**Admin UI Flow:**
1. User navigates to **pSEO → New Project**
2. Enters project name (e.g. "Worktale Articles")
3. Enters their root domain (e.g. `worktale.org`)
4. Chooses subdomain prefix: `articles`, `resources`, `help`, `tools`, `guides`, or custom
5. Contento provisions a new Site via the existing Sites API
6. User is shown CNAME instructions:
   ```
   Add this DNS record to your domain:
   Type:  CNAME
   Name:  articles
   Value: pseo.contentocms.com
   TTL:   3600
   ```
7. Contento polls for DNS propagation and confirms when active
8. SSL certificate provisioned automatically (Let's Encrypt / existing nginx/certbot stack)

**Backend:**
- New `PseoProject` entity: `id`, `siteId`, `rootDomain`, `subdomain`, `fqdn`, `status`, `createdAt`
- `POST /api/v1/pseo/projects` — create project, calls Sites API internally
- `GET /api/v1/pseo/projects/{id}/dns-status` — polls DNS + SSL status
- `POST /api/v1/pseo/projects/{id}/verify` — manual reverification trigger

**Checklist:**
- [ ] PseoProject entity and DB migration
- [ ] Sites API integration (auto-provision site on project create)
- [ ] CNAME instruction UI with copy-to-clipboard
- [ ] DNS propagation polling service (check every 5 min, timeout at 48h)
- [ ] SSL provisioning hook (certbot or ACME integration)
- [ ] Domain verification confirmation email
- [ ] Project dashboard showing status: `pending_dns` → `ssl_provisioning` → `active`

---

### Feature 2: Niche Taxonomy Library

**Overview:** A curated, extensible library of niche contexts. This is the most important part of the entire system. Rich niche context is what separates useful pSEO from name-swapped filler.

**Data Model:**
```json
{
  "slug": "developer-tools",
  "name": "Developer Tools",
  "context": {
    "audience": "Software engineers, DevOps, indie hackers, CTOs",
    "pain_points": "Onboarding friction, documentation gaps, tool fatigue, integration complexity",
    "monetization": "SaaS subscriptions, API credits, Pro tiers, enterprise licensing",
    "content_that_works": "Tutorials, comparisons, how-tos, changelogs, migration guides",
    "subtopics": ["CLI tools", "API development", "testing", "deployment", "logging", "monitoring"]
  }
}
```

**Day-One Library (minimum viable taxonomy):**

| Category | Niches (examples) |
|---|---|
| Software / SaaS | Developer Tools, Project Management, CRM, Analytics, Email Marketing, HR Tools |
| Content Creation | Blogging, YouTube, Podcasting, Newsletter, Course Creation |
| E-Commerce | Shopify Stores, Dropshipping, Print-on-Demand, Amazon FBA, DTC Brands |
| Professional Services | Consulting, Freelancing, Legal, Accounting, Real Estate |
| Health & Wellness | Fitness, Nutrition, Mental Health, Supplements, Telemedicine |
| Finance | Personal Finance, Investing, Crypto, Insurance, Fintech |
| Education | Online Learning, Tutoring, EdTech, Certification Prep |
| Local Business | Restaurants, Salons, Home Services, Retail, Auto |

Ship 150+ niches on day one. Target 300+ within 90 days (can use AI to generate niche context from a seed list).

**Admin UI:**
- Browse/search niche library
- "Use this niche" → adds to project
- "Customize" → fork a niche and edit audience, pain points, etc.
- "Create custom niche" → build from scratch

**Backend:**
- `NicheTaxonomy` table: `id`, `slug`, `name`, `context` (jsonb), `isSystem` (bool), `projectId` (nullable, for custom niches)
- `GET /api/v1/pseo/niches` — list all (system + project custom)
- `POST /api/v1/pseo/niches` — create custom niche
- `PUT /api/v1/pseo/niches/{id}` — update custom niche

**Checklist:**
- [ ] NicheTaxonomy entity and DB migration
- [ ] Seed script for 150+ system niches
- [ ] Niche browse/search UI with filtering by category
- [ ] Niche customization (fork + edit) UI
- [ ] Custom niche creation form
- [ ] Niche context preview (shows what gets injected into generation prompt)
- [ ] Import niches via JSON/CSV (for power users)

---

### Feature 3: Content Schema Library

**Overview:** Pre-built JSON schemas for each supported content type. Every generated page is validated against its schema before publishing.

**Built-in Content Types (Day One):**

| Type | Slug | Example Title Pattern | Traffic Potential |
|---|---|---|---|
| Idea List | `idea-list` | `100 [Content] Ideas for [Niche]` | Very High |
| Checklist | `checklist` | `[Topic] Checklist for [Niche]` | High |
| How-To Guide | `how-to` | `How to [Action] for [Niche]` | High |
| Comparison | `comparison` | `[Tool A] vs [Tool B] for [Niche]` | Medium |
| Alternatives | `alternatives` | `Best [Tool] Alternatives for [Niche]` | High |
| Resource List | `resource-list` | `Best [Resources] for [Niche]` | High |
| Tool Page | `tool-page` | `Free [Action] Tool for [Niche]` | Very High (engagement) |
| FAQ Page | `faq` | `[Topic] FAQs for [Niche]` | Medium |
| Glossary | `glossary` | `[Topic] Glossary for [Niche]` | Medium |
| Template Pack | `template-pack` | `[Topic] Templates for [Niche]` | High |

**Example Schema (Idea List):**
```typescript
interface IdeaListPage {
  meta: {
    content_type: "idea-list";
    niche: string;
    subtopic: string;
  };
  seo: {
    // NOTE: title is NOT generated by AI — set by template
    description: string;       // AI-generated, 150 chars max
    keywords: string[];        // exactly 5
  };
  content: {
    intro: string;             // 2-3 sentences, niche-aware
    sections: {
      heading: string;
      items: {                 // exactly 15-20 per section
        title: string;
        description: string;  // 1-2 sentences
        difficulty: 'beginner' | 'intermediate' | 'advanced';
        potential: 'high' | 'medium' | 'standard';
      }[];
    }[];                       // exactly 3-5 sections
    pro_tips: string[];        // exactly 5
    faq: {
      question: string;
      answer: string;          // 2-3 sentences
    }[];                       // exactly 3-5
  };
}
```

**Schema enforcement rules (critical for scale):**
- Exact item counts per section (e.g., 15-20 items, not "some items")
- Required difficulty/potential enumerations on every item
- Bounded string lengths on all fields
- Required section counts (not optional)
- FAQ always present (enables FAQPage schema markup)

**Admin UI:**
- Schema library browser
- "Preview schema" → shows the JSON structure
- "Customize schema" → fork and modify field constraints
- Schema validation test: paste sample JSON → see pass/fail

**Backend:**
- `ContentSchema` entity: `id`, `slug`, `name`, `schemaJson` (jsonb), `promptTemplate`, `rendererSlug`, `isSystem`
- Validation service: validates AI output against schema before persisting
- `GET /api/v1/pseo/schemas` — list schemas
- `POST /api/v1/pseo/schemas` — create custom schema

**Checklist:**
- [ ] ContentSchema entity and DB migration
- [ ] Seed all 10 day-one schemas
- [ ] JSON Schema validation service (use System.Text.Json schema validation or Newtonsoft)
- [ ] Schema browser UI
- [ ] Schema preview/diff UI
- [ ] Custom schema fork + edit UI
- [ ] Validation error display (field-level, human-readable)
- [ ] Prompt template per schema (system prompt + user prompt structure)

---

### Feature 4: Collection Builder

**Overview:** A Collection is the unit of pSEO work. It combines: one content type + one or more niches + URL template + generation settings. One collection generates N pages.

**Collection configuration:**

```
Collection: "Developer Tool Idea Lists"
├── Content Type: Idea List
├── Niches: [developer-tools, saas, devops]
├── Subtopics per niche: [CLI, API, testing, deployment] (auto-expanded from niche context)
├── URL Pattern: /{niche}-{subtopic}-ideas   → /developer-tools-cli-ideas
├── Title Template: "100 {subtopic} Ideas for {niche} in {year}"
├── Meta Description Template: "Discover 100 {subtopic} ideas for {niche}. Curated list with difficulty ratings and growth potential."
├── Target page count: ~48 pages (3 niches × ~16 subtopic combos)
├── Internal linking: Auto-link to 3 related pages in same collection
└── Back-link: "← Back to [root domain]" in header and footer
```

**Admin UI Flow:**
1. Name the collection
2. Choose content type (from schema library)
3. Select niches (multi-select, with niche context preview)
4. Configure subtopic expansion (auto from niche, or manual list)
5. Set URL pattern with variable tokens
6. Set title template (deterministic — never AI)
7. Preview estimated page count
8. Set publish schedule (all at once vs. N pages/day)
9. Set back-link text and target URL (defaults to root domain)
10. **Generate** → goes to generation pipeline

**Backend:**
- `PseoCollection` entity: `id`, `projectId`, `schemaId`, `name`, `urlPattern`, `titleTemplate`, `metaDescTemplate`, `nicheIds[]`, `publishSchedule`, `status`, `pageCount`, `generatedCount`, `publishedCount`
- `POST /api/v1/pseo/collections` — create collection
- `POST /api/v1/pseo/collections/{id}/generate` — trigger generation run
- `GET /api/v1/pseo/collections/{id}/progress` — SSE stream for generation progress

**Checklist:**
- [ ] PseoCollection entity and DB migration
- [ ] Collection builder UI (multi-step wizard)
- [ ] Subtopic expansion engine (cross-product of niche × subtopics)
- [ ] URL pattern builder with token preview
- [ ] Title template builder with token preview
- [ ] Estimated page count calculator
- [ ] Publish schedule options: immediate, batched (N/day), manual
- [ ] Back-link configuration UI
- [ ] Collection list/dashboard view showing status, counts, traffic

---

### Feature 5: Generation Pipeline

**Overview:** The engine that takes a Collection and produces validated JSON content for every page, using concurrent AI workers with schema enforcement.

**Pipeline stages:**

```
Collection
    ↓
[1] Expand → generate list of all (niche × subtopic) combos
    ↓
[2] Deduplicate → skip already-generated slugs
    ↓
[3] Queue → push jobs to generation queue
    ↓
[4] Generate (concurrent workers)
    │   ├── Build prompt: schema + niche context + title
    │   ├── Call AI API (/api/v1/ai/complete)
    │   ├── Request native JSON output
    │   └── Validate against schema
    ↓
[5] Validate
    │   ├── PASS → persist to ContentStore
    │   └── FAIL → retry once → if still fail → flag for review
    ↓
[6] Publish (per schedule)
    │   ├── Create Post via /api/v1/posts
    │   ├── Apply meta title/description from template
    │   ├── Inject schema markup (JSON-LD)
    │   └── Publish via /api/v1/posts/{id}/publish
    ↓
[7] Internal linking pass
    └── Update related pages with cross-links
```

**AI prompt architecture:**

The prompt follows the Byword principle: AI fills a schema, not a blank page.

```
System prompt:
"You are a structured content generator. You MUST respond with valid JSON only.
No markdown, no preamble, no explanation. The response must match this exact schema:
[schema definition]

Constraints:
- Each section must contain exactly 15-20 items
- Every item must include difficulty (beginner/intermediate/advanced) and potential (high/medium/standard)
- The intro must reference the specific niche audience
- Pro tips must be actionable and niche-specific
- Total response must be valid JSON parseable by JSON.parse()"

User prompt:
"Generate content for: [title]
Niche context: [full niche JSON including audience, pain_points, monetization, content_that_works, subtopics]
Subtopic focus: [specific subtopic]"
```

**Concurrency model:**
- Use `Parallel.ForEachAsync` with configurable degree of parallelism (default: 10 concurrent workers)
- Rate limiting: respect AI provider limits, implement exponential backoff
- Progress: emit events to SignalR hub → SSE to admin UI

**Title generation (deterministic — never AI):**
```csharp
// Titles come from templates, not AI
var title = titleTemplate
    .Replace("{niche}", niche.Name)
    .Replace("{subtopic}", subtopic)
    .Replace("{year}", DateTime.UtcNow.Year.ToString())
    .Replace("{count}", "100");
// Result: "100 CLI Ideas for Developer Tools in 2026"
```

**Checklist:**
- [ ] Generation queue (use existing Scheduler infrastructure or in-memory channel)
- [ ] Concurrent worker pool with configurable parallelism
- [ ] Prompt builder service (injects schema + niche context)
- [ ] AI API caller with retry logic and exponential backoff
- [ ] JSON validation service (schema enforcement, field constraints)
- [ ] Retry-once on validation failure, then flag for manual review
- [ ] `PseoPage` entity: `id`, `collectionId`, `slug`, `title`, `contentJson`, `status` (generated/validated/failed/published), `validationErrors`
- [ ] Content persistence (store validated JSON in PseoPage)
- [ ] Bulk post creation via Management API
- [ ] Deterministic title templating service
- [ ] Internal linking pass (post-generation, updates related pages)
- [ ] Progress tracking: SSE stream of generation job status
- [ ] Generation dashboard: total/validated/failed/published counts with live updates
- [ ] Failed page review queue: show validation errors, allow manual edit + retry

---

### Feature 6: Page Renderers

**Overview:** Each content type has a dedicated renderer that transforms its JSON into a fully-structured, SEO-optimized HTML page. Content and design are completely separated.

**Renderer responsibilities per page:**
- Render content JSON into structured HTML
- Apply interactive UI features per content type
- Inject JSON-LD schema markup (Article, FAQPage, BreadcrumbList)
- Render back-link to root domain (every page, prominent position)
- Render internal links to related pages in collection
- Render canonical URL tag
- Render Open Graph / Twitter Card meta tags
- Render breadcrumb navigation

**Content-type-specific UI features:**

| Content Type | UI Features |
|---|---|
| Idea List | Filter by difficulty, filter by potential, copy-to-clipboard per item |
| Checklist | Interactive checkboxes (localStorage state), progress indicator |
| Comparison | Structured feature table, winner callout, pros/cons columns |
| Tool Page | Functional embedded tool (niche-specific), copy output button |
| FAQ | Accordion expand/collapse, FAQ schema markup |
| Resource List | Category filter, external link tracking |
| How-To Guide | Step progress indicator, time estimate display |
| Alternatives | Comparison table, "Why switch?" section, CTA to root domain |

**Back-link component (critical for link equity):**

Every generated page renders this in both header and footer:
```html
<div class="pseo-backlink">
  <a href="https://worktale.org" rel="home">
    ← Back to Worktale
  </a>
</div>
```

Plus a contextual CTA in the body:
```html
<div class="pseo-cta">
  <p>Built with Worktale — the developer journaling tool that turns your git history into a work narrative.</p>
  <a href="https://worktale.org" class="cta-button">Try Worktale Free →</a>
</div>
```

**Checklist:**
- [ ] Base renderer: canonical, OG tags, breadcrumb, back-link, footer CTA
- [ ] Idea List renderer with difficulty/potential filters
- [ ] Checklist renderer with interactive checkboxes
- [ ] Comparison renderer with structured table
- [ ] Alternatives renderer with comparison table
- [ ] Resource List renderer with category filter
- [ ] How-To Guide renderer with step progress
- [ ] FAQ renderer with accordion + FAQPage JSON-LD
- [ ] Tool Page renderer (generalized, niche context injected)
- [ ] Glossary renderer with alphabetical navigation
- [ ] Template Pack renderer with download/copy actions
- [ ] JSON-LD injection service (Article + FAQPage + BreadcrumbList per page)
- [ ] Open Graph / Twitter Card meta tag injection
- [ ] Sitemap auto-update on page publish
- [ ] robots.txt for pSEO site (allow all, sitemap reference)

---

### Feature 7: SEO Infrastructure

**Overview:** Every generated page gets the full SEO treatment automatically. No configuration required.

**Per-page SEO (automatic):**

```html
<!-- Canonical -->
<link rel="canonical" href="https://articles.worktale.org/developer-tools-cli-ideas">

<!-- Meta -->
<title>100 CLI Ideas for Developer Tools in 2026</title>
<meta name="description" content="Discover 100 CLI ideas for developer tools...">

<!-- Open Graph -->
<meta property="og:title" content="100 CLI Ideas for Developer Tools in 2026">
<meta property="og:description" content="...">
<meta property="og:url" content="https://articles.worktale.org/...">
<meta property="og:type" content="article">

<!-- JSON-LD: Article -->
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "Article",
  "headline": "100 CLI Ideas for Developer Tools in 2026",
  "url": "https://articles.worktale.org/developer-tools-cli-ideas",
  "datePublished": "2026-03-11T00:00:00Z",
  "publisher": { "@type": "Organization", "name": "Worktale", "url": "https://worktale.org" }
}
</script>

<!-- JSON-LD: FAQPage (if FAQ content present) -->
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "FAQPage",
  "mainEntity": [{ "@type": "Question", "name": "...", "acceptedAnswer": {...} }]
}
</script>

<!-- JSON-LD: BreadcrumbList -->
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Worktale", "item": "https://worktale.org" },
    { "@type": "ListItem", "position": 2, "name": "Articles", "item": "https://articles.worktale.org" },
    { "@type": "ListItem", "position": 3, "name": "100 CLI Ideas...", "item": "https://articles.worktale.org/..." }
  ]
}
</script>
```

**Site-level SEO:**
- XML sitemap: auto-generated, updated on each publish batch, submitted to GSC
- `robots.txt`: `Allow: /`, `Sitemap: https://articles.worktale.org/sitemap.xml`
- `llms.txt`: machine-readable description (Contento already generates this)
- RSS feed: latest published pSEO pages

**Quality gates (pre-publish):**
- Minimum word count: 800 words (configurable)
- Required fields all present (schema validation)
- No duplicate slugs
- Meta description within 150 chars
- Title within 60 chars

**Checklist:**
- [ ] Per-page canonical URL injection (use pSEO subdomain, not contentocms.com)
- [ ] Article JSON-LD service
- [ ] FAQPage JSON-LD service (conditional on FAQ content presence)
- [ ] BreadcrumbList JSON-LD service (3-level: root → subdomain → page)
- [ ] Open Graph meta tag service
- [ ] XML sitemap generator for pSEO sites (extend existing SEO service)
- [ ] robots.txt for pSEO site
- [ ] Quality gate validation service (word count, required fields, length checks)
- [ ] Pages failing quality gates go to review queue, not auto-published
- [ ] GSC submission API integration (optional, manual trigger)

---

### Feature 8: Publishing & Progressive Rollout

**Overview:** Pages are published progressively, not all at once. Byword's lesson: batch rollouts allow indexing monitoring before scaling.

**Publish modes:**

| Mode | Behavior |
|---|---|
| Immediate | All validated pages published at once |
| Batched | N pages per day (e.g., 50/day) |
| Scheduled | Publish on specific date/time |
| Manual | Pages staged, user approves each batch |

**Progressive publish flow:**
1. Generation run completes → pages in `validated` status
2. Publish job runs per schedule (uses existing Scheduler/cron)
3. Each batch: create Post → set metadata → publish → update sitemap
4. Dashboard shows: Validated / Queued / Published / Indexed (GSC) counts
5. Alerts if validation failure rate exceeds threshold (e.g. >10%)

**Checklist:**
- [ ] Publish scheduler integration (use existing `/api/v1/scheduler`)
- [ ] Batch size configuration per collection
- [ ] Publish mode selector in collection settings
- [ ] Publish queue with ordered page list
- [ ] Real-time publish progress in dashboard
- [ ] Sitemap auto-rebuild after each batch
- [ ] Publish error handling (retry failed posts)
- [ ] "Pause publishing" control
- [ ] "Publish all now" override

---

### Feature 9: Analytics & Monitoring Dashboard

**Overview:** Track what's working. The feedback loop is where the real value compounds — traffic data feeds back into the niche taxonomy to improve future runs.

**Dashboard metrics:**

```
pSEO Project: Worktale Articles
───────────────────────────────────────────────────────
Collections:   4
Total pages:   487
Published:     312
Indexed*:       ~160 (51%)

Traffic (last 30 days)*:
  Clicks:      2,847
  Impressions: 84,230
  Avg CTR:     3.4%
  Avg Position: 18.2

Top Performing Pages:
  1. /developer-tools-cli-ideas         → 412 clicks
  2. /saas-onboarding-checklist         → 287 clicks
  3. /devops-monitoring-tools-list      → 201 clicks

Top Performing Collections:
  1. Developer Tool Idea Lists          → 1,204 clicks
  2. SaaS Resource Lists                → 891 clicks

* Requires Google Search Console integration
```

**GSC Integration:**
- OAuth connect to Google Search Console
- Pull click/impression/CTR/position data per URL
- Map GSC data to pSEO pages
- Show indexing status per page

**Feedback loop:**
- Identify top-performing niches → flag for expansion
- Identify zero-traffic pages after 60 days → flag for regeneration or removal
- Suggest new subtopic combos based on GSC query data

**Checklist:**
- [ ] GSC OAuth integration
- [ ] GSC data sync job (daily, uses Scheduler)
- [ ] Per-page GSC metric display
- [ ] Collection-level traffic rollup
- [ ] Top pages leaderboard
- [ ] Indexing status display (indexed / not indexed / excluded)
- [ ] "Regenerate this page" action (re-runs AI generation for single page)
- [ ] Zero-traffic page detection (configurable threshold, e.g. 0 clicks after 60 days)
- [ ] Niche performance ranking (which niches drive most traffic)
- [ ] Export performance data as CSV

---

### Feature 10: Admin & Onboarding UX

**Overview:** The entire pSEO engine is accessible via a dedicated section in the Contento admin panel. First-time users are guided through setup in under 10 minutes.

**Navigation structure:**
```
Admin Sidebar:
├── Posts
├── Media
├── ...existing...
└── pSEO ← new section
    ├── Overview (dashboard)
    ├── Projects
    ├── Collections
    ├── Niche Library
    ├── Schema Library
    └── Settings
```

**Onboarding flow (new pSEO user):**
1. **Welcome** — "Generate thousands of SEO pages for your site. No infrastructure required."
2. **Create Project** — enter root domain, choose subdomain
3. **DNS Setup** — show CNAME record, poll for propagation
4. **Choose Niches** — select 3-5 niches from library (or create custom)
5. **Choose Content Types** — pick 2-3 schemas (e.g. Idea List + Checklist)
6. **Preview** — show estimated page count and sample page titles
7. **Generate** — launch first batch, show live progress
8. **Done** — first pages published, share link to `articles.yourdomain.com`

**Checklist:**
- [ ] pSEO section in admin sidebar
- [ ] Project overview dashboard
- [ ] Onboarding wizard (multi-step, skippable after first project)
- [ ] DNS propagation status widget with retry button
- [ ] Live generation progress view (SSE-powered)
- [ ] "Sample page preview" before generating (renders one example page)
- [ ] Collection management table (list, pause, resume, delete)
- [ ] Page management table (filter by status, bulk publish, bulk delete)
- [ ] Settings: AI API key config, default publish schedule, quality gate thresholds

---

## Data Model Summary

```
PseoProject
  id, siteId, rootDomain, subdomain, fqdn, status, createdAt

NicheTaxonomy
  id, slug, name, context (jsonb), isSystem, projectId (nullable)

ContentSchema
  id, slug, name, schemaJson (jsonb), promptTemplate, rendererSlug, isSystem

PseoCollection
  id, projectId, schemaId, name, urlPattern, titleTemplate, metaDescTemplate,
  publishSchedule, batchSize, status, pageCount, generatedCount, publishedCount

PseoCollectionNiche (join)
  collectionId, nicheId

PseoPage
  id, collectionId, slug, title, metaDescription, contentJson (jsonb),
  bodyHtml (rendered), status (pending/generated/validated/failed/published),
  validationErrors (jsonb), publishedAt, postId (FK to Posts)

PseoAnalytics
  id, pageId, date, clicks, impressions, ctr, position, indexed
```

---

## API Surface (New Endpoints)

```
POST   /api/v1/pseo/projects                    Create project
GET    /api/v1/pseo/projects                    List projects
GET    /api/v1/pseo/projects/{id}               Get project
GET    /api/v1/pseo/projects/{id}/dns-status    DNS/SSL check
POST   /api/v1/pseo/projects/{id}/verify        Re-verify domain

GET    /api/v1/pseo/niches                      List niches
POST   /api/v1/pseo/niches                      Create custom niche
PUT    /api/v1/pseo/niches/{id}                 Update niche

GET    /api/v1/pseo/schemas                     List schemas
POST   /api/v1/pseo/schemas                     Create custom schema

POST   /api/v1/pseo/collections                 Create collection
GET    /api/v1/pseo/collections                 List collections
GET    /api/v1/pseo/collections/{id}            Get collection
POST   /api/v1/pseo/collections/{id}/generate   Trigger generation
GET    /api/v1/pseo/collections/{id}/progress   SSE progress stream
POST   /api/v1/pseo/collections/{id}/pause      Pause publishing
POST   /api/v1/pseo/collections/{id}/resume     Resume publishing

GET    /api/v1/pseo/pages                       List pages (filterable)
GET    /api/v1/pseo/pages/{id}                  Get page
POST   /api/v1/pseo/pages/{id}/regenerate       Re-generate single page
POST   /api/v1/pseo/pages/{id}/publish          Manual publish
DELETE /api/v1/pseo/pages/{id}                  Delete page

GET    /api/v1/pseo/analytics                   Project-level analytics
GET    /api/v1/pseo/analytics/pages             Per-page analytics
POST   /api/v1/pseo/analytics/sync              Trigger GSC sync
```

---

## Implementation Checklist (Full)

### Phase 0 — Foundation (Week 1-2)
- [ ] DB migrations: PseoProject, NicheTaxonomy, ContentSchema, PseoCollection, PseoCollectionNiche, PseoPage, PseoAnalytics
- [ ] Core service interfaces: IPseoProjectService, INicheService, ISchemaService, ICollectionService, IGenerationService, IPublishService
- [ ] Seed data: 150+ niches, 10 content schemas
- [ ] CNAME routing middleware (extend existing multi-site domain router)
- [ ] DNS propagation polling service
- [ ] SSL provisioning hook

### Phase 1 — Generation Engine (Week 2-4)
- [ ] Prompt builder service (schema + niche context injection)
- [ ] AI generation worker (calls /api/v1/ai/complete)
- [ ] JSON schema validation service
- [ ] Retry logic with exponential backoff
- [ ] Concurrent worker pool (configurable parallelism)
- [ ] Subtopic expansion engine (niche × subtopic cross-product)
- [ ] Deterministic title templating service
- [ ] Generation queue (Channel<T> or Scheduler integration)
- [ ] PseoPage persistence service
- [ ] SSE progress endpoint

### Phase 2 — Publishing (Week 3-4)
- [ ] Post creation service (wraps /api/v1/posts)
- [ ] Metadata injection (canonical, OG, JSON-LD)
- [ ] Quality gate validation (word count, field completeness, length limits)
- [ ] Batch publish scheduler (cron-based, uses existing Scheduler)
- [ ] Sitemap auto-rebuild on publish
- [ ] Internal linking pass (update related pages post-publish)
- [ ] Back-link and CTA injection per page

### Phase 3 — Renderers (Week 4-6)
- [ ] Base renderer (canonical, OG, breadcrumb, back-link)
- [ ] Idea List renderer (filters, copy-to-clipboard)
- [ ] Checklist renderer (interactive checkboxes)
- [ ] Comparison renderer (table, winner callout)
- [ ] Alternatives renderer
- [ ] Resource List renderer
- [ ] How-To Guide renderer
- [ ] FAQ renderer (accordion, FAQPage JSON-LD)
- [ ] Tool Page renderer
- [ ] Glossary renderer
- [ ] Template Pack renderer
- [ ] JSON-LD service (Article + FAQPage + BreadcrumbList)

### Phase 4 — Admin UI (Week 5-7)
- [ ] pSEO sidebar section
- [ ] Project creation wizard
- [ ] DNS setup UI with copy-to-clipboard CNAME
- [ ] DNS status polling widget
- [ ] Niche library browser + customization
- [ ] Schema library browser
- [ ] Collection builder wizard
- [ ] Generation progress dashboard (live SSE)
- [ ] Page management table (filter, bulk actions)
- [ ] Failed page review queue
- [ ] Publish schedule controls

### Phase 5 — Analytics (Week 7-8)
- [ ] GSC OAuth integration
- [ ] GSC data sync job
- [ ] Analytics dashboard (project + collection + page level)
- [ ] Top pages leaderboard
- [ ] Zero-traffic page detection
- [ ] Niche performance ranking
- [ ] CSV export

### Phase 6 — Polish & Launch (Week 8-9)
- [ ] Onboarding wizard (first-time flow)
- [ ] Sample page preview (pre-generation)
- [ ] End-to-end test: create project → generate → publish → verify live on subdomain
- [ ] Load test: 500 concurrent generation workers
- [ ] Documentation: pSEO setup guide, CNAME setup per registrar (Cloudflare, Namecheap, GoDaddy, Route53)
- [ ] Marketing site update: pSEO is the hero feature, not the CMS

---

## Positioning & Go-To-Market

**Before:** "Contento is an open-source CMS with a headless API, 8 themes, and a distraction-free editor."

**After:** "Contento gives any website a programmatic SEO engine. Generate thousands of structured, AI-powered pages hosted on your subdomain — no infrastructure required."

The target user is not a blogger. It is a SaaS founder, consultant, agency, or content entrepreneur who understands that organic traffic is leverage and wants to deploy the Byword strategy against their own domain without hiring engineers.

**The pitch (Worktale example):**
> "Worktale gets `articles.worktale.org`. 500 developer-focused SEO pages, live in 3 hours, hosted by Contento, linking back to your product. Every page passes equity to your main domain. Every indexed page is a permanent billboard for what you built."

---

## Open Questions

1. **AI provider choice:** The existing `/api/v1/ai/complete` endpoint is provider-agnostic. Which model do we default for pSEO generation — Claude Haiku (fast, cheap, good JSON) or allow per-project configuration?
2. **Billing model:** Per-project flat fee? Per-page generated? Tiered (500 pages / 5,000 pages / unlimited)?
3. **Tool pages:** The Byword data shows tool pages have the highest engagement. What's the minimum viable interactive tool we can generate programmatically? (e.g., a word counter, a readability scorer, a character counter — all renderable from a JSON config)
4. **Multi-subdomain per project:** Allow `articles.worktale.org` AND `tools.worktale.org` as separate collections under one project?
5. **White-label:** Should agency users be able to brand the admin panel under their own domain for client delivery?

---

*Contento pSEO v2.0 PRD — Plurral Consulting — March 2026*
