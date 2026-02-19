# Contento Plugin API Reference

This document is the complete reference for building plugins for Contento CMS. Plugins extend Contento's functionality through a sandboxed JavaScript runtime powered by [Jint](https://github.com/sebastienros/jint), providing a safe and predictable execution environment.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Plugin Structure](#plugin-structure)
- [Plugin Manifest](#plugin-manifest)
- [Lifecycle Hooks](#lifecycle-hooks)
- [Hook Reference](#hook-reference)
- [Plugin API Objects](#plugin-api-objects)
- [Sandbox Constraints](#sandbox-constraints)
- [Built-in Plugins](#built-in-plugins)
- [Development Workflow](#development-workflow)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Architecture Overview

Contento plugins run inside a **Jint JavaScript sandbox** — an embedded JavaScript interpreter for .NET. This architecture provides:

- **Security** — Plugins cannot access the filesystem, network, or host process directly.
- **Predictability** — Resource limits prevent runaway scripts from degrading performance.
- **Simplicity** — Write standard JavaScript; no build tools or bundling required.

Each plugin is loaded at application startup and registers handlers for specific lifecycle hooks. When those hooks fire during normal CMS operations, Contento invokes the registered handlers with relevant context data.

```
┌─────────────────────────────────┐
│         Contento CMS            │
│                                 │
│  ┌───────────┐  ┌────────────┐  │
│  │  Razor    │  │  API       │  │
│  │  Pages    │  │  Endpoints │  │
│  └─────┬─────┘  └─────┬──────┘  │
│        │               │         │
│        ▼               ▼         │
│  ┌─────────────────────────┐    │
│  │     Hook Dispatcher     │    │
│  └────────────┬────────────┘    │
│               │                  │
│    ┌──────────┼──────────┐      │
│    ▼          ▼          ▼      │
│  ┌─────┐  ┌─────┐  ┌─────┐    │
│  │Jint │  │Jint │  │Jint │    │
│  │ SEO │  │Share│  │Prog.│    │
│  └─────┘  └─────┘  └─────┘    │
│  64MB     64MB     64MB        │
│  5s max   5s max   5s max      │
└─────────────────────────────────┘
```

## Plugin Structure

A plugin is a directory inside the `/plugins/` folder containing at minimum a manifest file and a main JavaScript file.

```
plugins/
└── my-plugin/
    ├── plugin.json        # Plugin manifest (required)
    ├── main.js            # Entry point (required)
    ├── README.md          # Plugin documentation (optional)
    └── assets/            # Static assets (optional)
        ├── style.css
        └── script.js
```

### Minimal Plugin Example

**`plugins/hello-world/plugin.json`**

```json
{
  "name": "hello-world",
  "version": "1.0.0",
  "description": "Adds a greeting banner to every post.",
  "author": "Your Name",
  "entry": "main.js"
}
```

**`plugins/hello-world/main.js`**

```javascript
contento.on("post:render", function (context) {
  context.content = "<div class='greeting'>Welcome, reader.</div>" + context.content;
  return context;
});
```

This plugin prepends a greeting banner to every rendered post.

## Plugin Manifest

The `plugin.json` file declares metadata and configuration for your plugin.

### Required Fields

| Field         | Type     | Description                                      |
|---------------|----------|--------------------------------------------------|
| `name`        | `string` | Unique plugin identifier. Use lowercase with hyphens (e.g., `my-plugin`). |
| `version`     | `string` | Semantic version (e.g., `1.0.0`).                |
| `description` | `string` | Brief description of what the plugin does.       |
| `entry`       | `string` | Relative path to the main JavaScript file.       |

### Optional Fields

| Field          | Type       | Description                                     |
|----------------|------------|-------------------------------------------------|
| `author`       | `string`   | Plugin author name or organization.             |
| `homepage`     | `string`   | URL to the plugin's homepage or repository.     |
| `license`      | `string`   | SPDX license identifier (e.g., `MIT`).          |
| `hooks`        | `string[]` | Declare which hooks this plugin uses. Informational; the runtime detects registrations automatically. |
| `settings`     | `object`   | Default settings schema. Admins can override values in the dashboard. |
| `dependencies` | `object`   | Other plugins this plugin depends on, with version ranges. |

### Full Manifest Example

```json
{
  "name": "seo-meta",
  "version": "2.1.0",
  "description": "Injects SEO meta tags into page headers based on post content.",
  "author": "Contento Team",
  "homepage": "https://github.com/contento/plugin-seo-meta",
  "license": "MIT",
  "entry": "main.js",
  "hooks": [
    "post:render",
    "page:head"
  ],
  "settings": {
    "defaultTitle": {
      "type": "string",
      "label": "Default page title",
      "default": "Contento"
    },
    "titleSeparator": {
      "type": "string",
      "label": "Title separator character",
      "default": " — "
    },
    "includeOpenGraph": {
      "type": "boolean",
      "label": "Include Open Graph tags",
      "default": true
    },
    "includeTwitterCard": {
      "type": "boolean",
      "label": "Include Twitter Card tags",
      "default": true
    }
  }
}
```

## Lifecycle Hooks

Hooks are the mechanism by which plugins interact with Contento. A plugin registers handler functions for specific hooks, and Contento calls those handlers at the appropriate points during request processing.

### Registering a Hook Handler

Use the global `contento.on()` function:

```javascript
contento.on("hookName", function (context) {
  // Your logic here.
  // Modify and return context to affect output.
  return context;
});
```

### Handler Function Signature

Every handler receives a **context object** specific to the hook type and must return the (possibly modified) context. If a handler does not return the context, the original context is passed to the next handler unchanged.

### Hook Execution Order

When multiple plugins register handlers for the same hook, they execute in the order plugins are loaded (alphabetical by plugin name). Each handler receives the context returned by the previous handler, forming a pipeline.

```
post:render fires
  → seo-meta handler (modifies context)
    → social-share handler (modifies context)
      → reading-progress handler (modifies context)
        → final rendered output
```

## Hook Reference

### Content Hooks

#### `post:render`

Fired when a post is rendered for display. Use this to transform or augment post HTML content.

**Context Object:**

| Property       | Type     | Description                              |
|----------------|----------|------------------------------------------|
| `content`      | `string` | The rendered HTML content of the post.   |
| `post`         | `object` | The full post object (see Post Object).  |
| `isPreview`    | `bool`   | Whether this is an admin preview render. |

**Example:**

```javascript
contento.on("post:render", function (context) {
  // Add a CSS class to all images for lazy loading
  context.content = context.content.replace(
    /<img /g,
    '<img loading="lazy" class="content-image" '
  );
  return context;
});
```

#### `post:create`

Fired after a new post is created and saved to the database.

**Context Object:**

| Property  | Type     | Description                          |
|-----------|----------|--------------------------------------|
| `post`    | `object` | The newly created post object.       |
| `author`  | `object` | The user who created the post.       |

**Example:**

```javascript
contento.on("post:create", function (context) {
  contento.log("New post created: " + context.post.title);
  return context;
});
```

#### `post:update`

Fired after an existing post is updated.

**Context Object:**

| Property    | Type     | Description                           |
|-------------|----------|---------------------------------------|
| `post`      | `object` | The updated post object.              |
| `previous`  | `object` | The post object before the update.    |
| `author`    | `object` | The user who performed the update.    |

**Example:**

```javascript
contento.on("post:update", function (context) {
  if (context.previous.status === "draft" && context.post.status === "published") {
    contento.log("Post published: " + context.post.title);
  }
  return context;
});
```

#### `post:delete`

Fired after a post is deleted.

**Context Object:**

| Property  | Type     | Description                     |
|-----------|----------|---------------------------------|
| `post`    | `object` | The deleted post object.        |
| `author`  | `object` | The user who deleted the post.  |

#### `comment:create`

Fired after a new comment is created.

**Context Object:**

| Property   | Type     | Description                             |
|------------|----------|-----------------------------------------|
| `comment`  | `object` | The newly created comment object.       |
| `post`     | `object` | The post the comment belongs to.        |

**Example:**

```javascript
contento.on("comment:create", function (context) {
  // Sanitize comment content
  context.comment.content = context.comment.content
    .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, "");
  return context;
});
```

#### `comment:update`

Fired after a comment is updated.

**Context Object:**

| Property   | Type     | Description                        |
|------------|----------|------------------------------------|
| `comment`  | `object` | The updated comment object.        |
| `previous` | `object` | The comment before the update.     |
| `post`     | `object` | The post the comment belongs to.   |

#### `comment:delete`

Fired after a comment is deleted.

**Context Object:**

| Property   | Type     | Description                        |
|------------|----------|------------------------------------|
| `comment`  | `object` | The deleted comment object.        |
| `post`     | `object` | The post the comment belonged to.  |

### Page Hooks

#### `page:head`

Fired when the `<head>` section of a page is being rendered. Use this to inject meta tags, stylesheets, or scripts.

**Context Object:**

| Property   | Type     | Description                                    |
|------------|----------|------------------------------------------------|
| `tags`     | `string` | HTML string to inject into the `<head>`.       |
| `page`     | `object` | The current page/post being rendered.          |
| `url`      | `string` | The full URL of the current page.              |

**Example:**

```javascript
contento.on("page:head", function (context) {
  context.tags += '<meta property="og:title" content="' + context.page.title + '">';
  context.tags += '<meta property="og:url" content="' + context.url + '">';
  return context;
});
```

#### `page:footer`

Fired when the page footer is being rendered. Use this to inject scripts or HTML before the closing `</body>` tag.

**Context Object:**

| Property   | Type     | Description                                      |
|------------|----------|--------------------------------------------------|
| `html`     | `string` | HTML string to inject before `</body>`.          |
| `page`     | `object` | The current page/post being rendered.            |

**Example:**

```javascript
contento.on("page:footer", function (context) {
  context.html += '<script src="/plugins/my-plugin/assets/script.js"></script>';
  return context;
});
```

### Feed Hooks

#### `feed:item`

Fired for each item when generating the RSS feed.

**Context Object:**

| Property   | Type     | Description                             |
|------------|----------|-----------------------------------------|
| `item`     | `object` | The feed item object (title, link, etc).|
| `post`     | `object` | The source post object.                 |

#### `sitemap:entry`

Fired for each entry when generating the XML sitemap.

**Context Object:**

| Property    | Type     | Description                                |
|-------------|----------|--------------------------------------------|
| `entry`     | `object` | The sitemap entry (url, lastmod, priority). |

### Media Hooks

#### `media:upload`

Fired after a media file is uploaded.

**Context Object:**

| Property  | Type     | Description                          |
|-----------|----------|--------------------------------------|
| `media`   | `object` | The uploaded media object.           |
| `author`  | `object` | The user who uploaded the file.      |

#### `media:delete`

Fired after a media file is deleted.

**Context Object:**

| Property  | Type     | Description                          |
|-----------|----------|--------------------------------------|
| `media`   | `object` | The deleted media object.            |

## Plugin API Objects

### The `contento` Global Object

Every plugin has access to the `contento` global object, which provides the registration API and utility functions.

#### `contento.on(hook, handler)`

Register a handler function for a lifecycle hook.

- **hook** (`string`) — The hook name (e.g., `"post:render"`).
- **handler** (`function`) — The handler function receiving a context object.

#### `contento.log(message)`

Write a message to the Contento application log.

- **message** (`string`) — The log message.

```javascript
contento.log("SEO meta tags injected for: " + context.post.title);
```

#### `contento.settings`

Access the plugin's settings as configured by the administrator in the dashboard.

```javascript
var separator = contento.settings.titleSeparator || " — ";
var fullTitle = context.post.title + separator + contento.settings.defaultTitle;
```

#### `contento.site`

Read-only access to global site configuration.

| Property    | Type     | Description                    |
|-------------|----------|--------------------------------|
| `name`      | `string` | The site name.                 |
| `url`       | `string` | The site's public URL.         |
| `theme`     | `string` | The active theme name.         |
| `language`  | `string` | The site's language code.      |

```javascript
var canonicalUrl = contento.site.url + "/" + context.post.slug;
```

#### `contento.html.escape(str)`

Escape a string for safe inclusion in HTML.

```javascript
var safeTitle = contento.html.escape(context.post.title);
```

#### `contento.html.stripTags(str)`

Remove all HTML tags from a string.

```javascript
var plainText = contento.html.stripTags(context.post.content);
var description = plainText.substring(0, 160);
```

### Post Object

The post object is available in many hook contexts.

| Property       | Type       | Description                                  |
|----------------|------------|----------------------------------------------|
| `id`           | `int`      | Unique post identifier.                      |
| `title`        | `string`   | Post title.                                  |
| `slug`         | `string`   | URL-friendly slug.                           |
| `content`      | `string`   | Post body (HTML).                            |
| `excerpt`      | `string`   | Short excerpt or summary.                    |
| `status`       | `string`   | `"draft"`, `"published"`, or `"archived"`.   |
| `author`       | `object`   | Author user object.                          |
| `categories`   | `array`    | Array of category objects.                   |
| `tags`         | `array`    | Array of tag strings.                        |
| `featuredImage` | `string`  | URL to the featured image, or `null`.        |
| `publishedAt`  | `string`   | ISO 8601 publication timestamp.              |
| `createdAt`    | `string`   | ISO 8601 creation timestamp.                 |
| `updatedAt`    | `string`   | ISO 8601 last update timestamp.              |
| `metadata`     | `object`   | Arbitrary key-value metadata.                |

### Comment Object

| Property     | Type     | Description                             |
|--------------|----------|-----------------------------------------|
| `id`         | `int`    | Unique comment identifier.              |
| `postId`     | `int`    | ID of the parent post.                  |
| `authorName` | `string` | Display name of the comment author.     |
| `authorEmail`| `string` | Email of the comment author.            |
| `content`    | `string` | Comment body text.                      |
| `status`     | `string` | `"pending"`, `"approved"`, or `"spam"`. |
| `createdAt`  | `string` | ISO 8601 creation timestamp.            |

### Media Object

| Property      | Type     | Description                          |
|---------------|----------|--------------------------------------|
| `id`          | `int`    | Unique media identifier.             |
| `filename`    | `string` | Original filename.                   |
| `url`         | `string` | Public URL to the media file.        |
| `contentType` | `string` | MIME type (e.g., `image/jpeg`).      |
| `size`        | `int`    | File size in bytes.                  |
| `uploadedAt`  | `string` | ISO 8601 upload timestamp.           |

## Sandbox Constraints

Plugins execute within a Jint sandbox with strict resource limits to protect the host application.

| Constraint          | Default     | Description                                        |
|---------------------|-------------|----------------------------------------------------|
| **Memory limit**    | 64 MB       | Maximum heap memory per plugin instance.           |
| **Execution timeout** | 5 seconds | Maximum wall-clock time for a single hook invocation. |
| **Statement limit** | 100,000     | Maximum number of JavaScript statements per invocation. |

### What Plugins Cannot Do

- **No file system access** — Cannot read or write files on the server.
- **No network access** — Cannot make HTTP requests or open sockets.
- **No process spawning** — Cannot execute system commands.
- **No `require`/`import`** — Cannot load external modules (everything must be in `main.js` or inlined).
- **No `eval`** — Dynamic code evaluation is disabled.
- **No `setTimeout`/`setInterval`** — Asynchronous timers are not available.

### When Limits Are Exceeded

If a plugin exceeds any sandbox constraint, Contento will:

1. Terminate the plugin's current execution.
2. Log an error with the plugin name and violated constraint.
3. Return the unmodified context to the hook pipeline (the plugin's changes are discarded).
4. Continue processing the remaining plugins in the pipeline.

The plugin remains loaded and will be invoked again on the next hook event. Persistent violations should be addressed by the plugin author.

## Built-in Plugins

Contento ships with three example plugins in the `/plugins/` directory. These serve as both useful features and reference implementations.

### seo-meta

Injects SEO-optimized meta tags into page headers. Generates Open Graph, Twitter Card, and standard meta description tags from post content.

**Hooks used:** `post:render`, `page:head`

**Settings:**

| Setting            | Default       | Description                         |
|--------------------|---------------|-------------------------------------|
| `defaultTitle`     | `"Contento"`  | Fallback page title.                |
| `titleSeparator`   | `" — "`       | Character between post and site title. |
| `includeOpenGraph`  | `true`       | Generate Open Graph meta tags.      |
| `includeTwitterCard`| `true`       | Generate Twitter Card meta tags.    |

### social-share

Adds social media sharing buttons to posts. Supports configurable platforms and button placement.

**Hooks used:** `post:render`

**Settings:**

| Setting     | Default                                    | Description                    |
|-------------|--------------------------------------------|--------------------------------|
| `platforms` | `["twitter", "facebook", "linkedin"]`      | Which sharing platforms to show.|
| `position`  | `"bottom"`                                 | `"top"`, `"bottom"`, or `"both"`. |

### reading-progress

Displays a reading progress indicator bar at the top of the page as the user scrolls through a post.

**Hooks used:** `page:head`, `page:footer`

**Settings:**

| Setting   | Default       | Description                          |
|-----------|---------------|--------------------------------------|
| `color`   | `"--color-indigo"` | CSS variable or hex color for the bar. |
| `height`  | `"3px"`       | Height of the progress bar.          |

## Development Workflow

### 1. Create the Plugin Directory

```bash
mkdir plugins/my-plugin
```

### 2. Write the Manifest

Create `plugins/my-plugin/plugin.json` with the required fields.

### 3. Write the Main Script

Create `plugins/my-plugin/main.js` and register your hook handlers.

### 4. Restart Contento

Plugins are loaded at application startup. Restart the application to load your new plugin:

```bash
# If running with dotnet
dotnet run

# If running with Docker
docker compose restart contento
```

### 5. Verify Loading

Check the application log for plugin loading messages:

```
[INF] Plugin loaded: my-plugin v1.0.0 (2 hooks registered)
```

### 6. Test Your Plugin

Create or view a post to trigger the hooks your plugin handles. Check the rendered output and application logs.

### Debugging Tips

- Use `contento.log()` liberally during development to trace execution.
- Check the admin dashboard under **Settings > Plugins** to see loaded plugins and their status.
- If a plugin fails to load, the error message will appear in both the application log and the admin dashboard.

## Best Practices

### Keep Handlers Fast

Hooks execute synchronously in the request pipeline. A slow handler delays the entire page response. Stay well under the 5-second timeout.

```javascript
// Good: simple string manipulation
contento.on("post:render", function (context) {
  context.content += '<div class="share-buttons">...</div>';
  return context;
});

// Avoid: heavy computation in a render hook
contento.on("post:render", function (context) {
  for (var i = 0; i < 1000000; i++) { /* don't do this */ }
  return context;
});
```

### Always Return Context

If you forget to return the context, your modifications will be lost.

```javascript
// Correct
contento.on("post:render", function (context) {
  context.content += "<p>Appended content</p>";
  return context;  // modifications are preserved
});

// Bug: missing return
contento.on("post:render", function (context) {
  context.content += "<p>Appended content</p>";
  // context is lost, original is used instead
});
```

### Escape User-Generated Content

When injecting content into HTML, always escape it to prevent XSS.

```javascript
contento.on("page:head", function (context) {
  var safeTitle = contento.html.escape(context.page.title);
  context.tags += '<meta property="og:title" content="' + safeTitle + '">';
  return context;
});
```

### Use Meaningful Plugin Names

Plugin names should be lowercase, hyphenated, and descriptive. They serve as the unique identifier across the system.

```
seo-meta           (good)
social-share       (good)
reading-progress   (good)
myPlugin           (avoid: camelCase)
plugin1            (avoid: not descriptive)
```

### Declare Settings with Defaults

Always provide sensible defaults in your manifest so the plugin works out of the box without configuration.

## Troubleshooting

### Plugin not loading

- Verify `plugin.json` is valid JSON (no trailing commas, proper quoting).
- Ensure the `entry` path in the manifest points to an existing file.
- Check the application log for parse errors.

### Hook handler not firing

- Confirm you are using the exact hook name (they are case-sensitive).
- Verify the plugin appears in **Settings > Plugins** in the admin dashboard.
- Add a `contento.log()` call at the start of your handler to verify registration.

### Execution timeout errors

- Reduce the complexity of your handler logic.
- Avoid loops with unbounded iteration counts.
- Pre-compute values where possible instead of recalculating on each invocation.

### Memory limit exceeded

- Avoid building large strings or arrays incrementally in a loop.
- If processing post content, operate on substrings rather than copying the entire content repeatedly.
- The 64 MB limit is per plugin; most plugins should use well under 1 MB.

### Changes not appearing

- Confirm you restarted the application after modifying plugin files.
- Check that your handler returns the modified context.
- Verify another plugin further in the pipeline is not overwriting your changes.
