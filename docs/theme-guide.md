# Custom Chrome

Custom Chrome replaces the traditional theme system in Contento pSEO v2.0. Instead of full theme packages with layouts and templates, you upload your own header HTML, footer HTML, and CSS to brand your pSEO-generated pages.

## How it works

Each pSEO project can have its own chrome — a header, footer, and stylesheet that wrap the generated page content. This lets you match the look and feel of your main site without building a full theme.

Chrome is configured per-project in the admin dashboard under **Projects > [Your Project] > Chrome**, or via the API:

```bash
curl -X PUT http://localhost:5000/api/v1/projects/{slug}/chrome \
  -H "Authorization: Bearer YOUR_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "header_html": "<header><nav>...</nav></header>",
    "footer_html": "<footer>...</footer>",
    "css": "body { font-family: Inter, sans-serif; }"
  }'
```

## What to include

- **Header HTML** — Navigation bar, logo, top-level links. Rendered above the page content.
- **Footer HTML** — Footer links, copyright, contact info. Rendered below the page content.
- **CSS** — Styles for your header, footer, and any global overrides. The pSEO page content uses its own schema-driven styles, but your CSS can override them.

## Migrating from themes

If you previously used the Contento theme system, extract the header and footer HTML from your theme's `_Header.cshtml` and `_Footer.cshtml` partials and paste them into the Chrome fields. Convert any Razor syntax to static HTML.

For the CSS, copy the relevant portions from your theme's `theme.css` — typically the header, footer, and navigation styles.

## Documentation

For full Chrome API details, see the [API Reference](api.html).
