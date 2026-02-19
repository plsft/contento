# WordPress Migration Guide

This guide walks you through migrating your existing WordPress site to Contento CMS. Contento supports importing content from WordPress via the standard WXR (WordPress eXtended RSS) export format, preserving your posts, pages, categories, tags, comments, and media.

## Table of Contents

- [Migration Overview](#migration-overview)
- [Before You Begin](#before-you-begin)
- [Step 1: Export from WordPress](#step-1-export-from-wordpress)
- [Step 2: Prepare Your Contento Instance](#step-2-prepare-your-contento-instance)
- [Step 3: Import via API](#step-3-import-via-api)
- [Step 4: Import via Admin Dashboard](#step-4-import-via-admin-dashboard)
- [What Gets Imported](#what-gets-imported)
- [What Does Not Get Imported](#what-does-not-get-imported)
- [Post-Import Tasks](#post-import-tasks)
- [URL Redirects](#url-redirects)
- [Theme Migration](#theme-migration)
- [Plugin Alternatives](#plugin-alternatives)
- [Large Site Considerations](#large-site-considerations)
- [Rollback Plan](#rollback-plan)
- [Troubleshooting](#troubleshooting)

---

## Migration Overview

The migration process follows these steps:

```
WordPress                          Contento
┌──────────────┐                  ┌──────────────┐
│  Export WXR   │───── WXR ──────▶│  Import API  │
│  (XML file)   │     file        │              │
└──────────────┘                  └──────┬───────┘
                                         │
                                         ▼
                                  ┌──────────────┐
                                  │  Posts        │
                                  │  Pages        │
                                  │  Categories   │
                                  │  Tags         │
                                  │  Comments     │
                                  │  Media        │
                                  └──────────────┘
```

**Estimated time:**
- Small site (< 100 posts): 5-15 minutes
- Medium site (100-1,000 posts): 15-45 minutes
- Large site (1,000+ posts): 45 minutes to several hours (see [Large Site Considerations](#large-site-considerations))

## Before You Begin

### Checklist

- [ ] Contento instance is running and accessible
- [ ] You have administrator access to both WordPress and Contento
- [ ] You have a backup of your WordPress database
- [ ] You have enough disk space for media files (check your WordPress `wp-content/uploads` size)
- [ ] You have noted your current WordPress permalink structure
- [ ] You have documented any critical WordPress plugins that need equivalents

### Important Notes

1. **This is a one-way import.** Contento does not export back to WXR format.
2. **Run the import on a staging instance first.** Verify everything looks correct before importing to production.
3. **Do not delete your WordPress site** until you have verified the Contento import and set up proper redirects.
4. **Media files are downloaded** from your WordPress site during import, so the WordPress site must be accessible.

## Step 1: Export from WordPress

### Using the WordPress Admin Dashboard

1. Log in to your WordPress admin panel.
2. Navigate to **Tools > Export**.
3. Select **All content** (recommended) or choose specific content types.
4. Click **Download Export File**.
5. Save the `.xml` file to your local machine.

### Using WP-CLI (for large sites)

For sites with thousands of posts, WP-CLI provides a more reliable export:

```bash
# Export everything
wp export --dir=/tmp/exports/

# Export only posts
wp export --post_type=post --dir=/tmp/exports/

# Export posts from a specific date range
wp export --post_type=post --start_date=2024-01-01 --end_date=2025-12-31 --dir=/tmp/exports/
```

### Verifying the Export

Before importing, verify the WXR file is valid:

```bash
# Check file size (should not be empty)
ls -lh wordpress-export.xml

# Verify it's valid XML
xmllint --noout wordpress-export.xml

# Count items to import
grep -c "<item>" wordpress-export.xml
```

## Step 2: Prepare Your Contento Instance

### Fresh Installation (Recommended)

For the cleanest migration, start with a fresh Contento instance:

```bash
# Using Docker
docker compose up -d

# Verify it's running
curl http://localhost:5000/api/v1/site
```

### Existing Installation

If importing into an existing Contento instance:

1. **Back up your database** before importing.
2. Be aware that duplicate slugs will be automatically renamed (e.g., `my-post` becomes `my-post-2`).
3. Categories with the same name will be merged, not duplicated.

## Step 3: Import via API

The WordPress import endpoint accepts a WXR file via multipart form upload.

### Basic Import

```bash
curl -X POST http://localhost:5000/api/v1/import-export/wordpress \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -F "file=@wordpress-export.xml"
```

### Import with Options

```bash
curl -X POST http://localhost:5000/api/v1/import-export/wordpress \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -F "file=@wordpress-export.xml" \
  -F "importMedia=true" \
  -F "importComments=true" \
  -F "defaultStatus=published" \
  -F "defaultAuthorEmail=admin@contento.local"
```

### Import Options

| Parameter           | Type      | Default       | Description                                        |
|---------------------|-----------|---------------|----------------------------------------------------|
| `file`              | `file`    | (required)    | The WXR export file.                               |
| `importMedia`       | `boolean` | `true`        | Download and import media attachments.             |
| `importComments`    | `boolean` | `true`        | Import approved comments.                          |
| `importDrafts`      | `boolean` | `true`        | Import draft posts.                                |
| `importPages`       | `boolean` | `true`        | Import WordPress pages.                            |
| `defaultStatus`     | `string`  | (from WXR)    | Override status for all posts: `published`, `draft`.|
| `defaultAuthorEmail`| `string`  | (from WXR)    | Assign all imported content to this author.        |

### API Response

The API returns a summary of the import:

```json
{
  "success": true,
  "summary": {
    "postsImported": 142,
    "pagesImported": 8,
    "categoriesImported": 12,
    "tagsImported": 45,
    "commentsImported": 327,
    "mediaImported": 89,
    "errors": [],
    "warnings": [
      "Post 'untitled-draft' has no content; imported as empty draft.",
      "Media file 'old-photo.jpg' could not be downloaded (404); skipped."
    ],
    "duration": "00:02:34"
  }
}
```

## Step 4: Import via Admin Dashboard

For a graphical import experience:

1. Log in to the Contento admin dashboard at `/admin`.
2. Navigate to **Settings > Import/Export**.
3. Click **Import from WordPress**.
4. Upload your WXR export file.
5. Configure import options (media, comments, drafts).
6. Click **Start Import**.
7. Monitor the progress bar and review the summary when complete.

## What Gets Imported

| WordPress Content    | Contento Equivalent     | Notes                                       |
|----------------------|-------------------------|---------------------------------------------|
| Posts                | Posts                   | Full content, title, excerpt, slug, dates.  |
| Pages                | Posts (type: page)      | Imported as page-type posts.                |
| Categories           | Categories              | Hierarchical structure preserved.           |
| Tags                 | Tags                    | Imported as post tags.                      |
| Comments             | Comments                | Only approved comments by default.          |
| Featured Images      | Featured Images         | Downloaded and re-hosted.                   |
| Inline Images        | Media                   | URLs rewritten to local paths.              |
| Post Status          | Post Status             | `publish` → `published`, `draft` → `draft`.|
| Post Dates           | Post Dates              | Created, published, and modified dates.     |
| Author               | Author                  | Mapped by email; falls back to default.     |
| Excerpts             | Excerpts                | Manual excerpts preserved.                  |
| Slug/Permalink       | Slug                    | WordPress slug directly mapped.             |

## What Does Not Get Imported

| WordPress Feature     | Reason                                           | Alternative                             |
|-----------------------|--------------------------------------------------|-----------------------------------------|
| Theme/Design          | WordPress themes are PHP-based; incompatible.    | Choose or create a Contento theme.      |
| Plugins               | WordPress plugins are PHP; incompatible.         | Use Contento's Jint plugin system.      |
| Widgets               | WordPress-specific UI concept.                   | Use Contento layouts and partials.      |
| Menus                 | Structure is theme-dependent.                    | Recreate in Contento admin.             |
| Custom Fields (ACF)   | Plugin-specific data format.                     | Use Contento post metadata.             |
| WooCommerce data      | E-commerce data not supported.                   | Use a dedicated e-commerce platform.    |
| User accounts         | Passwords cannot be migrated (different hashing).| Recreate users; they must set new passwords.|
| Shortcodes            | WordPress-specific syntax.                       | Convert to HTML before or after import. |
| Gutenberg blocks      | Rendered to HTML during import.                  | Some formatting may need adjustment.    |
| Revisions             | Only the current version is imported.            | Not applicable.                         |

## Post-Import Tasks

After importing, complete these steps to ensure your site is fully operational.

### 1. Review Imported Content

Browse through your posts in the admin dashboard:

- Check that formatting is preserved, especially for posts with complex layouts.
- Verify that images display correctly.
- Check that categories and tags are assigned correctly.
- Review any warnings from the import summary.

### 2. Fix Shortcodes

If your WordPress posts used shortcodes (e.g., `[gallery]`, `[embed]`), they will appear as literal text. Search for and replace them:

```bash
# Find posts containing shortcodes
curl http://localhost:5000/api/v1/search?q=%5B \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN"
```

Common shortcode replacements:

| WordPress Shortcode     | Contento Replacement                       |
|-------------------------|--------------------------------------------|
| `[gallery ids="1,2,3"]` | HTML `<figure>` elements with images      |
| `[embed]url[/embed]`   | Direct `<iframe>` or `<a>` tag            |
| `[caption]...[/caption]`| `<figure>` with `<figcaption>`            |
| `[code]...[/code]`     | `<pre><code>...</code></pre>`              |

### 3. Verify Media Files

Check that all media files were downloaded successfully:

```bash
# List imported media
curl http://localhost:5000/api/v1/media \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN"
```

If any media files failed to download (listed in import warnings), you can manually upload them through the admin dashboard.

### 4. Configure Site Settings

Update site settings to match your WordPress configuration:

- Site name and description
- Permalink structure (Contento uses `/{slug}` by default)
- RSS feed settings
- Comment moderation settings

### 5. Set Up Redirects

See the [URL Redirects](#url-redirects) section below.

### 6. Update DNS

When you are ready to go live:

1. Point your domain's DNS A/AAAA records to your Contento server.
2. Update `Contento__SiteUrl` to your domain.
3. Ensure TLS certificates are configured.

## URL Redirects

WordPress and Contento may use different URL structures. Set up redirects to preserve SEO value and prevent broken links.

### Common WordPress URL Patterns

| WordPress Format                     | Contento Equivalent     |
|--------------------------------------|-------------------------|
| `/2024/01/my-post/`                  | `/my-post`              |
| `/category/news/`                    | `/category/news`        |
| `/tag/javascript/`                   | `/tag/javascript`       |
| `/author/john/`                      | (no direct equivalent)  |
| `/wp-content/uploads/2024/01/img.jpg`| `/media/img.jpg`        |
| `/feed/`                             | `/feed.xml`             |
| `/sitemap.xml` or `/sitemap_index.xml`| `/sitemap.xml`         |

### Nginx Redirect Rules

Add these to your Nginx server block:

```nginx
# Redirect dated permalink format to slug-only
rewrite ^/\d{4}/\d{2}/(.+?)/?$ /$1 permanent;

# Redirect WordPress feed URL
rewrite ^/feed/?$ /feed.xml permanent;

# Redirect WordPress media paths
rewrite ^/wp-content/uploads/\d{4}/\d{2}/(.+)$ /media/$1 permanent;

# Redirect WordPress login/admin
rewrite ^/wp-admin/?$ /admin permanent;
rewrite ^/wp-login\.php$ /admin permanent;

# Block common WordPress attack paths
location ~ ^/(wp-includes|wp-content|xmlrpc\.php) {
    return 410;
}
```

### Caddy Redirect Rules

```
your-domain.com {
    # Dated permalinks to slug
    @dated path_regexp dated ^/\d{4}/\d{2}/(.+?)/?$
    redir @dated /{re.dated.1} 301

    # WordPress feed
    redir /feed /feed.xml 301
    redir /feed/ /feed.xml 301

    # WordPress media
    @wpmedia path_regexp wpmedia ^/wp-content/uploads/\d{4}/\d{2}/(.+)$
    redir @wpmedia /media/{re.wpmedia.1} 301

    # WordPress admin
    redir /wp-admin /admin 301
    redir /wp-admin/ /admin 301
    redir /wp-login.php /admin 301

    reverse_proxy localhost:5000
}
```

## Theme Migration

WordPress themes cannot be directly ported to Contento since they use completely different architectures (PHP vs. Razor Pages). However, you can recreate the visual design.

### Strategy

1. **Identify your WordPress theme's key design elements:** colors, typography, layout structure, and spacing.
2. **Map WordPress colors to Contento CSS variables.**
3. **Choose the closest built-in theme** as a starting point.
4. **Create a custom theme** if the built-in themes do not match.

### Color Mapping Example

If your WordPress theme uses these colors:

```css
/* WordPress theme */
--primary: #2563eb;      /* Blue */
--text: #1f2937;         /* Dark gray */
--text-secondary: #6b7280;
--border: #e5e7eb;
--bg: #ffffff;
--bg-secondary: #f9fafb;
```

Map them to Contento variables:

```css
/* Contento theme */
:root {
  --color-ink: #1f2937;       /* --text */
  --color-stone: #4b5563;     /* between text and secondary */
  --color-ash: #6b7280;       /* --text-secondary */
  --color-cloud: #d1d5db;     /* lighter border */
  --color-mist: #e5e7eb;      /* --border */
  --color-paper: #f9fafb;     /* --bg-secondary */
  --color-snow: #ffffff;      /* --bg */
  --color-indigo: #2563eb;    /* --primary */
}
```

See the [Theme Creation Guide](theme-guide.md) for complete instructions on building a custom theme.

## Plugin Alternatives

Common WordPress plugins and their Contento equivalents:

| WordPress Plugin        | Contento Alternative                                      |
|-------------------------|----------------------------------------------------------|
| Yoast SEO / Rank Math   | Built-in `seo-meta` plugin                              |
| Social media sharing     | Built-in `social-share` plugin                          |
| Reading progress bar     | Built-in `reading-progress` plugin                      |
| Contact Form 7           | Custom Jint plugin or external service (Formspree, etc.)|
| Akismet (spam filtering) | Built-in comment moderation                             |
| WP Super Cache / W3TC    | Built-in Redis caching                                  |
| Google Analytics         | Custom Jint plugin (inject tracking code via `page:head`)|
| XML Sitemaps             | Built-in at `/sitemap.xml`                              |
| RSS feed                 | Built-in at `/feed.xml`                                 |

### Example: Google Analytics Plugin

Create a simple plugin to inject your tracking code:

**`plugins/analytics/plugin.json`**

```json
{
  "name": "analytics",
  "version": "1.0.0",
  "description": "Injects Google Analytics tracking code.",
  "entry": "main.js",
  "settings": {
    "measurementId": {
      "type": "string",
      "label": "GA4 Measurement ID",
      "default": ""
    }
  }
}
```

**`plugins/analytics/main.js`**

```javascript
contento.on("page:head", function (context) {
  var id = contento.settings.measurementId;
  if (id) {
    context.tags += '<script async src="https://www.googletagmanager.com/gtag/js?id=' + id + '"></script>';
    context.tags += '<script>';
    context.tags += 'window.dataLayer = window.dataLayer || [];';
    context.tags += 'function gtag(){dataLayer.push(arguments);}';
    context.tags += 'gtag("js", new Date());';
    context.tags += 'gtag("config", "' + id + '");';
    context.tags += '</script>';
  }
  return context;
});
```

## Large Site Considerations

Sites with thousands of posts or large media libraries require additional preparation.

### Splitting the Export

For sites with 5,000+ posts, split the WordPress export into chunks:

```bash
# Export by year
wp export --post_type=post --start_date=2023-01-01 --end_date=2023-12-31 --filename_format=export-2023.xml
wp export --post_type=post --start_date=2024-01-01 --end_date=2024-12-31 --filename_format=export-2024.xml
wp export --post_type=post --start_date=2025-01-01 --end_date=2025-12-31 --filename_format=export-2025.xml
```

Import each file sequentially:

```bash
for file in export-*.xml; do
  echo "Importing $file..."
  curl -X POST http://localhost:5000/api/v1/import-export/wordpress \
    -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
    -F "file=@$file"
  echo "Done."
done
```

### Media Pre-Download

For sites with many large images, pre-download media to avoid timeouts during import:

```bash
# On the WordPress server, create a media archive
cd /var/www/wordpress/wp-content/uploads
tar czf /tmp/wp-media.tar.gz .

# Transfer to Contento server
scp /tmp/wp-media.tar.gz contento-server:/tmp/

# Extract to Contento media directory
tar xzf /tmp/wp-media.tar.gz -C /opt/contento/wwwroot/media/
```

Then import with media download disabled:

```bash
curl -X POST http://localhost:5000/api/v1/import-export/wordpress \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -F "file=@wordpress-export.xml" \
  -F "importMedia=false"
```

### Database Performance

For large imports, temporarily adjust PostgreSQL settings:

```sql
-- Before import
ALTER SYSTEM SET work_mem = '64MB';
ALTER SYSTEM SET maintenance_work_mem = '256MB';
SELECT pg_reload_conf();

-- After import, restore defaults
ALTER SYSTEM RESET work_mem;
ALTER SYSTEM RESET maintenance_work_mem;
SELECT pg_reload_conf();
```

### Timeout Configuration

For large imports, increase the API request timeout:

```bash
# Increase curl timeout to 30 minutes
curl --max-time 1800 -X POST http://localhost:5000/api/v1/import-export/wordpress \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -F "file=@wordpress-export.xml"
```

If using Nginx as a reverse proxy, increase the proxy timeout:

```nginx
location /api/v1/import-export/ {
    proxy_pass http://contento;
    proxy_read_timeout 1800s;
    proxy_send_timeout 1800s;
    client_max_body_size 500M;
}
```

## Rollback Plan

If the migration does not go as planned, here is how to revert.

### Before Migration

1. Keep your WordPress site running and accessible.
2. Do not update DNS until migration is verified.
3. Take a Contento database backup before the import (so you can restore to a clean state).

### Reverting the Import

To clear imported content and start over:

```bash
# Take a fresh backup first
docker compose exec postgres pg_dump -U contento contento > pre-rollback.sql

# Restore to pre-import state
docker compose stop contento
gunzip < pre-import-backup.sql.gz | docker compose exec -T postgres psql -U contento contento
docker compose start contento
```

### Pointing DNS Back to WordPress

If you have already changed DNS, simply update the A/AAAA records back to your WordPress server's IP address. DNS propagation typically takes 5 minutes to 48 hours.

## Troubleshooting

### Import fails immediately

**"Unsupported file format"** — Ensure the file is a valid WXR XML file. Open it in a text editor and verify it starts with `<?xml` and contains `<rss version="2.0"`.

**"File too large"** — The default upload limit is 10 MB. For large exports, either split the file or increase `Contento__MaxUploadSizeMb` and the reverse proxy's `client_max_body_size`.

### Media files not downloading

**"Could not download media: 404"** — The WordPress site must be accessible during import. If the site is offline, pre-download media files as described in [Large Site Considerations](#large-site-considerations).

**"Could not download media: SSL error"** — If the WordPress site uses a self-signed or expired certificate, the download will fail. Fix the certificate or pre-download the files.

### Duplicate content

If you accidentally run the import twice, you may end up with duplicate posts (with suffixed slugs like `my-post-2`). To clean up:

1. Restore from the pre-import database backup.
2. Run the import once.

### Garbled characters / encoding issues

WXR files should be UTF-8 encoded. If you see garbled characters:

1. Check the file encoding: `file -i wordpress-export.xml`
2. Convert if necessary: `iconv -f ISO-8859-1 -t UTF-8 wordpress-export.xml > wordpress-export-utf8.xml`
3. Re-import using the converted file.

### Missing formatting

WordPress posts using the Block Editor (Gutenberg) are converted to HTML during export. Some complex block layouts may not render identically. Review posts with:

- Multi-column layouts
- Custom block patterns
- Embedded third-party content (tweets, videos)
- Table blocks

These may need manual cleanup after import.

### Comments missing

By default, only approved comments are imported. To also import pending comments:

```bash
curl -X POST http://localhost:5000/api/v1/import-export/wordpress \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -F "file=@wordpress-export.xml" \
  -F "importComments=true"
```

Spam comments are always excluded.
