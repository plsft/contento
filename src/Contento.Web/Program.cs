using System.Data;
using System.Security.Claims;
using Npgsql;
using Serilog;
using StackExchange.Redis;
using Noundry.Authnz.Extensions;
using Noundry.Authnz.Models;
using Noundry.Tuxedo.Bowtie.Core;
using Tailbreeze.Extensions;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Plugins;
using Contento.Services;
using Contento.Web.Auth;
using Contento.Web.BackgroundServices;
using Contento.Web.Middleware;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Noundry.AIG.Client;
using Noundry.AIG.Client.Configuration;
using Noundry.AIG.Core.Interfaces;
using Noundry.AIG.Providers;
using BowtieDI = Noundry.Tuxedo.Bowtie.Extensions.ServiceCollectionExtensions;

// Bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load configuration
    builder.Configuration.AddEnvironmentVariables();

    // Configure Serilog
    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
            .WriteTo.Console());

    // --- Database ---
    var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    string connectionString;

    if (string.IsNullOrEmpty(configConnectionString) || configConnectionString.StartsWith("${"))
    {
        connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "DATABASE_CONNECTION_STRING environment variable is required.");
    }
    else
    {
        connectionString = configConnectionString;
    }

    Log.Information("Using database connection: {ConnectionString}",
        connectionString.Contains("Password=")
            ? connectionString[..connectionString.IndexOf("Password=")] + "Password=***"
            : connectionString);

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    var dataSource = dataSourceBuilder.Build();
    builder.Services.AddSingleton(dataSource);

    builder.Services.AddScoped<IDbConnection>(sp =>
    {
        var ds = sp.GetRequiredService<NpgsqlDataSource>();
        return ds.OpenConnection();
    });

    // --- Redis ---
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrEmpty(redisConnectionString) || redisConnectionString.StartsWith("${"))
    {
        redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
    }

    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        Log.Information("Redis connected: {Endpoint}", redisConnectionString);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis connection failed — caching disabled. Endpoint: {Endpoint}", redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => null!);
    }

    // --- Application Services ---
    builder.Services.AddScoped<ISiteService, SiteService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IPostService, PostService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<ILayoutService, LayoutService>();
    builder.Services.AddScoped<IThemeService, ThemeService>();
    builder.Services.AddScoped<IMediaService, MediaService>();
    builder.Services.AddScoped<IPluginService, PluginService>();
    builder.Services.AddScoped<ITrafficService, TrafficService>();
    builder.Services.AddScoped<ISearchService, SearchService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<IMarkdownService, MarkdownService>();
    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<IImportExportService, ImportExportService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<DatabaseSeeder>();

    // --- New Services (Phase 2) ---
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<INewsletterService, NewsletterService>();
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
    // --- File Storage (SharpGrip.FileSystem — local + optional S3/MinIO) ---
    var storageProvider = builder.Configuration["Storage:Provider"] ?? "local";
    var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads");
    Directory.CreateDirectory(uploadsPath);

    builder.Services.AddSingleton<SharpGrip.FileSystem.IFileSystem>(sp =>
    {
        var adapters = new List<SharpGrip.FileSystem.Adapters.IAdapter>
        {
            new SharpGrip.FileSystem.Adapters.LocalAdapter("local", uploadsPath)
        };

        if (storageProvider == "s3")
        {
            var s3Endpoint = builder.Configuration["Storage:S3:Endpoint"];
            var s3AccessKey = builder.Configuration["Storage:S3:AccessKey"];
            var s3SecretKey = builder.Configuration["Storage:S3:SecretKey"];
            var s3Bucket = builder.Configuration["Storage:S3:Bucket"] ?? "contento";
            var s3Region = builder.Configuration["Storage:S3:Region"] ?? "us-east-1";

            if (!string.IsNullOrEmpty(s3Endpoint) && !s3Endpoint.StartsWith("${"))
            {
                var s3Config = new Amazon.S3.AmazonS3Config
                {
                    ServiceURL = s3Endpoint,
                    ForcePathStyle = true // Required for MinIO
                };
                var s3Client = new Amazon.S3.AmazonS3Client(s3AccessKey, s3SecretKey, s3Config);
                adapters.Add(new SharpGrip.FileSystem.Adapters.AmazonS3.AmazonS3Adapter("s3", "/", s3Client, s3Bucket));
            }
        }

        return new SharpGrip.FileSystem.FileSystem(adapters);
    });
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<ILayoutRenderer, LayoutRenderer>();

    // --- Phase 3 Services ---
    builder.Services.AddScoped<ISpamService, SpamService>();
    builder.Services.AddScoped<IPostTypeService, PostTypeService>();
    builder.Services.AddScoped<ITaskSchedulerService, TaskSchedulerService>();
    builder.Services.AddScoped<ISeoService, SeoService>();
    builder.Services.AddScoped<IMembershipPlanService, MembershipPlanService>();
    builder.Services.AddScoped<IMenuService, MenuService>();
    builder.Services.AddScoped<IRedirectService, RedirectService>();
    builder.Services.AddScoped<IOEmbedService, OEmbedService>();
    builder.Services.AddScoped<IShortcodeProcessor, ShortcodeProcessor>();
    builder.Services.AddSingleton<IPostLockService, PostLockService>();

    // --- Plugin Runtime (singleton — engines persist across requests) ---
    builder.Services.AddSingleton<IPluginRuntime>(sp =>
    {
        var pluginService = sp.CreateScope().ServiceProvider.GetRequiredService<IPluginService>();
        var logger = sp.GetRequiredService<ILogger<PluginHost>>();
        return new PluginHost(logger, pluginService);
    });

    // --- Task Scheduler Background Service (replaces ScheduledPublishingService) ---
    builder.Services.AddHostedService<TaskSchedulerBackgroundService>();

    // --- AI Gateway (BYOK) ---
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<IProviderFactory, ProviderFactory>();
    builder.Services.AddSingleton(new AigClientOptions { UseLocalProviders = true, EnableRetries = true });
    builder.Services.AddSingleton<IAigClient, AigClient>();
    builder.Services.AddScoped<IAiService, AiService>();

    // --- Register Bowtie for schema migrations ---
    BowtieDI.AddBowtie(builder.Services);

    // --- OpenAPI ---
    builder.Services.AddOpenApi();

    // --- Razor Pages ---
    builder.Services.AddRazorPages();
    builder.Services.AddControllers();
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
    }

    // --- Tailbreeze (Tailwind CSS v4) ---
    builder.Services.AddTailbreeze(options =>
    {
        options.TailwindVersion = "4";
        options.InputCssPath = "Styles/app.css";
        options.OutputCssPath = "css/app.css";
        options.EnableHotReload = builder.Environment.IsDevelopment();
    });

    // --- OAuth authentication ---
    builder.Services.AddNoundryOAuth(builder.Configuration, options =>
    {
        options.OnOAuthSuccess = async context =>
        {
            var userService = context.GetRequiredService<IUserService>();
            var email = context.UserInfo.Email;

            if (string.IsNullOrWhiteSpace(email))
                return OAuthSuccessResult.WithRedirect("/admin/login?error=email_required");

            email = email.ToLowerInvariant();
            var user = await userService.GetByEmailAsync(email);

            if (user == null)
            {
                var newUser = new User
                {
                    Email = email,
                    DisplayName = context.UserInfo.Name ?? email.Split('@')[0],
                    Role = "viewer",
                    IsActive = true,
                    PasswordHash = ""
                };
                user = await userService.CreateAsync(newUser, Guid.NewGuid().ToString());
            }

            if (!user.IsActive)
                return OAuthSuccessResult.WithRedirect("/admin/login?error=account_disabled");

            await userService.UpdateLastLoginAsync(user.Id);

            return new OAuthSuccessResult
            {
                AdditionalClaims = new List<Claim>
                {
                    new("app_user_id", user.Id.ToString()),
                    new(ClaimTypes.Role, user.Role),
                    new("display_name", user.DisplayName)
                },
                OverrideRedirectUri = "/admin"
            };
        };
    });

    // --- Bearer Token Authentication (for API access) ---
    builder.Services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, BearerTokenAuthHandler>("Bearer", null);

    // --- Stripe ---
    var stripeKey = builder.Configuration["Stripe:SecretKey"];
    if (!string.IsNullOrEmpty(stripeKey) && !stripeKey.StartsWith("${"))
    {
        Stripe.StripeConfiguration.ApiKey = stripeKey;
    }

    // --- Session ---
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(2);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = ".Contento.Session";
    });

    // --- Response Compression ---
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes = new[]
        {
            "text/html", "text/css", "application/javascript", "text/javascript",
            "application/json", "text/xml", "application/xml", "text/plain",
            "image/svg+xml", "application/rss+xml"
        };
    });

    // --- Antiforgery ---
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
    });

    // --- Authorization ---
    builder.Services.AddAuthorization();
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/Admin");
        options.Conventions.AllowAnonymousToPage("/Admin/Login");
        options.Conventions.AllowAnonymousToPage("/Index");
        options.Conventions.AllowAnonymousToPage("/Error");
        options.Conventions.AllowAnonymousToPage("/Register");
        options.Conventions.AllowAnonymousToPage("/ResetPassword");
    });

    var app = builder.Build();

    // --- Run Bowtie database migrations ---
    Log.Information("Running Bowtie database migrations...");
    try
    {
        var synchronizer = app.Services.GetRequiredService<DatabaseSynchronizer>();
        var assembly = typeof(Post).Assembly;

        var forceMigrations = app.Environment.IsDevelopment() ||
            Environment.GetEnvironmentVariable("FORCE_MIGRATIONS") == "true";

        await synchronizer.SynchronizeAsync(
            assemblyPath: assembly.Location,
            connectionString: connectionString,
            provider: DatabaseProvider.PostgreSQL,
            defaultSchema: "public",
            dryRun: false,
            outputFile: null,
            force: forceMigrations
        );
        Log.Information("Database migrations completed");

        // Create implicit text→jsonb cast so Tuxedo string params work with jsonb columns
        using (var castConn = dataSource.OpenConnection())
        using (var cmd = castConn.CreateCommand())
        {
            cmd.CommandText = """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_cast
                        WHERE castsource = 'text'::regtype AND casttarget = 'jsonb'::regtype
                    ) THEN
                        CREATE CAST (text AS jsonb) WITH INOUT AS IMPLICIT;
                    END IF;
                END $$;
                """;
            cmd.ExecuteNonQuery();
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration failed");
        throw;
    }

    // --- Seed data ---
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(seedDemoData: app.Environment.IsDevelopment());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database seeding failed");
        }
    }

    // --- Initialize Plugin Runtime ---
    try
    {
        using var pluginScope = app.Services.CreateScope();
        var siteService = pluginScope.ServiceProvider.GetRequiredService<ISiteService>();
        var defaultSite = await siteService.GetBySlugAsync("default");
        if (defaultSite != null)
        {
            var pluginRuntime = app.Services.GetRequiredService<IPluginRuntime>();
            await pluginRuntime.InitializeAsync(defaultSite.Id);
            Log.Information("Plugin runtime initialized for site {SiteId}", defaultSite.Id);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Plugin runtime initialization failed");
    }

    // --- Configure middleware pipeline ---
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseResponseCompression();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseTailbreeze();

    // Site resolution middleware (before routing)
    app.UseSiteResolution();

    // 301/302 redirect middleware (after site resolution, before routing)
    app.UseRedirectMiddleware();

    app.UseRouting();

    // OAuth authentication
    app.UseNoundryOAuth();

    app.UseSession();
    app.UseAntiforgery();
    app.UseAuthorization();

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        var csp = new[]
        {
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://js.stripe.com",
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net",
            "font-src 'self' https://fonts.gstatic.com data:",
            "img-src 'self' data: https:",
            "connect-src 'self'",
            "frame-src 'self' https://js.stripe.com https://www.youtube.com https://player.vimeo.com https://open.spotify.com https://platform.twitter.com https://codepen.io https://w.soundcloud.com",
            "frame-ancestors 'none'"
        };
        context.Response.Headers["Content-Security-Policy"] = string.Join("; ", csp);
        context.Response.Headers["X-Frame-Options"] = "DENY";

        if (!context.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        await next();
    });

    // OpenAPI + Scalar
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Contento CMS API";
        options.Theme = ScalarTheme.BluePlanet;
    });

    app.MapRazorPages();
    app.MapControllers();

    // Health check
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        version = "1.0.0",
        timestamp = DateTime.UtcNow
    })).AllowAnonymous();

    // Log startup
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = string.Join(", ", app.Urls);
        Log.Information("Contento CMS started successfully");
        Log.Information("Listening on: {Urls}", urls);
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
