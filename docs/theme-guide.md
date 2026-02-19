# Contento Theme Creation Guide

This guide covers everything you need to design, build, and package themes for Contento CMS. Contento themes follow the Wabi-Sabi design philosophy — embracing simplicity, natural aesthetics, and intentional restraint.

## Table of Contents

- [Design Philosophy](#design-philosophy)
- [Theme Structure](#theme-structure)
- [Theme Manifest](#theme-manifest)
- [CSS Architecture](#css-architecture)
- [CSS Variables Reference](#css-variables-reference)
- [Dark Mode Support](#dark-mode-support)
- [Layout Templates](#layout-templates)
- [Template Syntax](#template-syntax)
- [Built-in Themes](#built-in-themes)
- [Creating a Theme from Scratch](#creating-a-theme-from-scratch)
- [Responsive Design](#responsive-design)
- [Accessibility](#accessibility)
- [Testing Your Theme](#testing-your-theme)
- [Packaging and Distribution](#packaging-and-distribution)

---

## Design Philosophy

Contento's visual language is rooted in **Wabi-Sabi** (侘寂) — the Japanese aesthetic centered on the beauty of imperfection, impermanence, and incompleteness. When creating themes for Contento, consider these principles:

- **Ma (間)** — Negative space is not empty; it gives content room to breathe.
- **Kanso (簡素)** — Simplicity. Eliminate what is unnecessary.
- **Shibui (渋い)** — Subtle, understated beauty. Avoid flashy decoration.
- **Shizen (自然)** — Naturalness. Design should feel organic, not forced.
- **Fukinsei (不均斉)** — Asymmetry and imperfection create visual interest.

These are guidelines, not rules. Your theme should feel intentional and considered, whether it strictly follows Wabi-Sabi or takes its own creative direction.

## Theme Structure

A theme is a directory inside the `/Themes/` folder with the following structure:

```
Themes/
└── my-theme/
    ├── theme.json              # Theme manifest (required)
    ├── Layouts/                # Razor layout files
    │   ├── _Layout.cshtml      # Main layout (required)
    │   ├── _PostLayout.cshtml  # Single post layout
    │   ├── _PageLayout.cshtml  # Static page layout
    │   └── _ArchiveLayout.cshtml # Archive/listing layout
    ├── Partials/               # Reusable partial views
    │   ├── _Header.cshtml
    │   ├── _Footer.cshtml
    │   ├── _PostCard.cshtml
    │   ├── _Sidebar.cshtml
    │   ├── _Pagination.cshtml
    │   └── _Comments.cshtml
    ├── css/                    # Stylesheets
    │   ├── theme.css           # Main theme stylesheet (required)
    │   └── components/         # Component-specific styles
    ├── js/                     # JavaScript (optional)
    │   └── theme.js
    ├── img/                    # Theme images (optional)
    │   └── preview.png         # Theme preview image (recommended, 1200x800)
    └── fonts/                  # Custom fonts (optional)
```

## Theme Manifest

Every theme requires a `theme.json` file in its root directory.

### Required Fields

| Field         | Type     | Description                                   |
|---------------|----------|-----------------------------------------------|
| `name`        | `string` | Unique theme identifier (lowercase, hyphens). |
| `displayName` | `string` | Human-readable theme name.                    |
| `version`     | `string` | Semantic version.                             |
| `description` | `string` | Brief description of the theme.               |
| `author`      | `string` | Theme author name.                            |

### Optional Fields

| Field          | Type       | Description                                   |
|----------------|------------|-----------------------------------------------|
| `homepage`     | `string`   | URL to theme homepage or repository.          |
| `license`      | `string`   | SPDX license identifier.                      |
| `preview`      | `string`   | Relative path to preview image.               |
| `colors`       | `object`   | Color variable overrides (see CSS Variables).  |
| `fonts`        | `object`   | Font family declarations.                     |
| `supports`     | `string[]` | Feature flags: `"dark-mode"`, `"sidebar"`, `"comments"`, `"search"`. |
| `settings`     | `object`   | Admin-configurable settings schema.           |

### Example Manifest

```json
{
  "name": "sakura",
  "displayName": "Sakura",
  "version": "1.0.0",
  "description": "A theme inspired by cherry blossoms, with soft pinks and gentle transitions.",
  "author": "Your Name",
  "license": "MIT",
  "preview": "img/preview.png",
  "supports": [
    "dark-mode",
    "sidebar",
    "comments",
    "search"
  ],
  "colors": {
    "--color-ink": "#2d2226",
    "--color-stone": "#5c4a51",
    "--color-ash": "#8a7680",
    "--color-cloud": "#c4b5bb",
    "--color-mist": "#e8dde1",
    "--color-paper": "#f5eff1",
    "--color-snow": "#faf7f8",
    "--color-indigo": "#c45b84"
  },
  "fonts": {
    "heading": "'Noto Serif JP', serif",
    "body": "'Noto Sans JP', sans-serif",
    "mono": "'JetBrains Mono', monospace"
  },
  "settings": {
    "showAuthorBio": {
      "type": "boolean",
      "label": "Show author biography on posts",
      "default": true
    },
    "postsPerPage": {
      "type": "number",
      "label": "Posts per page",
      "default": 10
    }
  }
}
```

## CSS Architecture

Contento themes use CSS custom properties (variables) as the foundation for all visual styling. This approach enables dark mode support, theme switching, and consistent design tokens across components.

### Loading Order

Stylesheets are loaded in this order, with later declarations overriding earlier ones:

1. **TailBreeze base** — Utility-first CSS foundation from Noundry.
2. **Contento core** — CMS structural styles and default variables.
3. **Theme stylesheet** — Your `theme.css` file.
4. **Plugin stylesheets** — Any styles injected by active plugins.

### File Organization

Keep your CSS modular and organized:

```css
/* theme.css - Main entry point */

/* ============================================
   1. CSS Variables (color, typography, spacing)
   2. Base elements (body, headings, links)
   3. Layout (header, main, footer, sidebar)
   4. Components (cards, buttons, forms, nav)
   5. Content (post body, comments, media)
   6. Utilities (visibility, spacing helpers)
   7. Dark mode overrides
   ============================================ */
```

## CSS Variables Reference

Contento defines a standard set of CSS custom properties. Themes override these to establish their visual identity.

### Color Variables

These eight colors form the complete palette. They progress from darkest to lightest, representing a spectrum from ink on paper.

| Variable          | Role                    | Shizen Default | Usage                              |
|-------------------|-------------------------|----------------|------------------------------------|
| `--color-ink`     | Primary text            | `#1a1a2e`      | Body text, headings                |
| `--color-stone`   | Secondary text          | `#4a4a5a`      | Subtitles, metadata, captions      |
| `--color-ash`     | Tertiary text           | `#7a7a8a`      | Muted text, placeholders           |
| `--color-cloud`   | Borders                 | `#c4c4d0`      | Dividers, input borders            |
| `--color-mist`    | Subtle backgrounds      | `#e8e8f0`      | Code blocks, table stripes         |
| `--color-paper`   | Content background      | `#f5f5f8`      | Card backgrounds, sidebar          |
| `--color-snow`    | Page background         | `#fafafe`      | Main page background               |
| `--color-indigo`  | Accent color            | `#4a5568`      | Links, buttons, interactive elements |

### Typography Variables

| Variable              | Default                         | Description                  |
|-----------------------|---------------------------------|------------------------------|
| `--font-heading`      | `'Georgia', serif`              | Heading font stack.          |
| `--font-body`         | `'system-ui', sans-serif`       | Body text font stack.        |
| `--font-mono`         | `'Menlo', monospace`            | Code and monospace font.     |
| `--font-size-base`    | `1rem`                          | Base font size.              |
| `--line-height-body`  | `1.7`                           | Body text line height.       |
| `--line-height-heading`| `1.2`                          | Heading line height.         |

### Spacing Variables

| Variable              | Default   | Description                    |
|-----------------------|-----------|--------------------------------|
| `--spacing-xs`        | `0.25rem` | Extra small spacing.           |
| `--spacing-sm`        | `0.5rem`  | Small spacing.                 |
| `--spacing-md`        | `1rem`    | Medium spacing (base unit).    |
| `--spacing-lg`        | `2rem`    | Large spacing.                 |
| `--spacing-xl`        | `4rem`    | Extra large spacing.           |
| `--content-width`     | `48rem`   | Maximum width of content area. |
| `--sidebar-width`     | `16rem`   | Sidebar width.                 |

### Transition Variables

| Variable               | Default                    | Description                  |
|------------------------|----------------------------|------------------------------|
| `--transition-fast`    | `150ms ease`               | Fast UI transitions.         |
| `--transition-normal`  | `300ms ease`               | Standard transitions.        |
| `--transition-slow`    | `500ms ease`               | Slow, deliberate transitions.|

### Overriding Variables

Override variables in your `theme.css`:

```css
:root {
  /* Koyo (Autumn) palette */
  --color-ink: #2d1b00;
  --color-stone: #5c3d1e;
  --color-ash: #8a6a4a;
  --color-cloud: #c4a882;
  --color-mist: #e8d5be;
  --color-paper: #f5ebe0;
  --color-snow: #faf6f0;
  --color-indigo: #b85c2c;

  /* Typography */
  --font-heading: 'Playfair Display', serif;
  --font-body: 'Source Sans 3', sans-serif;

  /* Wider content area */
  --content-width: 52rem;
}
```

## Dark Mode Support

Contento supports automatic dark mode via the `prefers-color-scheme` media query. Themes opt in by declaring `"dark-mode"` in the `supports` array and providing dark overrides.

### Implementing Dark Mode

The pattern is straightforward: swap the color scale so dark backgrounds use light text.

```css
/* Light mode (default) */
:root {
  --color-ink: #1a1a2e;
  --color-stone: #4a4a5a;
  --color-ash: #7a7a8a;
  --color-cloud: #c4c4d0;
  --color-mist: #e8e8f0;
  --color-paper: #f5f5f8;
  --color-snow: #fafafe;
  --color-indigo: #4a5568;
}

/* Dark mode */
@media (prefers-color-scheme: dark) {
  :root {
    --color-ink: #e8e8f0;
    --color-stone: #c4c4d0;
    --color-ash: #7a7a8a;
    --color-cloud: #4a4a5a;
    --color-mist: #2a2a3e;
    --color-paper: #1e1e30;
    --color-snow: #16162a;
    --color-indigo: #8a9ab5;
  }
}
```

### Dark Mode Design Principles

1. **Do not simply invert colors.** A true dark theme requires adjusted contrast ratios, not a mechanical reversal.
2. **Reduce contrast slightly.** Pure white (`#fff`) on pure black (`#000`) creates eye strain. Use off-white on dark gray.
3. **Desaturate accent colors.** Bright, saturated colors on dark backgrounds appear to glow. Reduce saturation by 10-20%.
4. **Maintain the hierarchy.** The relative contrast between `ink`, `stone`, and `ash` should be preserved in both modes.
5. **Test with images.** Ensure post images and media look natural against the dark background.

### Manual Dark Mode Toggle

For a user-controlled toggle (in addition to automatic detection), use Alpine.js:

```html
<button
  x-data="{ dark: false }"
  x-init="dark = document.documentElement.classList.contains('dark')"
  @click="dark = !dark; document.documentElement.classList.toggle('dark')"
  :aria-label="dark ? 'Switch to light mode' : 'Switch to dark mode'"
>
  <span x-show="!dark">Moon Icon</span>
  <span x-show="dark">Sun Icon</span>
</button>
```

Add corresponding CSS:

```css
/* Manual toggle override */
html.dark {
  --color-ink: #e8e8f0;
  --color-stone: #c4c4d0;
  --color-ash: #7a7a8a;
  --color-cloud: #4a4a5a;
  --color-mist: #2a2a3e;
  --color-paper: #1e1e30;
  --color-snow: #16162a;
  --color-indigo: #8a9ab5;
}
```

## Layout Templates

Themes define page layouts using Razor (`.cshtml`) files. Contento looks for specific layout files by convention.

### Required Layouts

#### `_Layout.cshtml`

The master layout wrapping every page. Must include the `@RenderBody()` directive.

```html
@{
    Layout = null;
}
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - @ViewData["SiteName"]</title>

    <link rel="stylesheet" href="/css/tailbreeze.css" />
    <link rel="stylesheet" href="/themes/@ViewData["Theme"]/css/theme.css" />
    <link rel="alternate" type="application/rss+xml" href="/feed.xml" title="RSS Feed" />

    @RenderSection("Head", required: false)
    @Html.Raw(ViewData["PluginHeadTags"])
</head>
<body style="background-color: var(--color-snow); color: var(--color-ink);">
    <partial name="Partials/_Header" />

    <main>
        @RenderBody()
    </main>

    <partial name="Partials/_Footer" />

    <script src="/js/alpine.min.js" defer></script>
    @RenderSection("Scripts", required: false)
    @Html.Raw(ViewData["PluginFooterHtml"])
</body>
</html>
```

### Optional Layouts

#### `_PostLayout.cshtml`

Layout for individual blog posts. Inherits from `_Layout.cshtml`.

```html
@{
    Layout = "_Layout";
    ViewData["Title"] = Model.Post.Title;
}

<article class="post" itemscope itemtype="http://schema.org/BlogPosting">
    <header class="post-header">
        <h1 itemprop="headline">@Model.Post.Title</h1>
        <div class="post-meta" style="color: var(--color-stone);">
            <time datetime="@Model.Post.PublishedAt.ToString("o")" itemprop="datePublished">
                @Model.Post.PublishedAt.ToString("MMMM d, yyyy")
            </time>
            <span>by</span>
            <span itemprop="author">@Model.Post.Author.DisplayName</span>
        </div>
    </header>

    @if (Model.Post.FeaturedImage != null)
    {
        <figure class="post-featured-image">
            <img src="@Model.Post.FeaturedImage"
                 alt="@Model.Post.Title"
                 loading="lazy"
                 itemprop="image" />
        </figure>
    }

    <div class="post-content" itemprop="articleBody">
        @Html.Raw(Model.Post.RenderedContent)
    </div>

    @if (Model.Post.Categories.Any())
    {
        <footer class="post-footer">
            <div class="post-categories">
                @foreach (var category in Model.Post.Categories)
                {
                    <a href="/category/@category.Slug"
                       style="color: var(--color-indigo);">
                        @category.Name
                    </a>
                }
            </div>
        </footer>
    }

    <partial name="Partials/_Comments" model="Model.Comments" />
</article>
```

#### `_ArchiveLayout.cshtml`

Layout for post listings, category pages, and search results.

```html
@{
    Layout = "_Layout";
    ViewData["Title"] = Model.Title;
}

<div class="archive">
    <h1>@Model.Title</h1>

    @if (!string.IsNullOrEmpty(Model.Description))
    {
        <p style="color: var(--color-stone);">@Model.Description</p>
    }

    <div class="post-list">
        @foreach (var post in Model.Posts)
        {
            <partial name="Partials/_PostCard" model="post" />
        }
    </div>

    <partial name="Partials/_Pagination" model="Model.Pagination" />
</div>
```

## Template Syntax

Contento uses Razor syntax for templates. Here are common patterns used in themes.

### Conditional Rendering

```html
@if (Model.Post.FeaturedImage != null)
{
    <img src="@Model.Post.FeaturedImage" alt="" />
}
```

### Loops

```html
@foreach (var post in Model.Posts)
{
    <article>
        <h2><a href="/@post.Slug">@post.Title</a></h2>
        <p>@post.Excerpt</p>
    </article>
}
```

### Alpine.js Interactivity

Contento uses Alpine.js for lightweight client-side interactivity. Use it in your theme templates for menus, dropdowns, and toggles.

```html
<!-- Mobile menu toggle -->
<nav x-data="{ open: false }">
    <button @click="open = !open" aria-label="Toggle menu">
        Menu
    </button>
    <ul x-show="open" x-transition>
        <li><a href="/">Home</a></li>
        <li><a href="/archive">Archive</a></li>
        <li><a href="/about">About</a></li>
    </ul>
</nav>

<!-- Search overlay -->
<div x-data="{ showSearch: false }">
    <button @click="showSearch = true">Search</button>
    <div x-show="showSearch"
         x-transition.opacity
         @keydown.escape.window="showSearch = false"
         class="search-overlay">
        <input type="search"
               placeholder="Search..."
               x-ref="searchInput"
               @input.debounce.300ms="/* search logic */" />
        <button @click="showSearch = false">Close</button>
    </div>
</div>
```

## Built-in Themes

Study the three built-in themes to understand conventions and patterns.

### Shizen (自然 — Nature)

The default theme. Warm, neutral tones inspired by natural materials — wood, stone, and paper.

- **Palette:** Warm grays with subtle warmth, muted indigo accent.
- **Typography:** Serif headings, clean sans-serif body text.
- **Character:** Calm, grounded, like a paper notebook.

### Koyo (紅葉 — Autumn Leaves)

Rich autumnal palette with amber and crimson accents.

- **Palette:** Warm browns and oranges, burnt sienna accent.
- **Typography:** Elegant serif throughout.
- **Character:** Warm, rich, like an autumn afternoon.

### Yuki (雪 — Snow)

Minimal and cool, inspired by snow-covered landscapes.

- **Palette:** Cool grays and whites, steel blue accent.
- **Typography:** Light-weight sans-serif, generous spacing.
- **Character:** Serene, spacious, like fresh snowfall.

## Creating a Theme from Scratch

Follow this step-by-step walkthrough to create a complete theme.

### Step 1: Create the Theme Directory

```bash
mkdir -p Themes/sakura/{Layouts,Partials,css,js,img}
```

### Step 2: Define the Manifest

Create `Themes/sakura/theme.json`:

```json
{
  "name": "sakura",
  "displayName": "Sakura",
  "version": "1.0.0",
  "description": "Cherry blossom inspired theme with soft pinks and gentle movement.",
  "author": "Your Name",
  "preview": "img/preview.png",
  "supports": ["dark-mode", "sidebar", "comments", "search"],
  "colors": {
    "--color-ink": "#2d2226",
    "--color-stone": "#5c4a51",
    "--color-ash": "#8a7680",
    "--color-cloud": "#c4b5bb",
    "--color-mist": "#e8dde1",
    "--color-paper": "#f5eff1",
    "--color-snow": "#faf7f8",
    "--color-indigo": "#c45b84"
  }
}
```

### Step 3: Write the Stylesheet

Create `Themes/sakura/css/theme.css`:

```css
/* ===========================================
   Sakura Theme — Cherry Blossom
   =========================================== */

/* --- Variables --- */
:root {
  --color-ink: #2d2226;
  --color-stone: #5c4a51;
  --color-ash: #8a7680;
  --color-cloud: #c4b5bb;
  --color-mist: #e8dde1;
  --color-paper: #f5eff1;
  --color-snow: #faf7f8;
  --color-indigo: #c45b84;

  --font-heading: 'Noto Serif', 'Georgia', serif;
  --font-body: 'Noto Sans', 'system-ui', sans-serif;
  --font-mono: 'JetBrains Mono', 'Menlo', monospace;

  --content-width: 48rem;
  --sidebar-width: 16rem;
}

/* --- Base --- */
body {
  font-family: var(--font-body);
  font-size: var(--font-size-base, 1rem);
  line-height: var(--line-height-body, 1.7);
  color: var(--color-ink);
  background-color: var(--color-snow);
}

h1, h2, h3, h4, h5, h6 {
  font-family: var(--font-heading);
  line-height: var(--line-height-heading, 1.2);
  color: var(--color-ink);
}

a {
  color: var(--color-indigo);
  text-decoration: none;
  transition: color var(--transition-fast, 150ms ease);
}

a:hover {
  color: var(--color-ink);
}

/* --- Layout --- */
.site-header {
  padding: var(--spacing-lg, 2rem) 0;
  border-bottom: 1px solid var(--color-mist);
}

.site-header .site-title {
  font-family: var(--font-heading);
  font-size: 1.5rem;
  color: var(--color-ink);
}

.site-nav a {
  color: var(--color-stone);
  margin-left: var(--spacing-md, 1rem);
}

.site-nav a:hover {
  color: var(--color-indigo);
}

main {
  max-width: var(--content-width);
  margin: 0 auto;
  padding: var(--spacing-xl, 4rem) var(--spacing-md, 1rem);
}

.site-footer {
  padding: var(--spacing-lg, 2rem) 0;
  border-top: 1px solid var(--color-mist);
  color: var(--color-ash);
  font-size: 0.875rem;
  text-align: center;
}

/* --- Post Card --- */
.post-card {
  padding: var(--spacing-lg, 2rem) 0;
  border-bottom: 1px solid var(--color-mist);
}

.post-card:last-child {
  border-bottom: none;
}

.post-card h2 {
  font-size: 1.5rem;
  margin-bottom: var(--spacing-xs, 0.25rem);
}

.post-card .post-meta {
  color: var(--color-ash);
  font-size: 0.875rem;
  margin-bottom: var(--spacing-sm, 0.5rem);
}

.post-card .post-excerpt {
  color: var(--color-stone);
}

/* --- Single Post --- */
.post-header {
  margin-bottom: var(--spacing-xl, 4rem);
  text-align: center;
}

.post-header h1 {
  font-size: 2.5rem;
  margin-bottom: var(--spacing-sm, 0.5rem);
}

.post-header .post-meta {
  color: var(--color-ash);
}

.post-content {
  font-size: 1.125rem;
}

.post-content p {
  margin-bottom: var(--spacing-md, 1rem);
}

.post-content img {
  max-width: 100%;
  height: auto;
  border-radius: 0.25rem;
}

.post-content blockquote {
  border-left: 3px solid var(--color-indigo);
  padding-left: var(--spacing-md, 1rem);
  color: var(--color-stone);
  font-style: italic;
}

.post-content pre {
  background-color: var(--color-paper);
  padding: var(--spacing-md, 1rem);
  border-radius: 0.25rem;
  overflow-x: auto;
  font-family: var(--font-mono);
  font-size: 0.875rem;
}

.post-content code {
  font-family: var(--font-mono);
  font-size: 0.875em;
  background-color: var(--color-mist);
  padding: 0.1em 0.3em;
  border-radius: 0.2em;
}

.post-content pre code {
  background: none;
  padding: 0;
}

/* --- Comments --- */
.comments {
  margin-top: var(--spacing-xl, 4rem);
  padding-top: var(--spacing-lg, 2rem);
  border-top: 1px solid var(--color-mist);
}

.comment {
  padding: var(--spacing-md, 1rem) 0;
  border-bottom: 1px solid var(--color-mist);
}

.comment-author {
  font-weight: 600;
  color: var(--color-ink);
}

.comment-date {
  color: var(--color-ash);
  font-size: 0.875rem;
}

/* --- Pagination --- */
.pagination {
  display: flex;
  justify-content: center;
  gap: var(--spacing-sm, 0.5rem);
  margin-top: var(--spacing-xl, 4rem);
}

.pagination a,
.pagination span {
  padding: var(--spacing-xs, 0.25rem) var(--spacing-sm, 0.5rem);
  border-radius: 0.25rem;
}

.pagination .current {
  background-color: var(--color-indigo);
  color: var(--color-snow);
}

/* --- Dark Mode --- */
@media (prefers-color-scheme: dark) {
  :root {
    --color-ink: #f0e6ea;
    --color-stone: #c4b5bb;
    --color-ash: #8a7680;
    --color-cloud: #5c4a51;
    --color-mist: #3d2e33;
    --color-paper: #2d2226;
    --color-snow: #231a1e;
    --color-indigo: #e87aaa;
  }
}

html.dark {
  --color-ink: #f0e6ea;
  --color-stone: #c4b5bb;
  --color-ash: #8a7680;
  --color-cloud: #5c4a51;
  --color-mist: #3d2e33;
  --color-paper: #2d2226;
  --color-snow: #231a1e;
  --color-indigo: #e87aaa;
}
```

### Step 4: Create Layout Templates

Create `_Layout.cshtml`, `_PostLayout.cshtml`, and partials following the patterns shown in the [Layout Templates](#layout-templates) section above. Copy from a built-in theme and customize.

### Step 5: Add a Preview Image

Create a 1200x800 screenshot of your theme and save it as `Themes/sakura/img/preview.png`. This is displayed in the admin theme selector.

### Step 6: Activate the Theme

In the admin dashboard, go to **Settings > Appearance** and select your theme. Or set it in configuration:

```json
{
  "Contento": {
    "Theme": "sakura"
  }
}
```

## Responsive Design

Themes should be responsive. Use a mobile-first approach with CSS custom properties.

```css
/* Mobile first (default) */
main {
  padding: var(--spacing-md) var(--spacing-sm);
}

.post-header h1 {
  font-size: 1.75rem;
}

/* Tablet and up */
@media (min-width: 640px) {
  main {
    padding: var(--spacing-lg) var(--spacing-md);
  }

  .post-header h1 {
    font-size: 2rem;
  }
}

/* Desktop and up */
@media (min-width: 1024px) {
  main {
    max-width: var(--content-width);
    margin: 0 auto;
    padding: var(--spacing-xl) var(--spacing-md);
  }

  .post-header h1 {
    font-size: 2.5rem;
  }
}
```

### Breakpoint Conventions

| Breakpoint | Width     | Target           |
|------------|-----------|------------------|
| `sm`       | `640px`   | Large phones     |
| `md`       | `768px`   | Tablets          |
| `lg`       | `1024px`  | Small desktops   |
| `xl`       | `1280px`  | Large desktops   |

## Accessibility

Themes must meet WCAG 2.1 Level AA standards at minimum.

### Color Contrast

Ensure text-to-background contrast ratios meet these minimums:

| Element         | Minimum Ratio | Example                            |
|-----------------|---------------|------------------------------------|
| Body text       | 4.5:1         | `--color-ink` on `--color-snow`    |
| Large text (18px+) | 3:1       | Headings on page background        |
| Interactive     | 3:1           | `--color-indigo` on `--color-snow` |

### Focus Indicators

Always provide visible focus indicators for keyboard navigation:

```css
a:focus-visible,
button:focus-visible,
input:focus-visible {
  outline: 2px solid var(--color-indigo);
  outline-offset: 2px;
}
```

### Semantic HTML

- Use `<header>`, `<main>`, `<footer>`, `<nav>`, `<article>`, `<aside>` landmark elements.
- Use heading levels (`h1`-`h6`) in proper hierarchical order.
- Add `alt` attributes to all images.
- Use `aria-label` on icon-only buttons.

### Reduced Motion

Respect the user's motion preferences:

```css
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

## Testing Your Theme

### Visual Testing Checklist

Verify your theme with these content types and states:

- [ ] Post with no featured image
- [ ] Post with featured image
- [ ] Post with long title (100+ characters)
- [ ] Post with code blocks and inline code
- [ ] Post with blockquotes, lists, and tables
- [ ] Post with embedded images of various aspect ratios
- [ ] Archive page with 10+ posts
- [ ] Category page with description
- [ ] Page with comments (0, 1, many)
- [ ] Search results page (with results, empty results)
- [ ] 404 page
- [ ] Light mode and dark mode
- [ ] Mobile, tablet, and desktop viewports
- [ ] Print stylesheet (if supported)

### Browser Testing

Test in at minimum:

- Chrome / Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile Safari (iOS)
- Chrome (Android)

### Performance

- Keep total CSS under 50 KB (uncompressed).
- Optimize images; use modern formats (WebP, AVIF) where possible.
- Minimize custom JavaScript; lean on Alpine.js for interactivity.
- Test with browser DevTools Lighthouse audit.

## Packaging and Distribution

### Preparing for Distribution

1. Ensure all files are present and the theme works with a fresh Contento installation.
2. Add a `preview.png` screenshot.
3. Include a `README.md` in the theme directory with installation and customization instructions.
4. Remove any development artifacts or temporary files.

### Directory Structure for Distribution

```
sakura-theme-1.0.0/
├── theme.json
├── README.md
├── Layouts/
│   ├── _Layout.cshtml
│   ├── _PostLayout.cshtml
│   ├── _PageLayout.cshtml
│   └── _ArchiveLayout.cshtml
├── Partials/
│   ├── _Header.cshtml
│   ├── _Footer.cshtml
│   ├── _PostCard.cshtml
│   ├── _Sidebar.cshtml
│   ├── _Pagination.cshtml
│   └── _Comments.cshtml
├── css/
│   └── theme.css
├── img/
│   └── preview.png
└── fonts/
    └── (if any custom fonts)
```

### Installation

To install a theme, copy its directory into the Contento `/Themes/` folder and restart the application:

```bash
cp -r sakura-theme-1.0.0/ /path/to/contento/Themes/sakura/
```

The theme will appear in the admin dashboard under **Settings > Appearance**.
