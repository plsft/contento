using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Contento.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Contento.Services;

/// <summary>
/// Seeds default data for a fresh Contento installation
/// </summary>
public class DatabaseSeeder
{
    private readonly IDbConnection _db;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(IDbConnection db, ILogger<DatabaseSeeder> logger, IConfiguration configuration)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
        _configuration = Guard.Against.Null(configuration);
    }

    public async Task SeedAsync(bool seedDemoData = false)
    {
        await SeedDefaultSiteAsync();
        await SeedDefaultUserAsync();
        await SeedDefaultThemesAsync();
        await SeedDefaultLayoutAsync();
        await SeedDefaultPostTypesAsync();
        await SeedDefaultScheduledTasksAsync();
        await SeedDefaultMenusAsync();

        if (seedDemoData)
        {
            await SeedDemoPostsAsync();
        }

        _logger.LogInformation("Database seeding completed");
    }

    private async Task SeedDefaultSiteAsync()
    {
        var existing = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sites");
        if (existing > 0) return;

        var site = new Site
        {
            Id = Guid.NewGuid(),
            Name = "My Contento Site",
            Slug = "default",
            Tagline = "Content, made happy.",
            Locale = "en-US",
            Timezone = "UTC",
            IsPrimary = true
        };

        await _db.InsertAsync(site);
        _logger.LogInformation("Default site created: {SiteName}", site.Name);
    }

    private async Task SeedDefaultUserAsync()
    {
        var existing = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE email = 'admin@contento.local'");
        if (existing > 0) return;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@contento.local",
            DisplayName = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                _configuration["Admin:DefaultPassword"]
                ?? Environment.GetEnvironmentVariable("CONTENTO_ADMIN_PASSWORD")
                ?? throw new InvalidOperationException("Admin:DefaultPassword or CONTENTO_ADMIN_PASSWORD must be set")),
            Role = "owner",
            IsActive = true
        };

        await _db.InsertAsync(admin);
        _logger.LogInformation("Default admin user created: {Email}", admin.Email);
    }

    private async Task SeedDefaultThemesAsync()
    {
        // Each theme defines the FULL set of CSS variables used by the design system.
        // Variable names must match exactly what app.css :root defines.
        var themes = new[]
        {
            new Theme
            {
                Name = "Shizen",
                Slug = "shizen",
                Description = "Warm, organic, serif typography — inspired by nature",
                Version = "1.0.0",
                Author = "Contento",
                IsActive = true,
                CssVariables = """{"--font-serif":"'Noto Serif JP', Georgia, serif","--font-sans":"'Noto Sans JP', system-ui, sans-serif","--color-ink":"#2D2A24","--color-stone":"#7A7567","--color-ash":"#A39E93","--color-cloud":"#C8C3B8","--color-mist":"#E5E2DB","--color-paper":"#FAF8F5","--color-snow":"#FFFFFF","--color-indigo":"#8B6914","--color-indigo-light":"#B08A2E","--color-indigo-pale":"#F5F0E1"}"""
            },
            new Theme
            {
                Name = "Kōyō",
                Slug = "koyo",
                Description = "Rich earth tones, elegant — inspired by autumn leaves",
                Version = "1.0.0",
                Author = "Contento",
                CssVariables = """{"--font-serif":"'Noto Sans JP', system-ui, sans-serif","--font-sans":"'Noto Sans JP', system-ui, sans-serif","--color-ink":"#3C2415","--color-stone":"#7A5C48","--color-ash":"#A08672","--color-cloud":"#C4B09C","--color-mist":"#E5D8CA","--color-paper":"#FDF6EC","--color-snow":"#FFFFFF","--color-indigo":"#A0522D","--color-indigo-light":"#C47040","--color-indigo-pale":"#FCF0E6"}"""
            },
            new Theme
            {
                Name = "Yuki",
                Slug = "yuki",
                Description = "Stark white, minimal, sans-serif — inspired by snow",
                Version = "1.0.0",
                Author = "Contento",
                CssVariables = """{"--font-serif":"'Noto Sans JP', system-ui, sans-serif","--font-sans":"'Noto Sans JP', system-ui, sans-serif","--color-ink":"#1A1A1A","--color-stone":"#6B6B6B","--color-ash":"#9A9A9A","--color-cloud":"#BFBFBF","--color-mist":"#E5E5E5","--color-paper":"#FFFFFF","--color-snow":"#FFFFFF","--color-indigo":"#3D5A80","--color-indigo-light":"#5B7BA5","--color-indigo-pale":"#EEF2F7"}"""
            },
            new Theme
            {
                Name = "Ink",
                Slug = "ink",
                Description = "Modern editorial with bold display type and striking red accents",
                Version = "1.0.0",
                Author = "Contento",
                CssVariables = """{"--font-serif":"'Playfair Display', Georgia, serif","--font-sans":"'Inter', system-ui, sans-serif","--color-ink":"#1A1A1A","--color-stone":"#4A4A4A","--color-ash":"#8A8A8A","--color-cloud":"#BFBFBF","--color-mist":"#E0E0E0","--color-paper":"#FFFFFF","--color-snow":"#FFFFFF","--color-indigo":"#E63946","--color-indigo-light":"#F06570","--color-indigo-pale":"#FFF0F1"}"""
            },
            new Theme
            {
                Name = "Grove",
                Slug = "grove",
                Description = "Classic literary feel with warm parchment tones and deep green accents",
                Version = "1.0.0",
                Author = "Contento",
                CssVariables = """{"--font-serif":"'Merriweather', Georgia, serif","--font-sans":"'Source Sans 3', system-ui, sans-serif","--color-ink":"#2D2A24","--color-stone":"#5E5A52","--color-ash":"#8A857C","--color-cloud":"#B8B3AA","--color-mist":"#DDD9D2","--color-paper":"#FBF8F1","--color-snow":"#FFFFFF","--color-indigo":"#2D6A4F","--color-indigo-light":"#40916C","--color-indigo-pale":"#ECF5F0"}"""
            },
            new Theme
            {
                Name = "Neon",
                Slug = "neon",
                Description = "Playful creative vibe with soft purple accents and friendly sans-serif",
                Version = "1.0.0",
                Author = "Contento",
                CssVariables = """{"--font-serif":"'DM Sans', system-ui, sans-serif","--font-sans":"'DM Sans', system-ui, sans-serif","--color-ink":"#1A1A2E","--color-stone":"#4A4A5E","--color-ash":"#8888A0","--color-cloud":"#B5B5CC","--color-mist":"#E0E0ED","--color-paper":"#FAF5FF","--color-snow":"#FFFFFF","--color-indigo":"#7C3AED","--color-indigo-light":"#9F67FF","--color-indigo-pale":"#F3EEFF"}"""
            },
            new Theme
            {
                Name = "Folio",
                Slug = "folio",
                Description = "Minimal reading experience with elegant serif and teal accents",
                Version = "1.0.0",
                Author = "Contento",
                CssVariables = """{"--font-serif":"'Crimson Pro', Georgia, serif","--font-sans":"'Inter', system-ui, sans-serif","--color-ink":"#1C1917","--color-stone":"#57534E","--color-ash":"#A8A29E","--color-cloud":"#D6D3D1","--color-mist":"#E7E5E4","--color-paper":"#FEFDFB","--color-snow":"#FFFFFF","--color-indigo":"#0E7490","--color-indigo-light":"#22A8C4","--color-indigo-pale":"#E8F9FD"}"""
            },
            new Theme
            {
                Name = "Dusk",
                Slug = "dusk",
                Description = "Dark elegance with amber accents — perfect for night reading",
                Version = "1.0.0",
                Author = "Contento",
                CssVariables = """{"--font-serif":"'Lora', Georgia, serif","--font-sans":"'Inter', system-ui, sans-serif","--color-ink":"#E5E5E3","--color-stone":"#A3A3A0","--color-ash":"#737370","--color-cloud":"#525250","--color-mist":"#3A3A38","--color-paper":"#1A1A2E","--color-snow":"#242438","--color-indigo":"#F59E0B","--color-indigo-light":"#FBBF24","--color-indigo-pale":"#2D2A1A"}"""
            }
        };

        var seeded = 0;
        foreach (var theme in themes)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM themes WHERE slug = @Slug", new { theme.Slug });
            if (exists == 0)
            {
                await _db.InsertAsync(theme);
                seeded++;
            }
            else
            {
                // Update existing theme CSS variables to the expanded palette
                await _db.ExecuteAsync(
                    "UPDATE themes SET css_variables = @CssVariables::jsonb WHERE slug = @Slug",
                    new { theme.CssVariables, theme.Slug });
            }
        }

        if (seeded > 0)
            _logger.LogInformation("Themes seeded: {Count} new of {Total} total", seeded, themes.Length);
        else
            _logger.LogInformation("Themes updated: {Count} existing themes refreshed", themes.Length);
    }

    private async Task SeedDefaultLayoutAsync()
    {
        var siteId = await _db.ExecuteScalarAsync<Guid>("SELECT id FROM sites LIMIT 1");

        var layouts = new[]
        {
            new Layout
            {
                SiteId = siteId,
                Name = "Standard Blog",
                Slug = "standard-blog",
                Description = "Classic blog layout with header, content area, sidebar, and footer",
                IsDefault = true,
                Structure = """
                {
                    "grid": "12-col",
                    "rows": [
                        { "regions": [{ "region": "header", "cols": 12 }] },
                        { "regions": [{ "region": "menu", "cols": 12 }] },
                        { "regions": [{ "region": "meta", "cols": 12 }] },
                        { "regions": [
                            { "region": "body", "cols": 8 },
                            { "region": "right_nav", "cols": 4, "responsive": { "mobile": "hidden" } }
                        ]},
                        { "regions": [{ "region": "footer", "cols": 12 }] }
                    ],
                    "defaults": { "gap": "md", "maxWidth": "1280px", "padding": "lg" }
                }
                """
            },
            new Layout
            {
                SiteId = siteId,
                Name = "Full Width",
                Slug = "full-width",
                Description = "No sidebar — ideal for long-form articles and essays",
                Structure = """
                {
                    "grid": "12-col",
                    "rows": [
                        { "regions": [{ "region": "header", "cols": 12 }] },
                        { "regions": [{ "region": "meta", "cols": 12 }] },
                        { "regions": [{ "region": "body", "cols": 12 }] },
                        { "regions": [{ "region": "footer", "cols": 12 }] }
                    ],
                    "defaults": { "gap": "md", "maxWidth": "860px", "padding": "lg" }
                }
                """
            },
            new Layout
            {
                SiteId = siteId,
                Name = "Magazine",
                Slug = "magazine",
                Description = "Hero section with card grid and sidebar — great for content-heavy sites",
                Structure = """
                {
                    "grid": "12-col",
                    "rows": [
                        { "regions": [{ "region": "header", "cols": 12 }] },
                        { "regions": [{ "region": "hero", "cols": 12 }] },
                        { "regions": [
                            { "region": "body", "cols": 8 },
                            { "region": "sidebar", "cols": 4, "responsive": { "mobile": "below" } }
                        ]},
                        { "regions": [{ "region": "footer", "cols": 12 }] }
                    ],
                    "defaults": { "gap": "lg", "maxWidth": "1280px", "padding": "lg" }
                }
                """
            },
            new Layout
            {
                SiteId = siteId,
                Name = "Minimal",
                Slug = "minimal",
                Description = "Distraction-free reading with narrow centered column",
                Structure = """
                {
                    "grid": "12-col",
                    "rows": [
                        { "regions": [{ "region": "header", "cols": 12 }] },
                        { "regions": [{ "region": "body", "cols": 8, "offset": 2 }] },
                        { "regions": [{ "region": "footer", "cols": 12 }] }
                    ],
                    "defaults": { "gap": "sm", "maxWidth": "680px", "padding": "md" }
                }
                """
            }
        };

        var seeded = 0;
        foreach (var layout in layouts)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM layouts WHERE slug = @Slug AND site_id = @SiteId",
                new { layout.Slug, layout.SiteId });
            if (exists == 0)
            {
                await _db.InsertAsync(layout);
                seeded++;
            }
        }

        if (seeded > 0)
            _logger.LogInformation("Layouts seeded: {Count} new of {Total} total", seeded, layouts.Length);
    }

    private async Task SeedDefaultPostTypesAsync()
    {
        var siteId = await _db.ExecuteScalarAsync<Guid>("SELECT id FROM sites LIMIT 1");

        var postTypes = new[]
        {
            new PostType
            {
                SiteId = siteId,
                Name = "Blog Post",
                Slug = "post",
                Icon = "file-text",
                IsSystem = true,
                SortOrder = 0,
                Settings = """{"has_comments": true, "has_categories": true, "has_tags": true}"""
            },
            new PostType
            {
                SiteId = siteId,
                Name = "Page",
                Slug = "page",
                Icon = "layout",
                IsSystem = true,
                SortOrder = 1,
                Settings = """{"has_comments": false, "has_categories": false, "has_tags": false}"""
            },
            new PostType
            {
                SiteId = siteId,
                Name = "Product",
                Slug = "product",
                Icon = "shopping-bag",
                IsSystem = true,
                SortOrder = 2,
                Fields = """[{"name":"price","type":"number","label":"Price","required":true},{"name":"sku","type":"text","label":"SKU","required":false}]""",
                Settings = """{"has_comments": true, "has_categories": true, "has_tags": true}"""
            }
        };

        var seeded = 0;
        foreach (var pt in postTypes)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM post_types WHERE slug = @Slug AND site_id = @SiteId",
                new { pt.Slug, pt.SiteId });
            if (exists == 0)
            {
                await _db.InsertAsync(pt);
                seeded++;
            }
        }

        if (seeded > 0)
            _logger.LogInformation("Post types seeded: {Count} new of {Total} total", seeded, postTypes.Length);
    }

    private async Task SeedDefaultScheduledTasksAsync()
    {
        var siteId = await _db.ExecuteScalarAsync<Guid>("SELECT id FROM sites LIMIT 1");

        var tasks = new[]
        {
            new ScheduledTask
            {
                SiteId = siteId,
                Name = "Publish scheduled posts",
                TaskType = "publish_scheduled",
                CronExpression = "* * * * *",
                IsEnabled = true,
                NextRunAt = DateTime.UtcNow.AddMinutes(1)
            },
            new ScheduledTask
            {
                SiteId = siteId,
                Name = "Cleanup trashed posts",
                TaskType = "cleanup_trash",
                CronExpression = "0 3 * * *",
                IsEnabled = true,
                NextRunAt = DateTime.UtcNow.Date.AddDays(1).AddHours(3)
            },
            new ScheduledTask
            {
                SiteId = siteId,
                Name = "Expire memberships",
                TaskType = "expire_memberships",
                CronExpression = "0 * * * *",
                IsEnabled = true,
                NextRunAt = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour + 1)
            },
            new ScheduledTask
            {
                SiteId = siteId,
                Name = "Cleanup spam comments",
                TaskType = "cleanup_spam",
                CronExpression = "0 4 * * 0",
                IsEnabled = true,
                NextRunAt = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek).AddHours(4)
            }
        };

        var seeded = 0;
        foreach (var task in tasks)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM scheduled_tasks WHERE task_type = @TaskType AND site_id = @SiteId",
                new { task.TaskType, task.SiteId });
            if (exists == 0)
            {
                await _db.InsertAsync(task);
                seeded++;
            }
        }

        if (seeded > 0)
            _logger.LogInformation("Scheduled tasks seeded: {Count} new of {Total} total", seeded, tasks.Length);
    }

    private async Task SeedDefaultMenusAsync()
    {
        var siteId = await _db.ExecuteScalarAsync<Guid>("SELECT id FROM sites LIMIT 1");

        var menus = new[]
        {
            new
            {
                Menu = new Menu
                {
                    SiteId = siteId,
                    Name = "Main Navigation",
                    Slug = "main-navigation",
                    Location = "header",
                    IsActive = true
                },
                Items = new[]
                {
                    new MenuItem { Label = "Home", Url = "/", LinkType = "custom", SortOrder = 0 },
                    new MenuItem { Label = "Search", Url = "/search", LinkType = "custom", SortOrder = 1 }
                }
            },
            new
            {
                Menu = new Menu
                {
                    SiteId = siteId,
                    Name = "Footer",
                    Slug = "footer",
                    Location = "footer",
                    IsActive = true
                },
                Items = new[]
                {
                    new MenuItem { Label = "Home", Url = "/", LinkType = "custom", SortOrder = 0 },
                    new MenuItem { Label = "RSS Feed", Url = "/feed.xml", LinkType = "custom", SortOrder = 1 }
                }
            }
        };

        var seeded = 0;
        foreach (var entry in menus)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM menus WHERE slug = @Slug AND site_id = @SiteId",
                new { entry.Menu.Slug, entry.Menu.SiteId });
            if (exists == 0)
            {
                entry.Menu.Id = Guid.NewGuid();
                entry.Menu.CreatedAt = DateTime.UtcNow;
                entry.Menu.UpdatedAt = DateTime.UtcNow;
                await _db.InsertAsync(entry.Menu);

                foreach (var item in entry.Items)
                {
                    item.Id = Guid.NewGuid();
                    item.MenuId = entry.Menu.Id;
                    item.CreatedAt = DateTime.UtcNow;
                    await _db.InsertAsync(item);
                }

                seeded++;
            }
        }

        if (seeded > 0)
            _logger.LogInformation("Menus seeded: {Count} new of {Total} total", seeded, menus.Length);
    }

    private async Task SeedDemoPostsAsync()
    {
        var existing = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM posts");
        if (existing > 0) return;

        var siteId = await _db.ExecuteScalarAsync<Guid>("SELECT id FROM sites LIMIT 1");
        var userId = await _db.ExecuteScalarAsync<Guid>("SELECT id FROM users LIMIT 1");

        var post = new Post
        {
            SiteId = siteId,
            Title = "Welcome to Contento",
            Slug = "welcome-to-contento",
            Subtitle = "Your new writing home",
            Excerpt = "Contento is a writer-first content management system that doesn't feel like a CMS.",
            BodyMarkdown = """
# Welcome to Contento

Contento is what happens when you strip away every piece of CMS friction and rebuild from the writer's perspective.

## Why Contento?

- **Writer UX is sacred.** Every pixel serves the writer
- **Performance is non-negotiable.** Sub-100ms server responses
- **Security is foundational.** Every input validated, every route authorized
- **Beauty through restraint.** The Japanese principle of Ma (間)

## Getting Started

Start writing by clicking **New Post** in the dashboard. Your words deserve a beautiful home.

> [!tip]
> Use keyboard shortcuts for a faster writing experience: `Ctrl+B` for bold, `Ctrl+I` for italic.
""",
            BodyHtml = "",
            AuthorId = userId,
            Status = "published",
            Visibility = "public",
            PublishedAt = DateTime.UtcNow,
            Featured = true,
            WordCount = 89,
            ReadingTimeMinutes = 1,
            Tags = new[] { "welcome", "getting-started" }
        };

        await _db.InsertAsync(post);
        _logger.LogInformation("Demo post created: {Title}", post.Title);
    }
}
