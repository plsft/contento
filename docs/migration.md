# Data Import Guide

This guide covers importing data into Contento pSEO — niche taxonomies, content schemas, bulk page creation, and WordPress site analysis for pSEO niche mapping.

## Table of Contents

- [Overview](#overview)
- [Importing Niche Taxonomies](#importing-niche-taxonomies)
- [Importing Content Schemas](#importing-content-schemas)
- [Bulk Page Creation via API](#bulk-page-creation-via-api)
- [WordPress Site Analysis](#wordpress-site-analysis)
- [Troubleshooting](#troubleshooting)

---

## Overview

Contento ships with `150` built-in niches and `10` content schemas, seeded on first boot. You can extend these with custom data using JSON/CSV imports or the REST API.

Import methods:

| Data type        | Import method                  | Format         |
|------------------|--------------------------------|----------------|
| Niche taxonomies | `POST /api/v1/niches/import`   | JSON or CSV    |
| Content schemas  | `POST /api/v1/schemas/import`  | JSON           |
| Bulk pages       | `POST /api/v1/pages/bulk`      | JSON           |
| WP analysis      | `POST /api/v1/analyze/wordpress` | URL (remote) |

## Importing Niche Taxonomies

Niche taxonomies define the verticals Contento can generate pages for (e.g., "plumber", "dentist", "personal injury lawyer").

### JSON format

```json
{
  "niches": [
    {
      "name": "Plumber",
      "slug": "plumber",
      "category": "Home Services",
      "keywords": ["plumbing", "drain cleaning", "pipe repair"],
      "locations": ["Chicago", "Houston", "Phoenix"]
    },
    {
      "name": "Dentist",
      "slug": "dentist",
      "category": "Healthcare",
      "keywords": ["dental care", "teeth cleaning", "orthodontics"],
      "locations": ["Miami", "Denver", "Seattle"]
    }
  ]
}
```

### CSV format

```csv
name,slug,category,keywords,locations
Plumber,plumber,Home Services,"plumbing;drain cleaning;pipe repair","Chicago;Houston;Phoenix"
Dentist,dentist,Healthcare,"dental care;teeth cleaning;orthodontics","Miami;Denver;Seattle"
```

Multi-value fields (`keywords`, `locations`) are semicolon-delimited within the CSV cell.

### Import via API

```bash
# JSON import
curl -X POST http://localhost:5000/api/v1/niches/import \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d @niches.json

# CSV import
curl -X POST http://localhost:5000/api/v1/niches/import \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: text/csv" \
  --data-binary @niches.csv
```

### Response

```json
{
  "imported": 2,
  "skipped": 0,
  "errors": [],
  "total_niches": 152
}
```

Duplicate slugs are skipped, not overwritten. To update an existing niche, use `PUT /api/v1/niches/{slug}`.

## Importing Content Schemas

Content schemas define the structure of generated pages — which fields exist, their types, validation rules, and rendering templates.

### JSON format

```json
{
  "schemas": [
    {
      "name": "Local Business Landing Page",
      "slug": "local-business-v3",
      "description": "Service + location landing page with FAQ, reviews, and CTAs.",
      "fields": [
        { "name": "business_name", "type": "string", "required": true },
        { "name": "service", "type": "string", "required": true },
        { "name": "location", "type": "string", "required": true },
        { "name": "description", "type": "text", "required": true },
        { "name": "faqs", "type": "json", "required": false },
        { "name": "reviews", "type": "json", "required": false },
        { "name": "cta_text", "type": "string", "required": false, "default": "Get a Free Quote" },
        { "name": "cta_url", "type": "url", "required": false }
      ],
      "seo": {
        "title_template": "{{service}} in {{location}} | {{business_name}}",
        "meta_description_template": "Professional {{service}} services in {{location}}. {{description|truncate:160}}"
      }
    }
  ]
}
```

### Import via API

```bash
curl -X POST http://localhost:5000/api/v1/schemas/import \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d @schemas.json
```

### Response

```json
{
  "imported": 1,
  "skipped": 0,
  "errors": [],
  "total_schemas": 11
}
```

## Bulk Page Creation via API

Generate pages in bulk by submitting a JSON payload with niche, schema, and variable data.

### Request format

```bash
curl -X POST http://localhost:5000/api/v1/pages/bulk \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d @pages.json
```

### Payload

```json
{
  "project_id": "plumbers-chicago",
  "schema": "local-business-v3",
  "pages": [
    {
      "slug": "drain-cleaning-chicago",
      "variables": {
        "business_name": "Acme Plumbing",
        "service": "Drain Cleaning",
        "location": "Chicago",
        "description": "Professional drain cleaning services in Chicago.",
        "cta_text": "Call Now",
        "cta_url": "tel:+13125551234"
      }
    },
    {
      "slug": "pipe-repair-chicago",
      "variables": {
        "business_name": "Acme Plumbing",
        "service": "Pipe Repair",
        "location": "Chicago",
        "description": "Expert pipe repair services in Chicago."
      }
    }
  ]
}
```

### Response

```json
{
  "created": 2,
  "failed": 0,
  "errors": [],
  "pages": [
    { "slug": "drain-cleaning-chicago", "url": "https://plumbers-chicago.yourdomain.com/drain-cleaning-chicago" },
    { "slug": "pipe-repair-chicago", "url": "https://plumbers-chicago.yourdomain.com/pipe-repair-chicago" }
  ]
}
```

### Using AI generation

To have Contento generate content from the schema and variables using AI instead of providing static content, set `"generate": true`:

```json
{
  "project_id": "plumbers-chicago",
  "schema": "local-business-v3",
  "generate": true,
  "pages": [
    {
      "slug": "drain-cleaning-chicago",
      "variables": {
        "business_name": "Acme Plumbing",
        "service": "Drain Cleaning",
        "location": "Chicago"
      }
    }
  ]
}
```

Generation progress is streamed via SSE on `GET /api/v1/pages/bulk/{job_id}/progress`.

## WordPress Site Analysis

Contento can analyze an existing WordPress site and suggest pSEO niche mappings based on its content structure, categories, and pages.

### Request

```bash
curl -X POST http://localhost:5000/api/v1/analyze/wordpress \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example-plumbing.com"}'
```

### Response

```json
{
  "site": {
    "title": "Example Plumbing Co.",
    "url": "https://example-plumbing.com",
    "pages_found": 47,
    "categories": ["Residential", "Commercial", "Emergency"],
    "service_pages": ["Drain Cleaning", "Pipe Repair", "Water Heater Installation"],
    "location_pages": ["Chicago", "Evanston", "Oak Park"]
  },
  "suggestions": [
    {
      "niche": "plumber",
      "schema": "local-business-v3",
      "estimated_pages": 9,
      "reasoning": "3 services x 3 locations = 9 landing pages. Current site has individual service pages but no location-specific landing pages."
    }
  ]
}
```

This analysis is non-destructive — it reads the WordPress site's public pages, sitemap, and RSS feed. It does not require WordPress admin access.

### Using the suggestions

After reviewing the suggestions, you can create a pSEO project from them:

```bash
curl -X POST http://localhost:5000/api/v1/projects \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Example Plumbing pSEO",
    "slug": "example-plumbing",
    "niche": "plumber",
    "schema": "local-business-v3",
    "domain": "seo.example-plumbing.com"
  }'
```

Then use [Bulk Page Creation](#bulk-page-creation-via-api) to generate the pages.

## Troubleshooting

### Import fails with "invalid format"

- Verify JSON is valid: `python3 -m json.tool < data.json`
- For CSV, ensure the header row matches the expected field names exactly
- Multi-value fields in CSV must use semicolons, not commas

### Duplicate slug errors

Imports skip existing slugs by default. To force-update, use the individual `PUT` endpoint instead of bulk import:

```bash
curl -X PUT http://localhost:5000/api/v1/niches/plumber \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name": "Plumber", "keywords": ["plumbing", "updated-keyword"]}'
```

### Large imports timing out

For imports with thousands of entries, increase the request timeout and use the async endpoint:

```bash
# Async bulk import (returns a job ID immediately)
curl -X POST http://localhost:5000/api/v1/pages/bulk?async=true \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d @large-import.json

# Check progress
curl http://localhost:5000/api/v1/jobs/{job_id} \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN"
```

### WordPress analysis returns empty suggestions

- Verify the WordPress site is publicly accessible
- Check that the site has a sitemap at `/sitemap.xml` or `/sitemap_index.xml`
- Ensure the site is not behind authentication or a maintenance page
