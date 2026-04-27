# Plugin System

The Contento plugin system is available for advanced customization. It uses a sandboxed JavaScript runtime (Jint) to extend CMS functionality via lifecycle hooks.

For pSEO workflows, the built-in schema engine and REST API are the recommended approach. Plugins are not required for standard pSEO page generation, niche management, or content schema operations.

## When to use plugins

- Injecting custom analytics or tracking scripts into generated pages
- Transforming rendered HTML output (e.g., adding lazy-loading attributes)
- Custom SEO meta tag generation beyond what the schema engine provides

## API documentation

For the REST API (recommended for pSEO workflows), see the [API Reference](api.html).

## Plugin basics

Plugins live in the `/plugins/` directory. Each plugin has a `plugin.json` manifest and a `main.js` entry point. Plugins register handlers for lifecycle hooks using `contento.on("hookName", handler)`.

```javascript
contento.on("post:render", function (context) {
  context.content += '<div class="custom-footer">Custom content</div>';
  return context;
});
```

### Sandbox constraints

| Constraint        | Default   |
|-------------------|-----------|
| Memory limit      | 64 MB     |
| Execution timeout | 5 seconds |
| Statement limit   | 100,000   |

Plugins cannot access the filesystem, network, or host process. They execute synchronously in the request pipeline.

For questions or advanced plugin development, open an issue on the Contento repository.
