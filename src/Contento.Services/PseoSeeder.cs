using System.Data;
using System.Text.Json;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Contento.Core.Models;
using Microsoft.Extensions.Logging;

namespace Contento.Services;

/// <summary>
/// Seeds pSEO system data: 150+ niches across 8 categories and 10 day-one content schemas
/// </summary>
public class PseoSeeder
{
    private readonly IDbConnection _db;
    private readonly ILogger<PseoSeeder> _logger;

    public PseoSeeder(IDbConnection db, ILogger<PseoSeeder> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    public async Task SeedAsync()
    {
        await SeedNichesAsync();
        await SeedSchemasAsync();
    }

    // ─── Niches ────────────────────────────────────────────────────────────

    private async Task SeedNichesAsync()
    {
        var niches = BuildNiches();
        var seeded = 0;

        foreach (var niche in niches)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM niche_taxonomies WHERE slug = @Slug",
                new { niche.Slug });
            if (exists == 0)
            {
                await _db.InsertAsync(niche);
                seeded++;
            }
        }

        _logger.LogInformation("pSEO niches seeded: {Count} new of {Total} total", seeded, niches.Count);
    }

    private static List<NicheTaxonomy> BuildNiches()
    {
        var niches = new List<NicheTaxonomy>();

        // ── Software / SaaS (20) ───────────────────────────────────────────
        niches.AddRange(BuildCategory("Software / SaaS", new (string slug, string name, object ctx)[]
        {
            ("developer-tools", "Developer Tools", new
            {
                audience = "Software engineers, DevOps practitioners, indie hackers, and CTOs evaluating tooling",
                pain_points = "Onboarding friction, documentation gaps, tool fatigue, integration complexity, vendor lock-in",
                monetization = "SaaS subscriptions, API credits, pro tiers, enterprise licensing",
                content_that_works = "Tutorials, comparisons, how-tos, changelogs, migration guides",
                subtopics = new[] { "CLI tools", "API development", "testing frameworks", "deployment pipelines", "logging", "monitoring", "code editors", "version control", "package managers", "containerization" }
            }),
            ("project-management", "Project Management", new
            {
                audience = "Product managers, team leads, and startup founders managing cross-functional teams",
                pain_points = "Scope creep, status visibility, remote collaboration, tool overload, deadline tracking",
                monetization = "Per-seat SaaS pricing, freemium tiers, enterprise contracts",
                content_that_works = "Templates, workflow guides, methodology comparisons, productivity tips, case studies",
                subtopics = new[] { "agile methodology", "kanban boards", "sprint planning", "resource allocation", "time tracking", "roadmapping", "stakeholder reporting", "risk management", "Gantt charts", "OKR tracking" }
            }),
            ("crm", "CRM", new
            {
                audience = "Sales teams, account executives, and revenue operations leaders in B2B companies",
                pain_points = "Data hygiene, pipeline visibility, adoption resistance, integration silos, lead scoring accuracy",
                monetization = "Tiered SaaS subscriptions, add-on modules, implementation consulting",
                content_that_works = "Feature comparisons, implementation guides, CRM strategy articles, ROI calculators, workflow templates",
                subtopics = new[] { "pipeline management", "contact management", "sales forecasting", "email sequences", "lead scoring", "reporting dashboards", "integrations", "mobile CRM", "deal tracking", "customer segmentation" }
            }),
            ("analytics", "Analytics", new
            {
                audience = "Data analysts, growth marketers, and product teams needing actionable insights",
                pain_points = "Data silos, attribution complexity, privacy compliance, dashboard overload, real-time needs",
                monetization = "Usage-based pricing, data volume tiers, enterprise analytics suites",
                content_that_works = "Benchmark reports, setup tutorials, metric explainers, tool comparisons, privacy guides",
                subtopics = new[] { "web analytics", "product analytics", "marketing attribution", "A/B testing", "cohort analysis", "funnel optimization", "data visualization", "event tracking", "predictive analytics", "privacy-first analytics" }
            }),
            ("email-marketing", "Email Marketing", new
            {
                audience = "Marketers, solopreneurs, and e-commerce operators building subscriber relationships",
                pain_points = "Deliverability issues, low open rates, list hygiene, automation complexity, spam filters",
                monetization = "Subscriber-tier pricing, pay-per-send, premium templates, deliverability add-ons",
                content_that_works = "Subject line guides, automation workflows, deliverability tips, template galleries, case studies",
                subtopics = new[] { "drip campaigns", "list segmentation", "A/B testing emails", "transactional emails", "email deliverability", "newsletter strategy", "email templates", "opt-in forms", "cold email outreach", "email personalization" }
            }),
            ("hr-tools", "HR Tools", new
            {
                audience = "HR managers, people ops teams, and startup founders handling hiring and culture",
                pain_points = "Manual processes, compliance complexity, onboarding friction, retention tracking, payroll errors",
                monetization = "Per-employee pricing, module-based plans, payroll processing fees",
                content_that_works = "Policy templates, compliance checklists, onboarding guides, benefits comparisons, culture playbooks",
                subtopics = new[] { "applicant tracking", "onboarding automation", "performance reviews", "payroll processing", "benefits administration", "employee engagement", "time-off management", "compliance tracking", "org charts", "offboarding" }
            }),
            ("cloud-infrastructure", "Cloud Infrastructure", new
            {
                audience = "DevOps engineers, platform teams, and CTOs building scalable cloud architectures",
                pain_points = "Cost overruns, security misconfigurations, multi-cloud complexity, scaling bottlenecks, vendor lock-in",
                monetization = "Usage-based pricing, reserved capacity, managed service tiers, support plans",
                content_that_works = "Architecture diagrams, cost optimization guides, migration playbooks, security hardening tutorials, benchmark comparisons",
                subtopics = new[] { "serverless computing", "container orchestration", "infrastructure as code", "cloud cost optimization", "multi-cloud strategy", "edge computing", "cloud security", "auto-scaling", "managed databases", "CDN configuration" }
            }),
            ("cybersecurity", "Cybersecurity", new
            {
                audience = "CISOs, security engineers, and compliance officers protecting enterprise systems",
                pain_points = "Alert fatigue, talent shortage, evolving threats, compliance burden, third-party risk",
                monetization = "Endpoint licensing, managed security services, compliance audit tools, threat intelligence feeds",
                content_that_works = "Threat briefings, compliance checklists, incident response templates, tool comparisons, vulnerability roundups",
                subtopics = new[] { "endpoint protection", "zero trust architecture", "penetration testing", "SIEM tools", "vulnerability management", "identity security", "cloud security posture", "phishing prevention", "incident response", "compliance frameworks" }
            }),
            ("data-engineering", "Data Engineering", new
            {
                audience = "Data engineers, analytics engineers, and platform teams building data pipelines",
                pain_points = "Pipeline fragility, data quality issues, schema drift, cost at scale, orchestration complexity",
                monetization = "Compute-based pricing, data volume tiers, managed pipeline services",
                content_that_works = "Architecture guides, ETL tutorials, tool comparisons, data modeling best practices, performance optimization tips",
                subtopics = new[] { "ETL pipelines", "data warehousing", "stream processing", "data quality", "data catalogs", "orchestration tools", "data lakehouse", "schema management", "batch processing", "reverse ETL" }
            }),
            ("api-development", "API Development", new
            {
                audience = "Backend developers, API product managers, and integration engineers building and consuming APIs",
                pain_points = "Versioning headaches, documentation staleness, rate limiting, authentication complexity, breaking changes",
                monetization = "API call pricing, developer portal subscriptions, enterprise SLAs",
                content_that_works = "API design guides, SDK tutorials, authentication walkthroughs, versioning strategies, GraphQL vs REST comparisons",
                subtopics = new[] { "REST API design", "GraphQL", "API gateways", "rate limiting", "API documentation", "webhook integrations", "API versioning", "SDK development", "API security", "OpenAPI specification" }
            }),
            ("devops", "DevOps", new
            {
                audience = "DevOps engineers, SREs, and platform teams automating software delivery",
                pain_points = "Deployment failures, environment drift, slow feedback loops, incident management, toolchain sprawl",
                monetization = "Platform licensing, CI/CD pipeline minutes, managed DevOps services",
                content_that_works = "Pipeline templates, runbook guides, tool comparisons, incident postmortems, automation playbooks",
                subtopics = new[] { "CI/CD pipelines", "infrastructure as code", "GitOps", "observability", "release management", "configuration management", "secrets management", "blue-green deployments", "feature flags", "chaos engineering" }
            }),
            ("qa-testing", "QA Testing", new
            {
                audience = "QA engineers, test automation leads, and engineering managers ensuring software quality",
                pain_points = "Flaky tests, slow test suites, coverage gaps, manual regression burden, environment instability",
                monetization = "Test runner licensing, cloud testing infrastructure, test management SaaS",
                content_that_works = "Framework comparisons, test strategy guides, automation tutorials, CI integration guides, best practice articles",
                subtopics = new[] { "test automation", "end-to-end testing", "unit testing", "performance testing", "API testing", "mobile testing", "visual regression testing", "test data management", "load testing", "accessibility testing" }
            }),
            ("design-tools", "Design Tools", new
            {
                audience = "UI/UX designers, product designers, and design system teams creating digital experiences",
                pain_points = "Design-to-dev handoff friction, version control for designs, collaboration overhead, prototyping speed, asset management",
                monetization = "Per-editor SaaS pricing, team plans, plugin marketplace, enterprise design systems",
                content_that_works = "Tool comparisons, workflow tutorials, design system guides, plugin roundups, shortcut cheat sheets",
                subtopics = new[] { "UI design", "prototyping", "design systems", "wireframing", "design tokens", "component libraries", "design handoff", "user research tools", "icon design", "responsive design" }
            }),
            ("video-conferencing", "Video Conferencing", new
            {
                audience = "Remote teams, hybrid workplaces, and event organizers needing reliable virtual communication",
                pain_points = "Audio quality issues, meeting fatigue, recording management, integration gaps, security concerns",
                monetization = "Per-host licensing, meeting capacity tiers, webinar add-ons, recording storage",
                content_that_works = "Platform comparisons, setup guides, meeting best practices, integration tutorials, security checklists",
                subtopics = new[] { "virtual meetings", "webinar platforms", "screen sharing", "meeting recordings", "breakout rooms", "virtual backgrounds", "meeting transcription", "hybrid meeting setups", "meeting scheduling", "live captions" }
            }),
            ("helpdesk-software", "Helpdesk Software", new
            {
                audience = "Support managers, customer success teams, and ops leaders scaling customer service",
                pain_points = "Ticket backlog, slow response times, agent burnout, knowledge base gaps, channel fragmentation",
                monetization = "Per-agent pricing, automation add-ons, omnichannel tiers, AI chatbot modules",
                content_that_works = "Platform comparisons, workflow automation guides, KPI benchmarks, canned response templates, self-service strategy articles",
                subtopics = new[] { "ticket management", "live chat", "knowledge bases", "SLA tracking", "chatbot automation", "customer satisfaction scoring", "omnichannel support", "agent productivity", "escalation workflows", "self-service portals" }
            }),
            ("accounting-software", "Accounting Software", new
            {
                audience = "Small business owners, bookkeepers, and CFOs managing financial operations",
                pain_points = "Manual data entry, reconciliation errors, tax compliance, multi-currency complexity, reporting lag",
                monetization = "Subscription tiers, payroll add-ons, multi-entity pricing, accountant partner programs",
                content_that_works = "Software comparisons, setup tutorials, tax preparation guides, chart-of-accounts templates, integration walkthroughs",
                subtopics = new[] { "invoicing", "expense tracking", "bank reconciliation", "tax reporting", "accounts payable", "accounts receivable", "financial statements", "multi-currency accounting", "payroll integration", "audit trails" }
            }),
            ("erp-systems", "ERP Systems", new
            {
                audience = "Operations leaders, IT directors, and CFOs in mid-market and enterprise organizations",
                pain_points = "Implementation complexity, change management, customization costs, data migration risk, user adoption",
                monetization = "Enterprise licensing, implementation services, module-based pricing, annual maintenance fees",
                content_that_works = "Vendor comparisons, implementation guides, ROI calculators, change management playbooks, industry-specific case studies",
                subtopics = new[] { "manufacturing ERP", "supply chain management", "inventory management", "financial management", "procurement", "warehouse management", "production planning", "ERP implementation", "ERP customization", "ERP migration" }
            }),
            ("low-code-platforms", "Low-Code Platforms", new
            {
                audience = "Citizen developers, business analysts, and IT teams building internal tools rapidly",
                pain_points = "Scalability limits, vendor lock-in, governance concerns, integration gaps, enterprise readiness",
                monetization = "Per-app pricing, user-based tiers, enterprise governance add-ons, connector marketplace",
                content_that_works = "Platform comparisons, build-along tutorials, use case showcases, governance guides, citizen developer training",
                subtopics = new[] { "visual app builders", "workflow automation", "database builders", "form builders", "internal tool platforms", "API connectors", "business logic automation", "app deployment", "governance controls", "citizen developer programs" }
            }),
            ("identity-management", "Identity Management", new
            {
                audience = "Security architects, IT admins, and compliance teams managing user access and identity",
                pain_points = "Password fatigue, provisioning delays, compliance audits, SSO integration, privilege creep",
                monetization = "Per-user licensing, MFA add-ons, governance tiers, directory sync pricing",
                content_that_works = "Implementation guides, compliance checklists, SSO tutorials, protocol comparisons, zero trust identity articles",
                subtopics = new[] { "single sign-on", "multi-factor authentication", "user provisioning", "role-based access control", "directory services", "passwordless authentication", "identity governance", "privileged access management", "OAuth/OIDC", "SCIM provisioning" }
            }),
            ("workflow-automation", "Workflow Automation", new
            {
                audience = "Operations managers, business analysts, and tech-savvy teams eliminating manual processes",
                pain_points = "Process fragmentation, error-prone handoffs, approval bottlenecks, limited visibility, scaling manual work",
                monetization = "Task-based pricing, connector tiers, enterprise orchestration plans, premium integrations",
                content_that_works = "Automation recipes, platform comparisons, ROI calculators, integration tutorials, process mapping guides",
                subtopics = new[] { "no-code automation", "business process automation", "document workflows", "approval workflows", "integration platforms", "robotic process automation", "event-driven automation", "notification workflows", "data sync automation", "conditional logic builders" }
            }),
        }));

        // ── Content Creation (18) ──────────────────────────────────────────
        niches.AddRange(BuildCategory("Content Creation", new (string slug, string name, object ctx)[]
        {
            ("blogging", "Blogging", new
            {
                audience = "Bloggers, content creators, and niche site owners building audiences through written content",
                pain_points = "Traffic plateaus, monetization struggles, content consistency, SEO competition, writer's block",
                monetization = "Display ads, affiliate marketing, sponsored posts, digital products",
                content_that_works = "SEO guides, monetization strategies, content calendars, niche selection advice, blogging tool reviews",
                subtopics = new[] { "blog SEO", "content planning", "blog monetization", "WordPress tips", "blog design", "guest posting", "blog analytics", "niche blogging", "blog writing tips", "blog promotion" }
            }),
            ("youtube", "YouTube", new
            {
                audience = "YouTubers, video creators, and brands building audiences on the world's largest video platform",
                pain_points = "Algorithm changes, subscriber growth, video production costs, burnout, copyright strikes",
                monetization = "Ad revenue, sponsorships, merchandise, channel memberships, Super Chats",
                content_that_works = "Growth strategies, thumbnail design tips, SEO tutorials, equipment guides, analytics breakdowns",
                subtopics = new[] { "YouTube SEO", "thumbnail design", "video editing", "channel growth", "YouTube analytics", "monetization strategies", "content planning", "YouTube Shorts", "live streaming", "audience retention" }
            }),
            ("podcasting", "Podcasting", new
            {
                audience = "Podcast hosts, audio creators, and media companies launching and growing shows",
                pain_points = "Discoverability, audience growth, monetization difficulty, editing time, consistency",
                monetization = "Sponsorships, premium subscriptions, live shows, merchandise, consulting",
                content_that_works = "Launch guides, equipment reviews, interview techniques, growth strategies, monetization playbooks",
                subtopics = new[] { "podcast hosting", "audio editing", "podcast marketing", "interview techniques", "podcast analytics", "show notes", "podcast equipment", "RSS feeds", "podcast monetization", "remote recording" }
            }),
            ("newsletter", "Newsletter", new
            {
                audience = "Newsletter creators, writers, and media entrepreneurs building subscriber-funded businesses",
                pain_points = "Subscriber acquisition, open rate decay, monetization, content fatigue, platform dependence",
                monetization = "Paid subscriptions, sponsorships, affiliate links, premium tiers",
                content_that_works = "Growth tactics, subject line guides, platform comparisons, pricing strategies, case studies",
                subtopics = new[] { "subscriber growth", "email platforms", "newsletter monetization", "writing cadence", "subject lines", "paid newsletters", "newsletter design", "audience segmentation", "referral programs", "newsletter analytics" }
            }),
            ("course-creation", "Course Creation", new
            {
                audience = "Educators, subject matter experts, and entrepreneurs packaging knowledge into online courses",
                pain_points = "Completion rates, pricing anxiety, platform selection, content organization, marketing",
                monetization = "Course sales, coaching upsells, membership sites, certification fees",
                content_that_works = "Platform comparisons, pricing strategies, curriculum design guides, launch playbooks, student engagement tips",
                subtopics = new[] { "course platforms", "curriculum design", "video production", "student engagement", "course pricing", "course marketing", "quizzes and assessments", "drip content", "cohort-based courses", "course completion" }
            }),
            ("social-media-management", "Social Media Management", new
            {
                audience = "Social media managers, brands, and agencies managing presence across multiple platforms",
                pain_points = "Content volume, platform algorithm changes, engagement decline, ROI measurement, team coordination",
                monetization = "Management retainers, tool subscriptions, content packages, strategy consulting",
                content_that_works = "Platform-specific guides, scheduling strategies, analytics tutorials, content calendar templates, trend reports",
                subtopics = new[] { "content scheduling", "social analytics", "community management", "social listening", "influencer outreach", "social advertising", "platform algorithms", "content repurposing", "brand voice", "crisis management" }
            }),
            ("copywriting", "Copywriting", new
            {
                audience = "Freelance copywriters, marketing teams, and entrepreneurs crafting persuasive sales copy",
                pain_points = "Client acquisition, pricing confidence, writer's block, measuring copy effectiveness, niche selection",
                monetization = "Project-based fees, retainer agreements, course sales, copy templates",
                content_that_works = "Copywriting formulas, swipe files, headline guides, client acquisition strategies, portfolio tips",
                subtopics = new[] { "sales pages", "email copy", "landing pages", "headlines", "brand messaging", "direct response", "UX copy", "ad copy", "product descriptions", "call-to-action optimization" }
            }),
            ("content-marketing", "Content Marketing", new
            {
                audience = "Content marketers, brand strategists, and growth teams driving leads through content",
                pain_points = "Content ROI measurement, production bottlenecks, distribution challenges, stakeholder buy-in, content decay",
                monetization = "Agency retainers, consulting, content strategy tools, training programs",
                content_that_works = "Strategy frameworks, distribution playbooks, ROI calculators, content audit guides, case studies",
                subtopics = new[] { "content strategy", "content distribution", "content operations", "editorial calendars", "content repurposing", "content audits", "thought leadership", "gated content", "content metrics", "brand storytelling" }
            }),
            ("video-editing", "Video Editing", new
            {
                audience = "Video editors, content creators, and production teams crafting compelling visual stories",
                pain_points = "Render times, software learning curves, storage management, client feedback loops, hardware costs",
                monetization = "Freelance editing services, preset/template sales, course creation, stock footage licensing",
                content_that_works = "Software tutorials, effect breakdowns, workflow optimization, hardware reviews, before/after showcases",
                subtopics = new[] { "color grading", "motion graphics", "sound design", "transitions", "video effects", "timeline editing", "export settings", "proxy editing", "multi-cam editing", "video compression" }
            }),
            ("graphic-design", "Graphic Design", new
            {
                audience = "Graphic designers, brand designers, and creative teams producing visual assets",
                pain_points = "Client revisions, pricing projects, creative blocks, staying current with trends, file management",
                monetization = "Design services, template sales, brand identity packages, online courses",
                content_that_works = "Design tutorials, trend reports, tool comparisons, portfolio advice, pricing guides",
                subtopics = new[] { "logo design", "brand identity", "typography", "color theory", "layout design", "print design", "packaging design", "illustration", "icon design", "design systems" }
            }),
            ("photography", "Photography", new
            {
                audience = "Professional and hobbyist photographers building portfolios and client businesses",
                pain_points = "Client acquisition, pricing, gear costs, post-processing time, market saturation",
                monetization = "Client sessions, print sales, presets, workshops, stock photography",
                content_that_works = "Gear reviews, editing tutorials, business guides, location scouting tips, portfolio critiques",
                subtopics = new[] { "portrait photography", "landscape photography", "product photography", "wedding photography", "photo editing", "lighting techniques", "camera gear", "composition", "photo business", "drone photography" }
            }),
            ("music-production", "Music Production", new
            {
                audience = "Music producers, beatmakers, and audio engineers creating and distributing music",
                pain_points = "DAW complexity, mixing quality, sample licensing, distribution confusion, monetization",
                monetization = "Beat sales, mixing/mastering services, sample packs, production courses",
                content_that_works = "DAW tutorials, mixing guides, gear reviews, sample pack showcases, music theory lessons",
                subtopics = new[] { "DAW workflows", "mixing techniques", "mastering", "sound design", "sampling", "music theory", "beat making", "vocal recording", "plugin reviews", "music distribution" }
            }),
            ("live-streaming", "Live Streaming", new
            {
                audience = "Streamers, content creators, and brands using live video to engage audiences in real time",
                pain_points = "Audience growth, stream quality, equipment setup, burnout, monetization consistency",
                monetization = "Subscriptions, donations, sponsorships, merchandise, brand deals",
                content_that_works = "Setup guides, growth strategies, monetization tips, equipment reviews, engagement tactics",
                subtopics = new[] { "streaming setup", "OBS configuration", "audience engagement", "stream overlays", "Twitch growth", "stream monetization", "multi-platform streaming", "stream alerts", "chat moderation", "streaming hardware" }
            }),
            ("technical-writing", "Technical Writing", new
            {
                audience = "Technical writers, developer advocates, and documentation teams creating clear technical content",
                pain_points = "Keeping docs current, information architecture, user testing docs, toolchain selection, style consistency",
                monetization = "Contract writing, documentation consulting, style guide creation, training",
                content_that_works = "Documentation templates, style guides, tool comparisons, information architecture guides, API documentation tutorials",
                subtopics = new[] { "API documentation", "user guides", "knowledge bases", "docs-as-code", "style guides", "information architecture", "release notes", "tutorials", "README writing", "documentation tools" }
            }),
            ("ux-writing", "UX Writing", new
            {
                audience = "UX writers, content designers, and product teams crafting in-product microcopy",
                pain_points = "Stakeholder education, measuring impact, content consistency, scaling across products, localization",
                monetization = "Full-time roles, freelance contracts, UX writing courses, content design consulting",
                content_that_works = "Microcopy examples, A/B test case studies, content guidelines, portfolio advice, UX writing exercises",
                subtopics = new[] { "microcopy", "error messages", "onboarding flows", "button labels", "empty states", "voice and tone", "content design systems", "localization", "accessibility writing", "conversational design" }
            }),
            ("community-building", "Community Building", new
            {
                audience = "Community managers, founders, and creator-educators building engaged online communities",
                pain_points = "Member engagement, moderation at scale, monetization, platform migration, measuring community health",
                monetization = "Membership fees, community-led events, sponsorships, premium tiers",
                content_that_works = "Launch playbooks, engagement strategies, platform comparisons, moderation guides, community metrics frameworks",
                subtopics = new[] { "community platforms", "member onboarding", "engagement strategies", "moderation", "community events", "community metrics", "community-led growth", "Discord communities", "community monetization", "ambassador programs" }
            }),
            ("influencer-marketing", "Influencer Marketing", new
            {
                audience = "Brand marketers, influencer managers, and agencies running creator-based campaigns",
                pain_points = "Fake followers, ROI measurement, contract negotiation, content approval, finding the right fit",
                monetization = "Agency fees, platform commissions, campaign management retainers, influencer discovery tools",
                content_that_works = "Campaign playbooks, rate card guides, platform-specific strategies, case studies, contract templates",
                subtopics = new[] { "micro-influencers", "influencer discovery", "campaign management", "influencer contracts", "performance measurement", "content guidelines", "brand partnerships", "affiliate influencer programs", "influencer platforms", "UGC campaigns" }
            }),
            ("content-strategy", "Content Strategy", new
            {
                audience = "Content strategists, heads of content, and marketing leaders aligning content with business goals",
                pain_points = "Content ROI, cross-team alignment, content governance, channel proliferation, audience fragmentation",
                monetization = "Strategy consulting, content audits, training workshops, content operations tooling",
                content_that_works = "Strategy frameworks, audit templates, governance models, content ops guides, maturity assessments",
                subtopics = new[] { "content pillars", "content governance", "content operations", "editorial strategy", "channel strategy", "content measurement", "content taxonomy", "content workflow", "content lifecycle", "audience research" }
            }),
        }));

        // ── E-Commerce (18) ────────────────────────────────────────────────
        niches.AddRange(BuildCategory("E-Commerce", new (string slug, string name, object ctx)[]
        {
            ("shopify-stores", "Shopify Stores", new
            {
                audience = "Shopify merchants, DTC brands, and e-commerce entrepreneurs building online stores",
                pain_points = "Theme customization, app bloat, conversion optimization, shipping complexity, customer retention",
                monetization = "Product sales, Shopify app revenue, theme sales, consulting services",
                content_that_works = "Store setup guides, app reviews, conversion tips, theme comparisons, case studies",
                subtopics = new[] { "Shopify themes", "Shopify apps", "checkout optimization", "Shopify SEO", "product pages", "Shopify Plus", "Shopify payments", "inventory management", "Shopify analytics", "store migration" }
            }),
            ("dropshipping", "Dropshipping", new
            {
                audience = "Aspiring e-commerce entrepreneurs and side-hustlers starting low-inventory online businesses",
                pain_points = "Supplier reliability, shipping times, thin margins, product quality, customer complaints",
                monetization = "Product margin, upsells, email marketing revenue, course sales",
                content_that_works = "Supplier guides, product research tutorials, store setup walkthroughs, niche selection, profit calculators",
                subtopics = new[] { "supplier sourcing", "product research", "AliExpress alternatives", "fulfillment automation", "dropshipping niches", "pricing strategies", "store branding", "order tracking", "returns management", "dropshipping ads" }
            }),
            ("print-on-demand", "Print-on-Demand", new
            {
                audience = "Designers, artists, and side-hustlers selling custom-designed merchandise without inventory",
                pain_points = "Design quality, mockup creation, platform fees, niche competition, profit margins",
                monetization = "Product sales, design licensing, niche store portfolios",
                content_that_works = "Design tutorials, platform comparisons, niche research guides, mockup tools, trending design roundups",
                subtopics = new[] { "t-shirt design", "POD platforms", "niche research", "mockup creation", "Merch by Amazon", "Redbubble tips", "design tools", "pricing strategy", "product selection", "trademark research" }
            }),
            ("amazon-fba", "Amazon FBA", new
            {
                audience = "Amazon sellers, private label brands, and e-commerce operators using Fulfillment by Amazon",
                pain_points = "Fee complexity, competition, listing optimization, inventory planning, account suspensions",
                monetization = "Product sales, wholesale arbitrage, private label margins, consulting",
                content_that_works = "Product research guides, listing optimization tips, PPC tutorials, sourcing strategies, fee calculators",
                subtopics = new[] { "product research", "listing optimization", "Amazon PPC", "FBA fees", "private label", "wholesale sourcing", "inventory management", "Amazon SEO", "brand registry", "product photography" }
            }),
            ("dtc-brands", "DTC Brands", new
            {
                audience = "Direct-to-consumer brand founders, e-commerce marketers, and brand strategists",
                pain_points = "Customer acquisition costs, brand differentiation, retention, supply chain, scaling profitably",
                monetization = "Product sales, subscriptions, brand licensing, wholesale partnerships",
                content_that_works = "Brand building guides, acquisition strategies, retention playbooks, case studies, supply chain optimization",
                subtopics = new[] { "brand identity", "customer acquisition", "retention strategies", "subscription models", "packaging design", "supply chain", "brand storytelling", "community building", "influencer partnerships", "unit economics" }
            }),
            ("subscription-boxes", "Subscription Boxes", new
            {
                audience = "Subscription box entrepreneurs and curated commerce operators building recurring revenue",
                pain_points = "Churn rates, curation costs, fulfillment logistics, pricing, customer acquisition",
                monetization = "Monthly subscriptions, gift subscriptions, one-time boxes, add-on sales",
                content_that_works = "Launch guides, churn reduction strategies, curation tips, fulfillment walkthroughs, pricing models",
                subtopics = new[] { "box curation", "subscription pricing", "churn reduction", "fulfillment logistics", "packaging design", "subscriber acquisition", "unboxing experience", "gift subscriptions", "niche selection", "vendor relationships" }
            }),
            ("digital-products", "Digital Products", new
            {
                audience = "Creators, educators, and entrepreneurs selling downloadable and digital goods online",
                pain_points = "Piracy, pricing, delivery automation, marketing, product idea validation",
                monetization = "Direct sales, marketplace commissions, bundles, membership access",
                content_that_works = "Product idea lists, platform comparisons, pricing strategies, launch playbooks, sales funnel guides",
                subtopics = new[] { "ebook creation", "template design", "digital downloads", "Gumroad selling", "product pricing", "sales pages", "digital delivery", "product bundles", "Notion templates", "digital product marketing" }
            }),
            ("marketplace-selling", "Marketplace Selling", new
            {
                audience = "Multi-channel sellers, resellers, and e-commerce operators selling across online marketplaces",
                pain_points = "Platform fees, listing management, price competition, review management, policy changes",
                monetization = "Product sales across platforms, arbitrage margins, bulk wholesale",
                content_that_works = "Multi-channel guides, platform comparisons, listing optimization, fee breakdowns, automation tools",
                subtopics = new[] { "Etsy selling", "eBay strategies", "Walmart marketplace", "multi-channel management", "listing optimization", "pricing algorithms", "review management", "marketplace fees", "product sourcing", "seller metrics" }
            }),
            ("wholesale-b2b", "Wholesale B2B", new
            {
                audience = "B2B wholesalers, distributors, and manufacturers selling in bulk to business buyers",
                pain_points = "Order management, payment terms, catalog management, buyer discovery, logistics coordination",
                monetization = "Wholesale margins, volume pricing, trade show leads, B2B marketplace commissions",
                content_that_works = "B2B platform guides, trade show strategies, catalog management tips, payment terms articles, buyer acquisition",
                subtopics = new[] { "B2B e-commerce platforms", "wholesale pricing", "trade shows", "bulk order management", "payment terms", "catalog management", "B2B marketing", "distributor relationships", "MOQ strategies", "B2B customer portals" }
            }),
            ("product-photography", "Product Photography", new
            {
                audience = "E-commerce sellers, brands, and photographers specializing in product imagery for online sales",
                pain_points = "Consistency across products, background removal, lighting, cost per shot, turnaround time",
                monetization = "Photography services, editing services, presets, training courses",
                content_that_works = "Lighting tutorials, DIY studio guides, editing workflows, equipment reviews, before/after showcases",
                subtopics = new[] { "lighting setups", "background removal", "lifestyle photography", "360 product views", "flat lay photography", "photo editing", "DIY product photos", "color accuracy", "model photography", "photo studio setup" }
            }),
            ("inventory-management", "Inventory Management", new
            {
                audience = "E-commerce operators, warehouse managers, and retail businesses tracking stock across channels",
                pain_points = "Overselling, stockouts, multi-warehouse complexity, dead stock, demand forecasting",
                monetization = "Software subscriptions, implementation services, integration consulting",
                content_that_works = "Software comparisons, forecasting guides, warehouse organization tips, multi-channel sync tutorials, KPI guides",
                subtopics = new[] { "demand forecasting", "multi-warehouse management", "barcode systems", "reorder points", "dead stock reduction", "inventory audits", "SKU management", "just-in-time inventory", "ABC analysis", "inventory software" }
            }),
            ("shipping-logistics", "Shipping Logistics", new
            {
                audience = "E-commerce businesses, fulfillment teams, and logistics managers optimizing order delivery",
                pain_points = "Shipping costs, delivery speed expectations, international shipping, returns processing, carrier reliability",
                monetization = "Shipping software subscriptions, fulfillment services, carrier negotiation consulting",
                content_that_works = "Carrier comparisons, cost optimization guides, international shipping guides, packaging tips, returns process templates",
                subtopics = new[] { "shipping rate negotiation", "fulfillment centers", "last-mile delivery", "international shipping", "package tracking", "shipping labels", "returns management", "freight shipping", "same-day delivery", "packaging optimization" }
            }),
            ("payment-processing", "Payment Processing", new
            {
                audience = "E-commerce merchants, SaaS companies, and marketplace operators handling online payments",
                pain_points = "Transaction fees, chargebacks, PCI compliance, multi-currency support, fraud prevention",
                monetization = "Transaction percentage, payment gateway subscriptions, fraud prevention add-ons",
                content_that_works = "Gateway comparisons, fee breakdowns, fraud prevention guides, checkout optimization tips, compliance checklists",
                subtopics = new[] { "payment gateways", "checkout optimization", "fraud prevention", "chargeback management", "multi-currency payments", "mobile payments", "buy-now-pay-later", "recurring billing", "PCI compliance", "payment analytics" }
            }),
            ("customer-reviews", "Customer Reviews", new
            {
                audience = "E-commerce brands, local businesses, and SaaS companies managing online reputation through reviews",
                pain_points = "Fake reviews, low review volume, negative review response, review platform fragmentation, review authenticity",
                monetization = "Review management software, reputation consulting, review generation services",
                content_that_works = "Review request templates, response frameworks, platform comparisons, UGC strategies, social proof guides",
                subtopics = new[] { "review generation", "review responses", "review platforms", "social proof", "user-generated content", "review widgets", "video reviews", "review moderation", "star ratings impact", "review SEO" }
            }),
            ("pricing-strategy", "Pricing Strategy", new
            {
                audience = "E-commerce operators, SaaS founders, and pricing analysts optimizing revenue through strategic pricing",
                pain_points = "Price sensitivity, competitor undercutting, margin erosion, discount dependency, price testing",
                monetization = "Pricing consulting, optimization software, training workshops",
                content_that_works = "Pricing model comparisons, A/B test case studies, psychology of pricing, calculator tools, industry benchmarks",
                subtopics = new[] { "dynamic pricing", "price anchoring", "bundle pricing", "psychological pricing", "competitive pricing", "value-based pricing", "discount strategies", "price testing", "subscription pricing", "freemium models" }
            }),
            ("ecommerce-analytics", "E-commerce Analytics", new
            {
                audience = "E-commerce managers, growth analysts, and marketing teams tracking store performance",
                pain_points = "Data fragmentation, attribution complexity, metric overload, actionability gaps, privacy regulations",
                monetization = "Analytics platform subscriptions, consulting, custom dashboard services",
                content_that_works = "KPI guides, dashboard templates, tool comparisons, attribution model explainers, benchmark reports",
                subtopics = new[] { "conversion tracking", "customer lifetime value", "cohort analysis", "shopping cart analytics", "attribution modeling", "revenue analytics", "product performance", "customer segmentation", "funnel analysis", "heatmaps" }
            }),
            ("mobile-commerce", "Mobile Commerce", new
            {
                audience = "E-commerce businesses and mobile product teams optimizing the shopping experience on smartphones",
                pain_points = "Mobile conversion rates, app vs mobile web, checkout friction, push notification fatigue, app discoverability",
                monetization = "Mobile app subscriptions, in-app purchases, mobile-optimized ad revenue",
                content_that_works = "Mobile UX guides, app vs web comparisons, checkout optimization tips, push notification strategies, mobile payment guides",
                subtopics = new[] { "mobile checkout", "progressive web apps", "mobile UX", "push notifications", "mobile payments", "app store optimization", "mobile-first design", "mobile search", "SMS commerce", "mobile loyalty programs" }
            }),
            ("cross-border-commerce", "Cross-border Commerce", new
            {
                audience = "International sellers, global brands, and e-commerce operators expanding into new geographic markets",
                pain_points = "Currency conversion, customs/duties, localization, tax compliance, cross-border payments",
                monetization = "International product sales, localization services, cross-border logistics consulting",
                content_that_works = "Market entry guides, localization checklists, tax compliance overviews, currency strategy articles, logistics partner comparisons",
                subtopics = new[] { "market localization", "international payments", "customs and duties", "currency management", "international shipping", "tax compliance", "market research", "translation services", "local payment methods", "cross-border returns" }
            }),
        }));

        // ── Professional Services (20) ─────────────────────────────────────
        niches.AddRange(BuildCategory("Professional Services", new (string slug, string name, object ctx)[]
        {
            ("consulting", "Consulting", new
            {
                audience = "Independent consultants, boutique firms, and corporate advisors selling expertise",
                pain_points = "Lead generation, pricing engagements, scope creep, utilization rates, client retention",
                monetization = "Hourly/project fees, retainers, productized consulting, advisory boards",
                content_that_works = "Frameworks, case studies, engagement templates, pricing guides, thought leadership",
                subtopics = new[] { "proposal writing", "client acquisition", "pricing models", "scope management", "consulting frameworks", "deliverable templates", "stakeholder management", "practice growth", "niche specialization", "consulting tools" }
            }),
            ("freelancing", "Freelancing", new
            {
                audience = "Freelancers, independent contractors, and solopreneurs across creative and technical fields",
                pain_points = "Income instability, client acquisition, scope creep, isolation, benefits/taxes",
                monetization = "Project fees, retainers, productized services, course sales",
                content_that_works = "Client acquisition strategies, pricing guides, contract templates, productivity tips, tax guides",
                subtopics = new[] { "client proposals", "freelance pricing", "contract templates", "invoice management", "portfolio building", "freelance platforms", "time management", "freelance taxes", "client communication", "retainer agreements" }
            }),
            ("legal-services", "Legal Services", new
            {
                audience = "Law firms, solo practitioners, and legal tech companies serving businesses and individuals",
                pain_points = "Client acquisition, billable hour pressure, document management, compliance updates, competition",
                monetization = "Hourly billing, flat-fee packages, subscription legal services, legal tech SaaS",
                content_that_works = "Legal guides, compliance checklists, template contracts, practice area explainers, client intake strategies",
                subtopics = new[] { "client intake", "legal marketing", "practice management", "document automation", "compliance tracking", "billing software", "case management", "legal research tools", "client portals", "virtual law practice" }
            }),
            ("accounting", "Accounting", new
            {
                audience = "Accounting firms, CPAs, and bookkeepers serving small to mid-size businesses",
                pain_points = "Seasonal workload, client communication, technology adoption, talent retention, fee compression",
                monetization = "Monthly retainers, tax preparation fees, advisory services, cloud accounting subscriptions",
                content_that_works = "Tax guides, software comparisons, practice management tips, client communication templates, industry specialization",
                subtopics = new[] { "tax preparation", "bookkeeping services", "advisory services", "cloud accounting", "practice management", "client onboarding", "audit services", "payroll services", "financial reporting", "accounting technology" }
            }),
            ("real-estate", "Real Estate", new
            {
                audience = "Real estate agents, brokers, and property managers serving buyers, sellers, and renters",
                pain_points = "Lead generation, market fluctuations, commission pressure, transaction complexity, client management",
                monetization = "Commission-based sales, property management fees, referral income, real estate courses",
                content_that_works = "Market reports, neighborhood guides, home buying checklists, investment analyses, staging tips",
                subtopics = new[] { "lead generation", "listing marketing", "CRM for real estate", "virtual tours", "market analysis", "open houses", "transaction management", "property valuation", "real estate photography", "buyer representation" }
            }),
            ("financial-advisory", "Financial Advisory", new
            {
                audience = "Financial advisors, wealth managers, and RIAs serving high-net-worth individuals and families",
                pain_points = "Client acquisition, compliance, fee transparency, digital transformation, succession planning",
                monetization = "AUM fees, financial planning fees, insurance commissions, advisory retainers",
                content_that_works = "Market commentary, financial planning guides, retirement calculators, compliance updates, client education",
                subtopics = new[] { "financial planning", "retirement planning", "investment management", "estate planning", "tax planning", "risk management", "client reporting", "compliance", "practice growth", "technology adoption" }
            }),
            ("executive-coaching", "Executive Coaching", new
            {
                audience = "Executive coaches, leadership consultants, and corporate training firms developing leaders",
                pain_points = "Proving ROI, client acquisition, scaling one-on-one, maintaining boundaries, niche positioning",
                monetization = "Coaching engagements, group programs, assessments, corporate contracts, speaking fees",
                content_that_works = "Leadership frameworks, assessment tools, coaching methodologies, case studies, self-assessment guides",
                subtopics = new[] { "leadership development", "coaching methodologies", "360 assessments", "executive presence", "team coaching", "career transitions", "performance coaching", "emotional intelligence", "coaching tools", "group coaching" }
            }),
            ("management-consulting", "Management Consulting", new
            {
                audience = "Strategy consultants, management firms, and advisory practices serving enterprise clients",
                pain_points = "Pipeline development, talent recruitment, project staffing, intellectual property, differentiation",
                monetization = "Project-based fees, retainers, strategy workshops, interim management",
                content_that_works = "Industry analyses, strategy frameworks, transformation guides, benchmarking reports, case studies",
                subtopics = new[] { "strategy development", "organizational design", "change management", "digital transformation", "operations improvement", "M&A advisory", "cost optimization", "market entry", "competitive analysis", "performance management" }
            }),
            ("it-consulting", "IT Consulting", new
            {
                audience = "IT consultants, MSPs, and technology advisory firms helping businesses with IT strategy",
                pain_points = "Rapid technology change, talent availability, project scope, vendor management, security concerns",
                monetization = "Project fees, managed service contracts, staff augmentation, technology assessments",
                content_that_works = "Technology assessments, vendor comparisons, migration guides, security audits, IT strategy frameworks",
                subtopics = new[] { "IT strategy", "cloud migration", "cybersecurity consulting", "vendor selection", "IT governance", "system integration", "IT budgeting", "disaster recovery", "network design", "managed IT services" }
            }),
            ("marketing-agencies", "Marketing Agencies", new
            {
                audience = "Marketing agency owners, account managers, and growth agencies serving B2B and B2C clients",
                pain_points = "Client churn, scope creep, talent retention, proving ROI, pricing pressure",
                monetization = "Monthly retainers, project fees, performance-based pricing, white-label services",
                content_that_works = "Case studies, strategy templates, client reporting guides, agency growth playbooks, service packaging",
                subtopics = new[] { "client management", "agency pricing", "service packaging", "team management", "client reporting", "agency growth", "new business development", "agency tools", "white-label services", "agency specialization" }
            }),
            ("pr-firms", "PR Firms", new
            {
                audience = "Public relations professionals, communications agencies, and brand reputation managers",
                pain_points = "Media landscape changes, measuring PR value, crisis management, journalist relationships, digital PR",
                monetization = "Monthly retainers, project-based campaigns, crisis management fees, media training",
                content_that_works = "Press release templates, media pitch guides, crisis communication plans, PR measurement frameworks, case studies",
                subtopics = new[] { "media relations", "press releases", "crisis communication", "thought leadership PR", "digital PR", "influencer relations", "media monitoring", "PR measurement", "brand reputation", "event PR" }
            }),
            ("architecture", "Architecture", new
            {
                audience = "Architecture firms, solo practitioners, and design studios creating built environments",
                pain_points = "Project timelines, client expectations, regulatory compliance, design technology, fee negotiation",
                monetization = "Design fees, project management, consulting, BIM services",
                content_that_works = "Project showcases, design process guides, software tutorials, sustainability guides, regulatory updates",
                subtopics = new[] { "residential design", "commercial architecture", "sustainable design", "BIM workflows", "project management", "client presentations", "building codes", "interior architecture", "landscape architecture", "renovation design" }
            }),
            ("engineering-services", "Engineering Services", new
            {
                audience = "Engineering firms, structural engineers, and technical consultancies serving construction and manufacturing",
                pain_points = "Project complexity, regulatory compliance, talent shortage, technology adoption, liability management",
                monetization = "Project fees, consulting retainers, inspection services, design reviews",
                content_that_works = "Technical guides, code compliance articles, software tutorials, case studies, certification prep",
                subtopics = new[] { "structural engineering", "civil engineering", "environmental engineering", "project management", "CAD software", "code compliance", "site inspections", "quality assurance", "safety engineering", "engineering calculations" }
            }),
            ("medical-practices", "Medical Practices", new
            {
                audience = "Physicians, medical group administrators, and healthcare practice managers running clinical operations",
                pain_points = "Patient acquisition, insurance complexity, EHR usability, staff burnout, regulatory compliance",
                monetization = "Patient revenue, insurance reimbursement, concierge medicine, telemedicine fees",
                content_that_works = "Practice management guides, patient engagement strategies, compliance checklists, technology reviews, marketing tips",
                subtopics = new[] { "patient acquisition", "practice management", "EHR systems", "medical billing", "patient engagement", "telemedicine", "staff management", "regulatory compliance", "online reputation", "practice growth" }
            }),
            ("dental-practices", "Dental Practices", new
            {
                audience = "Dentists, dental practice owners, and dental service organizations growing their patient base",
                pain_points = "Patient retention, insurance negotiations, treatment acceptance, staff management, marketing costs",
                monetization = "Patient fees, dental plans, cosmetic procedures, dental membership plans",
                content_that_works = "Marketing guides, patient communication templates, practice growth strategies, technology reviews, case acceptance tips",
                subtopics = new[] { "patient marketing", "treatment planning", "dental technology", "practice management", "insurance management", "cosmetic dentistry", "patient retention", "staff training", "dental SEO", "patient reviews" }
            }),
            ("veterinary-services", "Veterinary Services", new
            {
                audience = "Veterinarians, vet clinic owners, and animal hospital managers serving pet owners",
                pain_points = "Client communication, emergency management, pricing sensitivity, staffing, technology adoption",
                monetization = "Veterinary services, pet wellness plans, boarding, grooming, pharmacy",
                content_that_works = "Pet health guides, practice management tips, client communication templates, technology reviews, marketing strategies",
                subtopics = new[] { "client communication", "practice management", "veterinary technology", "pet wellness plans", "emergency care", "staff management", "veterinary marketing", "telemedicine", "pharmacy management", "client retention" }
            }),
            ("therapy-practices", "Therapy Practices", new
            {
                audience = "Therapists, counselors, and mental health practice owners building sustainable private practices",
                pain_points = "Client acquisition, insurance credentialing, burnout, EHR selection, marketing ethics",
                monetization = "Session fees, insurance reimbursement, group therapy, telehealth, workshops",
                content_that_works = "Practice building guides, insurance navigation, telehealth setup, marketing ethics, self-care strategies",
                subtopics = new[] { "private practice building", "insurance credentialing", "telehealth setup", "client acquisition", "practice management software", "ethical marketing", "group therapy", "clinical documentation", "burnout prevention", "niche specialization" }
            }),
            ("recruiting-agencies", "Recruiting Agencies", new
            {
                audience = "Recruiting firms, staffing agencies, and talent acquisition consultants placing candidates",
                pain_points = "Candidate sourcing, client acquisition, placement speed, fee negotiation, market competition",
                monetization = "Placement fees, retainer searches, RPO contracts, temp staffing margins",
                content_that_works = "Sourcing strategies, industry salary guides, interview frameworks, client pitch templates, market trend reports",
                subtopics = new[] { "candidate sourcing", "client development", "interview processes", "ATS tools", "salary benchmarking", "employer branding", "contract staffing", "executive search", "recruitment marketing", "diversity recruiting" }
            }),
            ("tax-preparation", "Tax Preparation", new
            {
                audience = "Tax professionals, enrolled agents, and tax preparation firms serving individuals and businesses",
                pain_points = "Seasonal workload, regulatory changes, client document collection, technology adoption, fee pressure",
                monetization = "Tax preparation fees, year-round advisory, tax planning retainers, software commissions",
                content_that_works = "Tax deadline guides, deduction checklists, regulatory updates, software comparisons, client communication templates",
                subtopics = new[] { "individual taxes", "business taxes", "tax planning", "tax software", "client management", "document collection", "regulatory updates", "state taxes", "tax credits", "year-round services" }
            }),
            ("insurance-brokerage", "Insurance Brokerage", new
            {
                audience = "Insurance brokers, independent agents, and insurance agencies serving personal and commercial clients",
                pain_points = "Lead generation, carrier relationships, compliance, technology adoption, client retention",
                monetization = "Commissions, renewal fees, consulting fees, agency management services",
                content_that_works = "Coverage guides, comparison tools, risk assessment checklists, industry updates, client education",
                subtopics = new[] { "commercial insurance", "personal insurance", "risk assessment", "carrier management", "client retention", "insurance technology", "compliance", "claims management", "agency growth", "digital marketing" }
            }),
        }));

        // ── Health & Wellness (18) ─────────────────────────────────────────
        niches.AddRange(BuildCategory("Health & Wellness", new (string slug, string name, object ctx)[]
        {
            ("fitness-training", "Fitness Training", new
            {
                audience = "Personal trainers, fitness coaches, and gym owners helping clients achieve physical goals",
                pain_points = "Client retention, program design, online vs in-person, pricing, liability",
                monetization = "Training sessions, online programs, group classes, supplement partnerships",
                content_that_works = "Workout plans, exercise tutorials, nutrition guides, transformation stories, equipment reviews",
                subtopics = new[] { "strength training", "HIIT workouts", "online coaching", "program design", "client management", "fitness technology", "group fitness", "nutrition planning", "injury prevention", "home workouts" }
            }),
            ("nutrition-coaching", "Nutrition Coaching", new
            {
                audience = "Nutritionists, dietitians, and health coaches guiding clients toward better eating habits",
                pain_points = "Client compliance, misinformation, scope of practice, meal planning time, certification confusion",
                monetization = "Coaching packages, meal plans, group programs, corporate wellness contracts",
                content_that_works = "Meal plans, nutrition guides, recipe collections, myth-busting articles, client success stories",
                subtopics = new[] { "meal planning", "macronutrient tracking", "sports nutrition", "weight management", "food sensitivities", "gut health", "supplement guidance", "mindful eating", "nutrition for chronic conditions", "plant-based nutrition" }
            }),
            ("mental-health", "Mental Health", new
            {
                audience = "Mental health professionals, wellness advocates, and organizations promoting psychological well-being",
                pain_points = "Stigma, access barriers, provider burnout, insurance complexity, crisis management",
                monetization = "Therapy sessions, digital mental health tools, workshops, corporate wellness programs",
                content_that_works = "Coping strategies, resource guides, awareness articles, self-help tools, provider directories",
                subtopics = new[] { "anxiety management", "depression support", "stress reduction", "mindfulness practices", "therapy types", "self-care routines", "mental health apps", "workplace mental health", "crisis resources", "mental health advocacy" }
            }),
            ("supplements", "Supplements", new
            {
                audience = "Supplement brands, health-conscious consumers, and wellness retailers in the nutraceutical space",
                pain_points = "Regulatory compliance, ingredient sourcing, market saturation, misinformation, trust building",
                monetization = "Product sales, subscription models, private labeling, affiliate programs",
                content_that_works = "Ingredient explainers, product comparisons, research roundups, dosage guides, quality certification guides",
                subtopics = new[] { "vitamins and minerals", "protein supplements", "herbal supplements", "sports nutrition", "nootropics", "probiotics", "supplement quality", "regulatory compliance", "ingredient sourcing", "supplement stacks" }
            }),
            ("telemedicine", "Telemedicine", new
            {
                audience = "Healthcare providers, telehealth platforms, and medical practices offering virtual care",
                pain_points = "Regulatory compliance, patient experience, technology adoption, reimbursement, clinical limitations",
                monetization = "Virtual visit fees, platform subscriptions, enterprise licensing, value-based care",
                content_that_works = "Implementation guides, platform comparisons, compliance updates, patient experience tips, technology reviews",
                subtopics = new[] { "virtual consultations", "telehealth platforms", "remote monitoring", "regulatory compliance", "patient engagement", "telehealth technology", "prescribing regulations", "insurance coverage", "telehealth marketing", "clinical workflows" }
            }),
            ("yoga", "Yoga", new
            {
                audience = "Yoga instructors, studio owners, and wellness brands serving practitioners of all levels",
                pain_points = "Studio overhead, online competition, teacher certification costs, client retention, seasonal attendance",
                monetization = "Class fees, memberships, online subscriptions, teacher training, retreats",
                content_that_works = "Pose guides, sequence tutorials, philosophy articles, business tips for instructors, retreat guides",
                subtopics = new[] { "yoga poses", "yoga sequences", "meditation integration", "yoga for beginners", "yoga teacher training", "online yoga classes", "yoga philosophy", "restorative yoga", "yoga business", "yoga retreats" }
            }),
            ("meditation", "Meditation", new
            {
                audience = "Meditation teachers, mindfulness app developers, and wellness practitioners teaching presence",
                pain_points = "Consistency challenges, app market saturation, monetization, beginner intimidation, measuring progress",
                monetization = "App subscriptions, guided meditation sales, retreat fees, corporate programs",
                content_that_works = "Guided meditations, technique explainers, scientific research summaries, beginner guides, app comparisons",
                subtopics = new[] { "guided meditation", "mindfulness techniques", "meditation apps", "breathwork", "body scanning", "walking meditation", "meditation for sleep", "meditation for anxiety", "meditation retreats", "meditation research" }
            }),
            ("physical-therapy", "Physical Therapy", new
            {
                audience = "Physical therapists, rehabilitation clinics, and PT practice owners treating movement disorders",
                pain_points = "Insurance reimbursement, patient compliance, documentation burden, staffing, competition",
                monetization = "Patient sessions, cash-pay programs, telehealth PT, wellness programs, continuing education",
                content_that_works = "Exercise protocols, condition guides, recovery timelines, practice management tips, technology reviews",
                subtopics = new[] { "exercise prescription", "manual therapy", "sports rehabilitation", "post-surgical recovery", "telehealth PT", "patient education", "practice management", "documentation", "billing optimization", "specialization" }
            }),
            ("weight-loss", "Weight Loss", new
            {
                audience = "Weight loss coaches, health professionals, and wellness programs helping clients lose weight sustainably",
                pain_points = "Sustainability, misinformation, emotional eating, plateaus, medical complexity",
                monetization = "Coaching programs, meal delivery, supplements, digital programs, group challenges",
                content_that_works = "Meal plans, success stories, myth debunking, exercise guides, mindset articles",
                subtopics = new[] { "calorie management", "exercise for weight loss", "behavioral change", "meal planning", "weight loss plateaus", "body composition", "intermittent fasting", "metabolism", "emotional eating", "sustainable weight loss" }
            }),
            ("sports-medicine", "Sports Medicine", new
            {
                audience = "Sports medicine physicians, athletic trainers, and sports rehab clinics treating athletes",
                pain_points = "Return-to-play decisions, injury prevention, technology integration, athlete compliance, concussion management",
                monetization = "Clinical services, team contracts, performance assessments, rehab programs",
                content_that_works = "Injury prevention guides, rehabilitation protocols, sport-specific advice, technology reviews, research summaries",
                subtopics = new[] { "injury prevention", "concussion management", "rehabilitation protocols", "performance optimization", "sports nutrition", "biomechanics", "athletic taping", "return-to-play criteria", "overuse injuries", "sports psychology" }
            }),
            ("functional-medicine", "Functional Medicine", new
            {
                audience = "Functional medicine practitioners, integrative health clinics, and root-cause-focused providers",
                pain_points = "Insurance coverage, patient education, testing costs, evidence debates, practice building",
                monetization = "Cash-pay consultations, lab testing, supplement dispensary, membership models",
                content_that_works = "Root cause explainers, lab test guides, protocol articles, patient success stories, practitioner training",
                subtopics = new[] { "root cause analysis", "gut health", "hormonal balance", "detoxification", "lab testing", "nutrition protocols", "supplements", "autoimmune conditions", "patient education", "practice building" }
            }),
            ("skin-care", "Skin Care", new
            {
                audience = "Dermatologists, estheticians, and skincare brands helping consumers achieve healthy skin",
                pain_points = "Product overwhelm, misinformation, ingredient confusion, skin type matching, treatment costs",
                monetization = "Product sales, treatment services, consultations, brand partnerships, content creation",
                content_that_works = "Ingredient explainers, routine guides, product reviews, skin type guides, myth debunking",
                subtopics = new[] { "skincare routines", "active ingredients", "acne treatment", "anti-aging", "sunscreen", "sensitive skin", "professional treatments", "clean beauty", "skincare tools", "seasonal skincare" }
            }),
            ("sleep-health", "Sleep Health", new
            {
                audience = "Sleep specialists, wellness brands, and health practitioners addressing sleep quality issues",
                pain_points = "Sleep disorder prevalence, technology disruption, inconsistent routines, medication reliance, screening gaps",
                monetization = "Sleep products, coaching programs, diagnostic services, app subscriptions",
                content_that_works = "Sleep hygiene guides, product reviews, disorder explainers, routine optimization tips, research summaries",
                subtopics = new[] { "sleep hygiene", "insomnia management", "sleep tracking", "sleep environment", "circadian rhythm", "sleep disorders", "napping", "sleep supplements", "sleep technology", "sleep and mental health" }
            }),
            ("addiction-recovery", "Addiction Recovery", new
            {
                audience = "Addiction counselors, recovery centers, and support organizations helping individuals achieve sobriety",
                pain_points = "Relapse prevention, stigma, treatment access, family involvement, aftercare continuity",
                monetization = "Treatment services, insurance billing, sober living, outpatient programs, digital recovery tools",
                content_that_works = "Recovery guides, coping strategies, family resources, treatment comparisons, success stories",
                subtopics = new[] { "relapse prevention", "treatment approaches", "family support", "sober living", "medication-assisted treatment", "group therapy", "dual diagnosis", "aftercare planning", "recovery community", "digital recovery tools" }
            }),
            ("womens-health", "Women's Health", new
            {
                audience = "Women's health practitioners, OB-GYN practices, and wellness brands serving women across life stages",
                pain_points = "Research gaps, hormonal complexity, access to care, stigma around topics, life-stage transitions",
                monetization = "Clinical services, digital health platforms, product sales, educational programs",
                content_that_works = "Condition guides, hormonal health articles, life-stage resources, provider directories, wellness tips",
                subtopics = new[] { "hormonal health", "fertility", "pregnancy wellness", "menopause", "pelvic floor health", "breast health", "reproductive health", "menstrual health", "postpartum care", "preventive screenings" }
            }),
            ("mens-health", "Men's Health", new
            {
                audience = "Men's health practitioners, wellness brands, and health platforms addressing men's unique health needs",
                pain_points = "Stigma around help-seeking, preventive care avoidance, mental health barriers, hormonal changes, fitness injuries",
                monetization = "Telehealth consultations, supplement sales, fitness programs, health screenings",
                content_that_works = "Health guides, fitness programs, mental health resources, nutrition plans, preventive care checklists",
                subtopics = new[] { "testosterone health", "prostate health", "cardiovascular fitness", "mental health", "sexual health", "hair loss", "nutrition", "preventive screenings", "stress management", "aging well" }
            }),
            ("senior-wellness", "Senior Wellness", new
            {
                audience = "Geriatric care providers, senior living communities, and wellness brands serving older adults",
                pain_points = "Mobility limitations, chronic condition management, social isolation, cognitive decline, caregiver burden",
                monetization = "Care services, wellness programs, assistive products, community memberships",
                content_that_works = "Exercise guides, nutrition for aging, cognitive health articles, safety checklists, caregiver resources",
                subtopics = new[] { "fall prevention", "cognitive health", "nutrition for seniors", "mobility exercises", "chronic disease management", "social engagement", "caregiver support", "medication management", "senior technology", "end-of-life planning" }
            }),
            ("corporate-wellness", "Corporate Wellness", new
            {
                audience = "HR leaders, corporate wellness managers, and workplace health vendors serving employee populations",
                pain_points = "Engagement rates, ROI measurement, program diversity, remote workforce, budget justification",
                monetization = "Program licensing, per-employee pricing, wellness platform subscriptions, consulting",
                content_that_works = "Program design guides, ROI calculators, engagement strategies, vendor comparisons, wellness challenge templates",
                subtopics = new[] { "employee engagement", "mental health programs", "fitness challenges", "nutrition programs", "stress management", "health screenings", "wellness technology", "remote wellness", "wellness incentives", "program measurement" }
            }),
        }));

        // ── Finance (18) ───────────────────────────────────────────────────
        niches.AddRange(BuildCategory("Finance", new (string slug, string name, object ctx)[]
        {
            ("personal-finance", "Personal Finance", new
            {
                audience = "Individuals, families, and young professionals managing money and building financial literacy",
                pain_points = "Budgeting discipline, debt burden, savings consistency, financial literacy gaps, information overload",
                monetization = "Affiliate links, sponsored content, financial product referrals, courses, coaching",
                content_that_works = "Budget templates, savings strategies, debt payoff plans, financial calculators, beginner guides",
                subtopics = new[] { "budgeting methods", "emergency funds", "saving strategies", "financial goals", "money mindset", "financial literacy", "bank account optimization", "automating finances", "financial planning tools", "money management apps" }
            }),
            ("investing", "Investing", new
            {
                audience = "Retail investors, aspiring traders, and wealth-builders learning to grow money through markets",
                pain_points = "Information asymmetry, emotional decision-making, fee awareness, portfolio construction, timing anxiety",
                monetization = "Newsletter subscriptions, brokerage referrals, course sales, advisory services",
                content_that_works = "Investment guides, market analysis, portfolio strategies, broker comparisons, educational series",
                subtopics = new[] { "stock investing", "index funds", "ETFs", "dividend investing", "growth vs value", "portfolio diversification", "investment research", "brokerage platforms", "dollar-cost averaging", "risk management" }
            }),
            ("cryptocurrency", "Cryptocurrency", new
            {
                audience = "Crypto investors, DeFi enthusiasts, and blockchain developers navigating digital asset markets",
                pain_points = "Volatility, security risks, regulatory uncertainty, information overload, scam prevalence",
                monetization = "Exchange referrals, newsletter subscriptions, trading tools, educational content",
                content_that_works = "Coin analyses, wallet guides, security tutorials, DeFi explainers, regulatory updates",
                subtopics = new[] { "Bitcoin", "Ethereum", "DeFi protocols", "crypto wallets", "blockchain basics", "crypto trading", "NFTs", "staking", "crypto security", "regulatory landscape" }
            }),
            ("insurance", "Insurance", new
            {
                audience = "Insurance seekers, policyholders, and insurance professionals navigating coverage options",
                pain_points = "Policy complexity, premium costs, coverage gaps, claims processes, comparison difficulty",
                monetization = "Insurance referrals, comparison tool commissions, educational content, lead generation",
                content_that_works = "Coverage guides, comparison articles, claims process walkthroughs, savings tips, policy explainers",
                subtopics = new[] { "health insurance", "auto insurance", "home insurance", "life insurance", "disability insurance", "umbrella insurance", "insurance comparisons", "claims process", "coverage optimization", "insurance for businesses" }
            }),
            ("fintech", "Fintech", new
            {
                audience = "Fintech founders, investors, and financial professionals tracking innovation in financial services",
                pain_points = "Regulatory navigation, user trust, market saturation, legacy system integration, funding competition",
                monetization = "SaaS subscriptions, transaction fees, API licensing, consulting, investment",
                content_that_works = "Industry analyses, startup profiles, regulatory guides, technology deep dives, trend reports",
                subtopics = new[] { "digital banking", "payment innovation", "regtech", "insurtech", "lending platforms", "open banking", "embedded finance", "blockchain finance", "financial APIs", "neobanks" }
            }),
            ("stock-trading", "Stock Trading", new
            {
                audience = "Active traders, day traders, and options traders seeking to profit from short-term market movements",
                pain_points = "Emotional trading, overtrading, risk management, platform selection, tax complexity",
                monetization = "Trading education, signal services, platform referrals, trading tools",
                content_that_works = "Trading strategies, technical analysis guides, platform reviews, risk management tips, market commentary",
                subtopics = new[] { "day trading", "swing trading", "options trading", "technical analysis", "chart patterns", "risk management", "trading platforms", "position sizing", "trading psychology", "market scanners" }
            }),
            ("real-estate-investing", "Real Estate Investing", new
            {
                audience = "Real estate investors, landlords, and aspiring property owners building wealth through real estate",
                pain_points = "Capital requirements, property management, market timing, tenant issues, financing complexity",
                monetization = "Rental income, property appreciation, course sales, coaching, syndication",
                content_that_works = "Investment analysis guides, market reports, property management tips, financing strategies, case studies",
                subtopics = new[] { "rental properties", "house flipping", "REITs", "commercial real estate", "real estate syndication", "property management", "financing strategies", "market analysis", "tax strategies", "vacation rentals" }
            }),
            ("retirement-planning", "Retirement Planning", new
            {
                audience = "Pre-retirees, retirement planners, and financial advisors helping clients prepare for retirement",
                pain_points = "Savings shortfalls, longevity risk, healthcare costs, Social Security complexity, withdrawal strategies",
                monetization = "Financial advisory fees, retirement planning tools, educational courses, product referrals",
                content_that_works = "Retirement calculators, savings guides, withdrawal strategy articles, Social Security guides, lifestyle planning",
                subtopics = new[] { "401(k) optimization", "IRA strategies", "Social Security", "retirement income", "healthcare in retirement", "early retirement", "pension planning", "retirement budgeting", "estate planning", "retirement lifestyle" }
            }),
            ("debt-management", "Debt Management", new
            {
                audience = "Individuals in debt, credit counselors, and financial coaches helping people become debt-free",
                pain_points = "Overwhelm, interest burden, collection harassment, credit score impact, emotional stress",
                monetization = "Debt counseling fees, consolidation referrals, course sales, budgeting tool affiliates",
                content_that_works = "Payoff calculators, strategy comparisons, negotiation scripts, success stories, budgeting templates",
                subtopics = new[] { "debt snowball vs avalanche", "debt consolidation", "credit card debt", "student loan debt", "debt negotiation", "bankruptcy guidance", "debt-free living", "interest reduction", "collection defense", "debt management plans" }
            }),
            ("credit-building", "Credit Building", new
            {
                audience = "Credit-builders, young adults, and individuals recovering from poor credit seeking to improve scores",
                pain_points = "Score confusion, limited history, errors on reports, predatory products, patience required",
                monetization = "Credit card referrals, credit monitoring affiliates, course sales, coaching",
                content_that_works = "Score improvement guides, credit card comparisons, dispute letter templates, credit-building strategies, myth debunking",
                subtopics = new[] { "credit score factors", "credit reports", "secured credit cards", "credit disputes", "credit utilization", "authorized user strategies", "credit monitoring", "credit freezes", "credit-builder loans", "credit myths" }
            }),
            ("tax-planning", "Tax Planning", new
            {
                audience = "High-income earners, business owners, and tax professionals optimizing tax positions",
                pain_points = "Tax code complexity, changing regulations, missed deductions, estimated tax payments, audit risk",
                monetization = "Tax planning services, software referrals, educational content, consulting",
                content_that_works = "Tax strategy guides, deduction checklists, entity structure comparisons, year-end planning tips, regulatory updates",
                subtopics = new[] { "tax deductions", "business entity selection", "estimated taxes", "tax-loss harvesting", "retirement account strategies", "capital gains planning", "estate tax planning", "state tax optimization", "tax credits", "audit preparation" }
            }),
            ("wealth-management", "Wealth Management", new
            {
                audience = "High-net-worth individuals, family offices, and wealth advisors managing complex financial portfolios",
                pain_points = "Tax efficiency, estate complexity, multi-generational planning, alternative investments, privacy",
                monetization = "AUM-based fees, family office services, alternative investment access, estate planning",
                content_that_works = "Investment strategies, tax optimization guides, estate planning articles, family governance, philanthropy guides",
                subtopics = new[] { "portfolio management", "estate planning", "tax optimization", "alternative investments", "family governance", "philanthropic planning", "risk management", "trust structures", "wealth transfer", "family office services" }
            }),
            ("banking", "Banking", new
            {
                audience = "Consumers, small business owners, and financial professionals comparing banking products and services",
                pain_points = "Fee transparency, low interest rates, poor customer service, digital experience gaps, account complexity",
                monetization = "Account referrals, comparison tool commissions, sponsored reviews, educational content",
                content_that_works = "Account comparisons, fee analyses, savings optimization guides, digital banking reviews, business banking guides",
                subtopics = new[] { "high-yield savings", "checking accounts", "business banking", "online banks", "credit unions", "banking fees", "mobile banking", "international banking", "banking security", "account bonuses" }
            }),
            ("budgeting", "Budgeting", new
            {
                audience = "Individuals, couples, and families creating and maintaining budgets to control spending",
                pain_points = "Consistency, tracking difficulty, partner alignment, unexpected expenses, lifestyle inflation",
                monetization = "App referrals, spreadsheet sales, coaching, course creation, affiliate partnerships",
                content_that_works = "Budget templates, app comparisons, method explainers, challenge ideas, savings strategies",
                subtopics = new[] { "budget methods", "budgeting apps", "expense tracking", "envelope budgeting", "zero-based budgeting", "family budgeting", "budget templates", "irregular expenses", "budget reviews", "spending triggers" }
            }),
            ("student-loans", "Student Loans", new
            {
                audience = "Student loan borrowers, recent graduates, and parents navigating education financing and repayment",
                pain_points = "Repayment complexity, interest burden, forgiveness program confusion, refinancing decisions, income-driven plans",
                monetization = "Refinancing referrals, repayment tool affiliates, counseling services, educational content",
                content_that_works = "Repayment strategy guides, forgiveness program explainers, refinancing comparisons, calculators, policy updates",
                subtopics = new[] { "repayment strategies", "loan forgiveness", "refinancing", "income-driven repayment", "federal vs private loans", "loan consolidation", "PSLF program", "student loan interest", "deferment and forbearance", "student loan legislation" }
            }),
            ("small-business-finance", "Small Business Finance", new
            {
                audience = "Small business owners, startup founders, and entrepreneurs managing business finances",
                pain_points = "Cash flow management, funding access, tax complexity, bookkeeping, financial planning",
                monetization = "Financial tool referrals, consulting, course sales, accounting service partnerships",
                content_that_works = "Cash flow guides, funding option comparisons, tax strategy articles, bookkeeping tutorials, financial planning templates",
                subtopics = new[] { "cash flow management", "business loans", "business credit", "invoicing", "payroll management", "tax obligations", "financial statements", "funding options", "profit margins", "financial forecasting" }
            }),
            ("esg-investing", "ESG Investing", new
            {
                audience = "Socially conscious investors, ESG fund managers, and sustainability-focused financial professionals",
                pain_points = "Greenwashing, measurement inconsistency, performance concerns, data quality, regulatory evolution",
                monetization = "Fund referrals, ESG ratings tools, advisory services, research subscriptions",
                content_that_works = "ESG fund comparisons, impact measurement guides, regulatory updates, greenwashing spotlights, industry analyses",
                subtopics = new[] { "ESG ratings", "sustainable funds", "impact investing", "green bonds", "social investing", "governance metrics", "ESG reporting", "climate investing", "DEI investing", "ESG regulation" }
            }),
            ("alternative-investments", "Alternative Investments", new
            {
                audience = "Accredited investors, family offices, and advisors diversifying beyond traditional stocks and bonds",
                pain_points = "Illiquidity, due diligence complexity, high minimums, fee structures, access barriers",
                monetization = "Platform referrals, advisory fees, educational content, fund placement",
                content_that_works = "Asset class explainers, platform comparisons, due diligence guides, performance analyses, regulatory updates",
                subtopics = new[] { "private equity", "hedge funds", "venture capital", "real assets", "collectibles", "art investing", "wine investing", "farmland", "private credit", "crowdfunding platforms" }
            }),
        }));

        // ── Education (18) ─────────────────────────────────────────────────
        niches.AddRange(BuildCategory("Education", new (string slug, string name, object ctx)[]
        {
            ("online-learning", "Online Learning", new
            {
                audience = "Online learners, educators, and edtech platforms delivering education through digital channels",
                pain_points = "Completion rates, engagement, credential recognition, technology barriers, content quality",
                monetization = "Course sales, platform subscriptions, certificates, enterprise training, tutoring",
                content_that_works = "Platform comparisons, learning path guides, study tips, credential analyses, technology reviews",
                subtopics = new[] { "learning platforms", "MOOC courses", "microlearning", "video-based learning", "self-paced courses", "learning management systems", "digital credentials", "learning communities", "mobile learning", "adaptive learning" }
            }),
            ("tutoring", "Tutoring", new
            {
                audience = "Tutors, tutoring companies, and parents seeking academic support for students",
                pain_points = "Client acquisition, scheduling, pricing, student engagement, online vs in-person",
                monetization = "Hourly rates, package deals, group tutoring, online tutoring platforms",
                content_that_works = "Study techniques, subject guides, tutor marketing tips, platform comparisons, parent resources",
                subtopics = new[] { "math tutoring", "reading tutoring", "online tutoring platforms", "group tutoring", "tutor marketing", "homework help", "study skills", "tutoring business", "special needs tutoring", "SAT/ACT tutoring" }
            }),
            ("edtech", "EdTech", new
            {
                audience = "EdTech founders, education investors, and school administrators adopting educational technology",
                pain_points = "School procurement, teacher adoption, ROI measurement, student data privacy, integration complexity",
                monetization = "School/district licensing, freemium models, API access, sponsored content, consulting",
                content_that_works = "Product reviews, implementation guides, ROI analyses, privacy compliance articles, trend reports",
                subtopics = new[] { "classroom technology", "learning analytics", "student engagement tools", "assessment platforms", "virtual classrooms", "AI in education", "gamification", "digital textbooks", "educational data", "teacher tools" }
            }),
            ("certification-prep", "Certification Prep", new
            {
                audience = "Professionals pursuing industry certifications and career advancement through credentials",
                pain_points = "Study time management, exam anxiety, material quality, cost, recertification requirements",
                monetization = "Study materials, practice exams, bootcamps, coaching, study group facilitation",
                content_that_works = "Study guides, practice exams, exam tips, certification comparisons, career impact analyses",
                subtopics = new[] { "IT certifications", "project management certs", "cloud certifications", "security certifications", "professional certifications", "study planning", "practice exams", "exam strategies", "continuing education", "certification ROI" }
            }),
            ("language-learning", "Language Learning", new
            {
                audience = "Language learners, polyglots, and language educators across self-study and classroom settings",
                pain_points = "Consistency, speaking practice, plateau frustration, method confusion, motivation",
                monetization = "App subscriptions, tutoring sessions, course sales, textbook affiliates, immersion programs",
                content_that_works = "Method comparisons, vocabulary guides, grammar explanations, cultural context articles, app reviews",
                subtopics = new[] { "language apps", "immersion methods", "vocabulary building", "grammar guides", "speaking practice", "language exchange", "translation tools", "language certifications", "bilingual education", "language learning for kids" }
            }),
            ("stem-education", "STEM Education", new
            {
                audience = "STEM educators, parents, and organizations promoting science, technology, engineering, and math learning",
                pain_points = "Engagement challenges, resource access, equity gaps, teacher preparation, curriculum design",
                monetization = "Curriculum sales, workshop fees, educational kits, platform subscriptions, grants",
                content_that_works = "Project ideas, curriculum guides, resource roundups, career path articles, equity-focused strategies",
                subtopics = new[] { "coding for kids", "robotics education", "science experiments", "math enrichment", "engineering challenges", "STEM careers", "STEM equity", "hands-on learning", "STEM competitions", "teacher resources" }
            }),
            ("test-preparation", "Test Preparation", new
            {
                audience = "Students, parents, and test prep companies preparing for standardized exams and entrance tests",
                pain_points = "Test anxiety, score plateaus, study discipline, material overload, timing strategies",
                monetization = "Prep courses, practice tests, tutoring, study materials, app subscriptions",
                content_that_works = "Study schedules, practice questions, strategy guides, score improvement stories, test format explainers",
                subtopics = new[] { "SAT prep", "ACT prep", "GRE prep", "GMAT prep", "LSAT prep", "MCAT prep", "test-taking strategies", "study schedules", "practice tests", "score analysis" }
            }),
            ("college-admissions", "College Admissions", new
            {
                audience = "High school students, parents, and college counselors navigating the university application process",
                pain_points = "Application complexity, essay anxiety, financial aid confusion, school selection, timeline stress",
                monetization = "Counseling services, essay review, application courses, college match tools",
                content_that_works = "Application guides, essay examples, financial aid explainers, school profiles, timeline checklists",
                subtopics = new[] { "application essays", "college selection", "financial aid", "scholarships", "recommendation letters", "extracurricular strategy", "interview preparation", "application timeline", "early decision", "transfer admissions" }
            }),
            ("homeschooling", "Homeschooling", new
            {
                audience = "Homeschooling parents, family educators, and curriculum providers serving home-educated students",
                pain_points = "Curriculum selection, socialization concerns, time management, regulatory compliance, teaching confidence",
                monetization = "Curriculum sales, co-op fees, tutoring, resource subscriptions, workshop fees",
                content_that_works = "Curriculum reviews, scheduling guides, socialization strategies, state law guides, teaching tips",
                subtopics = new[] { "curriculum selection", "homeschool methods", "socialization", "state regulations", "homeschool co-ops", "record keeping", "homeschool scheduling", "special needs homeschooling", "high school homeschooling", "homeschool resources" }
            }),
            ("corporate-training", "Corporate Training", new
            {
                audience = "L&D professionals, training companies, and HR teams developing employee skills programs",
                pain_points = "Engagement, ROI measurement, content relevance, technology adoption, scaling training",
                monetization = "Training contracts, LMS licensing, course creation, consulting, certification programs",
                content_that_works = "Program design guides, ROI frameworks, technology reviews, engagement strategies, industry benchmarks",
                subtopics = new[] { "onboarding programs", "leadership training", "compliance training", "skills assessments", "e-learning development", "training ROI", "blended learning", "microlearning", "training technology", "instructor-led training" }
            }),
            ("professional-development", "Professional Development", new
            {
                audience = "Career-focused professionals, managers, and organizations investing in employee growth and advancement",
                pain_points = "Time for learning, relevance, career path clarity, skill gap identification, credential value",
                monetization = "Workshops, coaching, certification programs, membership communities, course sales",
                content_that_works = "Skill development guides, career path articles, learning resource roundups, networking strategies, goal-setting frameworks",
                subtopics = new[] { "skill development", "career advancement", "networking", "mentorship", "leadership skills", "public speaking", "time management", "executive education", "industry conferences", "professional reading" }
            }),
            ("coding-bootcamps", "Coding Bootcamps", new
            {
                audience = "Career changers, aspiring developers, and bootcamp operators offering intensive coding education",
                pain_points = "Bootcamp selection, cost justification, job placement rates, curriculum quality, learning pace",
                monetization = "Tuition, income share agreements, corporate training, prep courses, career services",
                content_that_works = "Bootcamp comparisons, student reviews, curriculum breakdowns, job placement data, learning path guides",
                subtopics = new[] { "bootcamp selection", "web development bootcamps", "data science bootcamps", "part-time bootcamps", "online bootcamps", "bootcamp financing", "job placement", "portfolio building", "interview prep", "bootcamp alternatives" }
            }),
            ("special-education", "Special Education", new
            {
                audience = "Special education teachers, parents of children with disabilities, and inclusion advocates",
                pain_points = "IEP navigation, resource scarcity, inclusion challenges, behavioral management, parent communication",
                monetization = "Resource sales, consulting, training workshops, assistive technology, tutoring",
                content_that_works = "IEP guides, strategy articles, resource roundups, assistive technology reviews, advocacy guides",
                subtopics = new[] { "IEP development", "behavior management", "assistive technology", "inclusive education", "learning disabilities", "autism support", "speech therapy", "occupational therapy", "transition planning", "parent advocacy" }
            }),
            ("early-childhood", "Early Childhood", new
            {
                audience = "Early childhood educators, daycare operators, and parents of young children (0-8 years)",
                pain_points = "Developmental milestones, behavior management, parent communication, regulatory compliance, curriculum design",
                monetization = "Program fees, curriculum sales, training workshops, educational materials, consulting",
                content_that_works = "Activity ideas, developmental guides, classroom management tips, parent resources, curriculum frameworks",
                subtopics = new[] { "developmental milestones", "play-based learning", "classroom management", "literacy development", "STEM for young children", "social-emotional learning", "parent engagement", "assessment methods", "outdoor education", "creative arts" }
            }),
            ("higher-education", "Higher Education", new
            {
                audience = "University administrators, faculty, and higher ed professionals managing academic institutions",
                pain_points = "Enrollment declines, funding pressures, student retention, technology adoption, institutional relevance",
                monetization = "Tuition, research grants, online program revenue, consulting, continuing education",
                content_that_works = "Enrollment strategies, retention analyses, technology reviews, policy discussions, best practice guides",
                subtopics = new[] { "enrollment management", "student retention", "online programs", "academic technology", "faculty development", "student success", "institutional research", "accreditation", "campus operations", "student engagement" }
            }),
            ("trade-schools", "Trade Schools", new
            {
                audience = "Trade school administrators, prospective students, and workforce development organizations",
                pain_points = "Enrollment marketing, employer partnerships, curriculum currency, student financing, job placement",
                monetization = "Tuition, employer partnerships, certification programs, continuing education, equipment sales",
                content_that_works = "Trade comparisons, career outcome data, program spotlights, financial aid guides, industry trend articles",
                subtopics = new[] { "skilled trades", "apprenticeships", "trade certifications", "welding programs", "electrician training", "HVAC training", "plumbing courses", "automotive training", "construction management", "career placement" }
            }),
            ("study-abroad", "Study Abroad", new
            {
                audience = "Students considering international study, study abroad providers, and university international offices",
                pain_points = "Cost concerns, safety worries, credit transfer, culture shock, program selection",
                monetization = "Program fees, travel insurance referrals, housing partnerships, language courses, advisory services",
                content_that_works = "Destination guides, packing lists, budget planners, culture adjustment tips, program comparisons",
                subtopics = new[] { "program selection", "study abroad destinations", "funding and scholarships", "visa requirements", "cultural adjustment", "housing abroad", "safety tips", "credit transfer", "language preparation", "internships abroad" }
            }),
            ("educational-games", "Educational Games", new
            {
                audience = "Game developers, educators, and parents using game-based learning to engage students",
                pain_points = "Engagement vs learning balance, age appropriateness, screen time concerns, assessment integration, cost",
                monetization = "Game sales, subscriptions, in-app purchases, licensing to schools, advertising",
                content_that_works = "Game reviews, age-appropriate guides, learning outcome analyses, development tutorials, gamification strategies",
                subtopics = new[] { "math games", "reading games", "science games", "coding games", "puzzle games", "gamification", "game-based learning", "educational apps", "board games for learning", "game design for education" }
            }),
        }));

        // ── Local Business (20) ────────────────────────────────────────────
        niches.AddRange(BuildCategory("Local Business", new (string slug, string name, object ctx)[]
        {
            ("restaurants", "Restaurants", new
            {
                audience = "Restaurant owners, chefs, and hospitality managers running dining establishments",
                pain_points = "Labor costs, food waste, online reviews, delivery margins, seasonal fluctuations",
                monetization = "Dine-in revenue, delivery, catering, private events, merchandise",
                content_that_works = "Menu engineering tips, marketing strategies, operations guides, technology reviews, staff management",
                subtopics = new[] { "menu engineering", "restaurant marketing", "staff management", "food cost control", "online ordering", "restaurant technology", "customer experience", "health inspections", "restaurant design", "catering operations" }
            }),
            ("salons", "Salons", new
            {
                audience = "Salon owners, hairstylists, and beauty professionals running hair and beauty businesses",
                pain_points = "Client retention, no-shows, pricing, product sales, hiring stylists",
                monetization = "Service fees, product retail, memberships, gift cards, training classes",
                content_that_works = "Marketing strategies, client retention tips, pricing guides, social media strategies, business management",
                subtopics = new[] { "client retention", "salon marketing", "booking systems", "salon pricing", "product recommendations", "social media for salons", "staff recruitment", "salon design", "retail sales", "customer loyalty" }
            }),
            ("home-services", "Home Services", new
            {
                audience = "Home service providers, handymen, and general contractors serving residential customers",
                pain_points = "Lead generation, seasonal demand, pricing transparency, customer trust, scheduling",
                monetization = "Service fees, maintenance contracts, referral programs, upselling additional services",
                content_that_works = "Service guides, pricing estimators, DIY vs pro comparisons, marketing tips, customer trust strategies",
                subtopics = new[] { "lead generation", "pricing strategies", "customer reviews", "scheduling software", "service areas", "seasonal marketing", "insurance requirements", "licensing", "fleet management", "customer communication" }
            }),
            ("retail-stores", "Retail Stores", new
            {
                audience = "Retail shop owners, boutique operators, and brick-and-mortar merchants competing in local markets",
                pain_points = "Foot traffic decline, online competition, inventory management, staffing, visual merchandising",
                monetization = "Product sales, loyalty programs, online store expansion, events, consignment",
                content_that_works = "Visual merchandising tips, local marketing strategies, inventory guides, customer experience articles, POS comparisons",
                subtopics = new[] { "visual merchandising", "local SEO", "POS systems", "inventory management", "customer loyalty", "store layout", "retail marketing", "seasonal displays", "staff training", "omnichannel retail" }
            }),
            ("auto-repair", "Auto Repair", new
            {
                audience = "Auto repair shop owners, mechanics, and automotive service managers serving vehicle owners",
                pain_points = "Customer trust, technician shortage, part sourcing, digital marketing, pricing transparency",
                monetization = "Repair services, maintenance packages, parts markup, fleet accounts, inspections",
                content_that_works = "Maintenance guides, pricing transparency articles, marketing strategies, shop management tips, technology reviews",
                subtopics = new[] { "shop marketing", "technician recruitment", "diagnostic equipment", "customer trust", "pricing transparency", "fleet services", "shop management software", "parts sourcing", "warranty services", "digital inspections" }
            }),
            ("landscaping", "Landscaping", new
            {
                audience = "Landscapers, lawn care companies, and outdoor living professionals serving residential and commercial clients",
                pain_points = "Seasonal revenue, crew management, equipment costs, client acquisition, weather dependence",
                monetization = "Service contracts, maintenance plans, design fees, hardscape projects, seasonal cleanups",
                content_that_works = "Design inspiration, seasonal care guides, equipment reviews, business growth tips, pricing strategies",
                subtopics = new[] { "lawn care", "landscape design", "irrigation systems", "hardscaping", "tree services", "seasonal maintenance", "commercial landscaping", "equipment management", "crew management", "snow removal" }
            }),
            ("cleaning-services", "Cleaning Services", new
            {
                audience = "Cleaning company owners, janitorial services, and residential cleaning professionals",
                pain_points = "Employee turnover, pricing pressure, scheduling complexity, supply costs, quality consistency",
                monetization = "Recurring cleaning contracts, deep cleaning, move-in/out cleaning, commercial contracts",
                content_that_works = "Business startup guides, pricing strategies, marketing tips, employee management, quality checklists",
                subtopics = new[] { "residential cleaning", "commercial cleaning", "employee management", "pricing models", "cleaning supplies", "scheduling systems", "quality control", "marketing strategies", "insurance needs", "green cleaning" }
            }),
            ("plumbing", "Plumbing", new
            {
                audience = "Plumbing company owners, master plumbers, and plumbing contractors serving residential and commercial clients",
                pain_points = "Emergency calls, pricing disputes, apprentice training, marketing, seasonal demand shifts",
                monetization = "Service calls, maintenance agreements, remodeling projects, commercial contracts, water heater sales",
                content_that_works = "DIY guides, pricing transparency articles, business tips, marketing strategies, technology reviews",
                subtopics = new[] { "emergency plumbing", "drain cleaning", "water heater services", "remodeling plumbing", "commercial plumbing", "plumbing marketing", "apprentice training", "plumbing technology", "customer communication", "pricing strategies" }
            }),
            ("electrical", "Electrical", new
            {
                audience = "Electricians, electrical contractors, and electrical service companies serving homes and businesses",
                pain_points = "Safety liability, code compliance, customer education, pricing, competition from unlicensed",
                monetization = "Service calls, panel upgrades, EV charger installation, commercial contracts, maintenance plans",
                content_that_works = "Safety guides, code update articles, marketing strategies, business growth tips, technology trends",
                subtopics = new[] { "residential electrical", "commercial electrical", "EV charger installation", "panel upgrades", "electrical safety", "code compliance", "smart home wiring", "generator installation", "lighting design", "electrical marketing" }
            }),
            ("hvac", "HVAC", new
            {
                audience = "HVAC company owners, technicians, and heating/cooling contractors serving residential and commercial markets",
                pain_points = "Seasonal demand swings, technician shortage, equipment costs, energy efficiency regulations, customer trust",
                monetization = "Installation, maintenance contracts, emergency repairs, equipment sales, indoor air quality",
                content_that_works = "Maintenance tips, energy efficiency guides, equipment comparisons, business management, marketing strategies",
                subtopics = new[] { "heating systems", "cooling systems", "maintenance plans", "energy efficiency", "indoor air quality", "ductwork", "HVAC marketing", "technician training", "smart thermostats", "commercial HVAC" }
            }),
            ("roofing", "Roofing", new
            {
                audience = "Roofing contractors, roofing company owners, and storm restoration specialists",
                pain_points = "Weather dependence, insurance claims, lead generation, safety, material costs",
                monetization = "Roof installations, repairs, inspections, insurance restoration, commercial contracts",
                content_that_works = "Material comparisons, maintenance guides, insurance claim tips, marketing strategies, safety guides",
                subtopics = new[] { "roof replacement", "roof repair", "storm damage", "roofing materials", "insurance claims", "roof inspections", "commercial roofing", "roofing marketing", "safety procedures", "gutter services" }
            }),
            ("pest-control", "Pest Control", new
            {
                audience = "Pest control company owners, exterminators, and pest management professionals",
                pain_points = "Seasonal demand, customer retention, chemical regulations, reputation management, technician training",
                monetization = "Service plans, one-time treatments, commercial contracts, wildlife removal, termite bonds",
                content_that_works = "Pest identification guides, prevention tips, treatment comparisons, business growth strategies, marketing guides",
                subtopics = new[] { "residential pest control", "commercial pest control", "termite treatment", "wildlife removal", "bed bug treatment", "pest prevention", "green pest control", "pest control marketing", "regulatory compliance", "seasonal pests" }
            }),
            ("moving-companies", "Moving Companies", new
            {
                audience = "Moving company owners, relocation specialists, and logistics professionals serving residential and commercial moves",
                pain_points = "Seasonal demand, damage claims, pricing competition, crew reliability, online reviews",
                monetization = "Moving services, packing services, storage rentals, specialty moves, corporate relocation",
                content_that_works = "Moving checklists, pricing guides, packing tips, business management, marketing strategies",
                subtopics = new[] { "residential moving", "commercial moving", "long-distance moving", "packing services", "storage solutions", "specialty moves", "moving estimates", "crew management", "moving marketing", "insurance and liability" }
            }),
            ("pet-services", "Pet Services", new
            {
                audience = "Pet service business owners including groomers, sitters, walkers, and pet daycare operators",
                pain_points = "Scheduling, seasonal fluctuations, pet safety, pricing, customer trust",
                monetization = "Grooming, boarding, daycare, walking, training, pet sitting, retail products",
                content_that_works = "Pet care guides, business startup tips, marketing strategies, pricing guides, safety protocols",
                subtopics = new[] { "pet grooming", "pet boarding", "dog walking", "pet daycare", "pet training", "pet sitting", "pet photography", "pet retail", "pet health", "pet business management" }
            }),
            ("photography-studios", "Photography Studios", new
            {
                audience = "Studio photography business owners, portrait photographers, and creative studio operators",
                pain_points = "Booking consistency, pricing, equipment costs, client management, studio overhead",
                monetization = "Session fees, print sales, mini sessions, commercial shoots, studio rentals",
                content_that_works = "Marketing strategies, pricing guides, portfolio building, studio setup tips, client experience articles",
                subtopics = new[] { "portrait photography", "studio lighting", "client management", "pricing packages", "studio marketing", "photo editing workflow", "studio design", "mini sessions", "commercial photography", "studio equipment" }
            }),
            ("fitness-studios", "Fitness Studios", new
            {
                audience = "Boutique fitness studio owners, group fitness instructors, and gym operators serving local communities",
                pain_points = "Member retention, class scheduling, instructor turnover, pricing, competition from apps",
                monetization = "Memberships, class packs, personal training, retail, corporate wellness",
                content_that_works = "Retention strategies, marketing guides, class programming tips, pricing models, technology reviews",
                subtopics = new[] { "member retention", "class scheduling", "studio marketing", "instructor management", "pricing strategies", "studio technology", "group fitness programming", "studio design", "community building", "hybrid classes" }
            }),
            ("daycare-centers", "Daycare Centers", new
            {
                audience = "Daycare owners, childcare center directors, and early learning program administrators",
                pain_points = "Licensing compliance, staff turnover, parent communication, enrollment, safety protocols",
                monetization = "Tuition fees, extended care, summer programs, enrichment classes, drop-in care",
                content_that_works = "Compliance guides, parent communication tips, enrollment strategies, staff management, curriculum ideas",
                subtopics = new[] { "licensing requirements", "staff management", "parent communication", "enrollment marketing", "safety protocols", "curriculum development", "facility design", "technology in childcare", "nutrition programs", "financial management" }
            }),
            ("event-venues", "Event Venues", new
            {
                audience = "Event venue owners, wedding venue operators, and conference center managers hosting events",
                pain_points = "Seasonal booking, venue marketing, vendor coordination, pricing, client expectations",
                monetization = "Venue rental, catering, event coordination, vendor referrals, corporate bookings",
                content_that_works = "Venue marketing guides, event planning checklists, pricing strategies, vendor management, virtual tours",
                subtopics = new[] { "wedding venues", "corporate event spaces", "venue marketing", "event coordination", "vendor partnerships", "venue pricing", "venue design", "booking management", "event technology", "seasonal events" }
            }),
            ("florists", "Florists", new
            {
                audience = "Floral shop owners, event florists, and flower delivery businesses serving local customers",
                pain_points = "Perishability, seasonal demand, wire service dependency, online competition, pricing",
                monetization = "Arrangements, event floristry, subscriptions, delivery, workshops, weddings",
                content_that_works = "Arrangement guides, business tips, marketing strategies, seasonal trend reports, wedding floral planning",
                subtopics = new[] { "wedding floristry", "event arrangements", "delivery logistics", "flower sourcing", "floral design", "shop marketing", "seasonal flowers", "subscription services", "floral workshops", "sustainable floristry" }
            }),
            ("bakeries", "Bakeries", new
            {
                audience = "Bakery owners, pastry chefs, and home bakers turning passion into commercial baking businesses",
                pain_points = "Production scaling, ingredient costs, early hours, custom order management, food regulations",
                monetization = "Retail sales, custom orders, wholesale, catering, classes, online ordering",
                content_that_works = "Recipe development tips, business management guides, marketing strategies, production scaling, food safety",
                subtopics = new[] { "custom cakes", "bread baking", "pastry production", "bakery marketing", "food safety", "production scaling", "online ordering", "wholesale baking", "bakery design", "specialty diets" }
            }),
        }));

        return niches;
    }

    private static List<NicheTaxonomy> BuildCategory(string category, (string slug, string name, object ctx)[] entries)
    {
        var list = new List<NicheTaxonomy>();
        foreach (var (slug, name, ctx) in entries)
        {
            list.Add(new NicheTaxonomy
            {
                Slug = slug,
                Name = name,
                Category = category,
                Context = JsonSerializer.Serialize(ctx),
                IsSystem = true
            });
        }
        return list;
    }

    // ─── Content Schemas ───────────────────────────────────────────────────

    private async Task SeedSchemasAsync()
    {
        var schemas = BuildSchemas();
        var seeded = 0;

        foreach (var schema in schemas)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM content_schemas WHERE slug = @Slug",
                new { schema.Slug });
            if (exists == 0)
            {
                await _db.InsertAsync(schema);
                seeded++;
            }
        }

        _logger.LogInformation("pSEO content schemas seeded: {Count} new of {Total} total", seeded, schemas.Count);
    }

    private static List<ContentSchema> BuildSchemas()
    {
        return new List<ContentSchema>
        {
            // ── 1. idea-list ────────────────────────────────────────────
            new ContentSchema
            {
                Slug = "idea-list",
                Name = "Idea List",
                Description = "A comprehensive list of ideas, tips, or examples around a specific subtopic and niche",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "ideas", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string", description = "SEO-optimized page title" },
                        ["intro"] = new { type = "string", description = "2-3 paragraph introduction establishing context and value" },
                        ["ideas"] = new
                        {
                            type = "array",
                            minItems = 25,
                            items = new
                            {
                                type = "object",
                                required = new[] { "number", "title", "description" },
                                properties = new Dictionary<string, object>
                                {
                                    ["number"] = new { type = "integer" },
                                    ["title"] = new { type = "string" },
                                    ["description"] = new { type = "string", description = "2-4 sentences explaining the idea with actionable detail" },
                                    ["example"] = new { type = "string", description = "Optional real-world example" }
                                }
                            }
                        },
                        ["conclusion"] = new { type = "string", description = "2-3 paragraph conclusion with next steps" },
                        ["faq"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["question"] = new { type = "string" },
                                    ["answer"] = new { type = "string" }
                                }
                            }
                        }
                    }
                }),
                PromptTemplate = "You are an expert content strategist specializing in {niche}. Generate a comprehensive, actionable idea list about {subtopic}. Each idea must be specific, practical, and immediately usable by the target audience. Avoid generic advice — every idea should demonstrate deep domain knowledge. Include real-world examples where possible. Write in a professional but approachable tone. Target 50-100 ideas minimum, organized from foundational to advanced.",
                UserPromptTemplate = "Create a comprehensive idea list: \"100 {subtopic} Ideas for {niche} in {year}\". Target audience: {audience}. Key pain points to address: {pain_points}.",
                TitlePattern = "100 {subtopic} Ideas for {niche} in {year}",
                MetaDescPattern = "Discover 100+ actionable {subtopic} ideas for {niche}. Practical strategies, real examples, and expert tips to accelerate your success in {year}.",
                RendererSlug = "idea-list",
                IsSystem = true
            },

            // ── 2. checklist ────────────────────────────────────────────
            new ContentSchema
            {
                Slug = "checklist",
                Name = "Checklist",
                Description = "A step-by-step checklist that guides users through a process related to a subtopic and niche",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "sections", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs explaining why this checklist matters and who it's for" },
                        ["sections"] = new
                        {
                            type = "array",
                            minItems = 3,
                            items = new
                            {
                                type = "object",
                                required = new[] { "heading", "items" },
                                properties = new Dictionary<string, object>
                                {
                                    ["heading"] = new { type = "string" },
                                    ["description"] = new { type = "string" },
                                    ["items"] = new
                                    {
                                        type = "array",
                                        items = new
                                        {
                                            type = "object",
                                            required = new[] { "task", "details" },
                                            properties = new Dictionary<string, object>
                                            {
                                                ["task"] = new { type = "string", description = "Clear, actionable task description" },
                                                ["details"] = new { type = "string", description = "Why this matters and how to do it" },
                                                ["pro_tip"] = new { type = "string", description = "Optional expert tip" }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["conclusion"] = new { type = "string" },
                        ["downloadable_summary"] = new { type = "string", description = "Condensed bullet-point version for quick reference" }
                    }
                }),
                PromptTemplate = "You are a meticulous {niche} expert creating a comprehensive, actionable checklist about {subtopic}. Every item must be specific and verifiable — the reader should be able to check it off as done. Organize items logically from setup/prerequisites through execution to verification. Include expert pro-tips that demonstrate deep domain knowledge. The checklist should be thorough enough that following it guarantees a quality outcome.",
                UserPromptTemplate = "Create a thorough checklist: \"{subtopic} Checklist for {niche}\". Target audience: {audience}. Pain points to address: {pain_points}. Include 20-40 specific, checkable items organized in logical sections.",
                TitlePattern = "{subtopic} Checklist for {niche}",
                MetaDescPattern = "The complete {subtopic} checklist for {niche}. Step-by-step tasks, expert pro-tips, and a downloadable summary to ensure nothing gets missed.",
                RendererSlug = "checklist",
                IsSystem = true
            },

            // ── 3. how-to ──────────────────────────────────────────────
            new ContentSchema
            {
                Slug = "how-to",
                Name = "How-To Guide",
                Description = "A detailed step-by-step tutorial showing how to accomplish a specific task",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "prerequisites", "steps", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs explaining what the reader will learn and achieve" },
                        ["prerequisites"] = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "What the reader needs before starting"
                        },
                        ["estimated_time"] = new { type = "string", description = "How long this process typically takes" },
                        ["difficulty"] = new { type = "string", description = "beginner, intermediate, or advanced" },
                        ["steps"] = new
                        {
                            type = "array",
                            minItems = 5,
                            items = new
                            {
                                type = "object",
                                required = new[] { "step_number", "title", "content" },
                                properties = new Dictionary<string, object>
                                {
                                    ["step_number"] = new { type = "integer" },
                                    ["title"] = new { type = "string" },
                                    ["content"] = new { type = "string", description = "Detailed instruction with 3-5 paragraphs" },
                                    ["warning"] = new { type = "string", description = "Optional common mistake to avoid" },
                                    ["tip"] = new { type = "string", description = "Optional pro tip for better results" }
                                }
                            }
                        },
                        ["conclusion"] = new { type = "string", description = "Summary and next steps" },
                        ["troubleshooting"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["problem"] = new { type = "string" },
                                    ["solution"] = new { type = "string" }
                                }
                            }
                        }
                    }
                }),
                PromptTemplate = "You are an experienced {niche} practitioner writing a detailed how-to guide about {subtopic}. Write as if teaching someone in person — be specific, anticipate questions, and explain the 'why' behind each step. Include common pitfalls and how to avoid them. Each step should be self-contained and actionable. Include a troubleshooting section for common issues. The guide should take someone from zero to completion.",
                UserPromptTemplate = "Write a detailed how-to guide: \"How to {subtopic} for {niche}\". Target audience: {audience}. Address these pain points: {pain_points}. Include prerequisites, clear steps, and troubleshooting tips.",
                TitlePattern = "How to {subtopic} for {niche}",
                MetaDescPattern = "Learn exactly how to {subtopic} for {niche} with this step-by-step guide. Includes prerequisites, detailed instructions, pro tips, and troubleshooting.",
                RendererSlug = "how-to",
                IsSystem = true
            },

            // ── 4. comparison ───────────────────────────────────────────
            new ContentSchema
            {
                Slug = "comparison",
                Name = "Comparison",
                Description = "A detailed side-by-side comparison of tools, methods, or approaches for a specific niche",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "criteria", "items", "comparison_table", "verdict", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs setting up the comparison and why it matters" },
                        ["criteria"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["name"] = new { type = "string" },
                                    ["description"] = new { type = "string" },
                                    ["weight"] = new { type = "string", description = "How important this criterion is: high, medium, low" }
                                }
                            }
                        },
                        ["items"] = new
                        {
                            type = "array",
                            minItems = 3,
                            items = new
                            {
                                type = "object",
                                required = new[] { "name", "summary", "pros", "cons", "best_for", "scores" },
                                properties = new Dictionary<string, object>
                                {
                                    ["name"] = new { type = "string" },
                                    ["summary"] = new { type = "string", description = "2-3 paragraph overview" },
                                    ["pros"] = new { type = "array", items = new { type = "string" } },
                                    ["cons"] = new { type = "array", items = new { type = "string" } },
                                    ["best_for"] = new { type = "string" },
                                    ["pricing"] = new { type = "string" },
                                    ["scores"] = new { type = "object", description = "Score per criterion (1-10)" }
                                }
                            }
                        },
                        ["comparison_table"] = new { type = "object", description = "Structured feature comparison matrix" },
                        ["verdict"] = new { type = "string", description = "2-3 paragraphs with final recommendation by use case" },
                        ["conclusion"] = new { type = "string" }
                    }
                }),
                PromptTemplate = "You are an unbiased {niche} analyst creating a thorough comparison of {subtopic} options. Be fair and evidence-based — highlight genuine strengths and weaknesses for each option. Use specific criteria that matter to the target audience. Include pricing where relevant. Provide a clear verdict organized by use case (e.g., 'Best for beginners', 'Best for enterprise'). Avoid marketing language — be honest and helpful.",
                UserPromptTemplate = "Create a detailed comparison: \"{subtopic} Comparison for {niche}\". Target audience: {audience}. Compare at least 5 options using criteria that matter most to this audience. Include pricing, pros/cons, and clear recommendations by use case.",
                TitlePattern = "{subtopic} Comparison for {niche}",
                MetaDescPattern = "Unbiased {subtopic} comparison for {niche}. Side-by-side analysis with pricing, pros/cons, feature matrix, and clear recommendations by use case.",
                RendererSlug = "comparison",
                IsSystem = true
            },

            // ── 5. alternatives ─────────────────────────────────────────
            new ContentSchema
            {
                Slug = "alternatives",
                Name = "Alternatives",
                Description = "A roundup of the best alternatives to a popular tool, service, or approach",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "original_overview", "alternatives", "comparison_table", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs on why people look for alternatives" },
                        ["original_overview"] = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["name"] = new { type = "string" },
                                ["strengths"] = new { type = "array", items = new { type = "string" } },
                                ["limitations"] = new { type = "array", items = new { type = "string" } },
                                ["pricing"] = new { type = "string" }
                            }
                        },
                        ["alternatives"] = new
                        {
                            type = "array",
                            minItems = 5,
                            items = new
                            {
                                type = "object",
                                required = new[] { "name", "tagline", "description", "pros", "cons", "best_for", "pricing" },
                                properties = new Dictionary<string, object>
                                {
                                    ["name"] = new { type = "string" },
                                    ["tagline"] = new { type = "string", description = "One-line value proposition" },
                                    ["description"] = new { type = "string", description = "2-3 paragraphs detailed overview" },
                                    ["pros"] = new { type = "array", items = new { type = "string" } },
                                    ["cons"] = new { type = "array", items = new { type = "string" } },
                                    ["best_for"] = new { type = "string" },
                                    ["pricing"] = new { type = "string" },
                                    ["migration_difficulty"] = new { type = "string", description = "easy, moderate, complex" }
                                }
                            }
                        },
                        ["comparison_table"] = new { type = "object", description = "Quick-reference feature matrix" },
                        ["conclusion"] = new { type = "string", description = "2-3 paragraphs with final thoughts and decision framework" }
                    }
                }),
                PromptTemplate = "You are a {niche} expert helping readers find the best alternative to a popular {subtopic} solution. First explain why someone might want to switch — be specific about common frustrations. Then present each alternative with honest analysis including migration difficulty. Prioritize alternatives by how well they address the original tool's limitations. Include real pricing and practical migration considerations.",
                UserPromptTemplate = "Create an alternatives guide: \"Best {subtopic} Alternatives for {niche}\". Target audience: {audience}. List 8-12 alternatives with honest pros/cons, pricing, migration difficulty, and clear best-for recommendations.",
                TitlePattern = "Best {subtopic} Alternatives for {niche}",
                MetaDescPattern = "Discover the best {subtopic} alternatives for {niche}. Honest reviews with pricing, pros/cons, migration tips, and recommendations for every use case.",
                RendererSlug = "alternatives",
                IsSystem = true
            },

            // ── 6. resource-list ────────────────────────────────────────
            new ContentSchema
            {
                Slug = "resource-list",
                Name = "Resource List",
                Description = "A curated collection of the best resources (tools, articles, courses, communities) for a subtopic",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "categories", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs on why these resources matter and how to use this guide" },
                        ["categories"] = new
                        {
                            type = "array",
                            minItems = 4,
                            items = new
                            {
                                type = "object",
                                required = new[] { "name", "description", "resources" },
                                properties = new Dictionary<string, object>
                                {
                                    ["name"] = new { type = "string", description = "Category name (e.g., 'Tools', 'Courses', 'Communities')" },
                                    ["description"] = new { type = "string" },
                                    ["resources"] = new
                                    {
                                        type = "array",
                                        items = new
                                        {
                                            type = "object",
                                            required = new[] { "name", "description", "type", "cost" },
                                            properties = new Dictionary<string, object>
                                            {
                                                ["name"] = new { type = "string" },
                                                ["description"] = new { type = "string", description = "2-3 sentences on what makes this resource valuable" },
                                                ["url"] = new { type = "string" },
                                                ["type"] = new { type = "string", description = "tool, course, book, community, blog, podcast, template" },
                                                ["cost"] = new { type = "string", description = "free, freemium, paid with price range" },
                                                ["best_for"] = new { type = "string" }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["getting_started_path"] = new { type = "string", description = "Recommended order of resources for beginners" },
                        ["conclusion"] = new { type = "string" }
                    }
                }),
                PromptTemplate = "You are a {niche} curator assembling the definitive resource list for {subtopic}. Only recommend resources you'd genuinely suggest to a colleague — no filler. Organize resources into logical categories (tools, courses, communities, books, etc.). Include a mix of free and paid options. For each resource, explain specifically what makes it valuable and who it's best for. Include a 'getting started' path for beginners.",
                UserPromptTemplate = "Curate a comprehensive resource list: \"Best {subtopic} Resources for {niche}\". Target audience: {audience}. Include tools, courses, communities, books, and other resources organized by category. Specify cost (free/paid) and best-for use cases.",
                TitlePattern = "Best {subtopic} Resources for {niche}",
                MetaDescPattern = "The best {subtopic} resources for {niche} — curated tools, courses, books, and communities. Organized by category with cost info and beginner paths.",
                RendererSlug = "resource-list",
                IsSystem = true
            },

            // ── 7. tool-page ────────────────────────────────────────────
            new ContentSchema
            {
                Slug = "tool-page",
                Name = "Tool Page",
                Description = "A free interactive tool or calculator page that provides immediate value",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "tool_description", "how_to_use", "methodology", "related_content", "faq" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs explaining what the tool does and why it's useful" },
                        ["tool_description"] = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["purpose"] = new { type = "string" },
                                ["inputs"] = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        properties = new Dictionary<string, object>
                                        {
                                            ["name"] = new { type = "string" },
                                            ["type"] = new { type = "string", description = "text, number, select, range" },
                                            ["label"] = new { type = "string" },
                                            ["placeholder"] = new { type = "string" },
                                            ["options"] = new { type = "array", items = new { type = "string" } }
                                        }
                                    }
                                },
                                ["output_format"] = new { type = "string", description = "Description of what the tool outputs" }
                            }
                        },
                        ["how_to_use"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["step"] = new { type = "integer" },
                                    ["instruction"] = new { type = "string" }
                                }
                            }
                        },
                        ["methodology"] = new { type = "string", description = "2-3 paragraphs explaining the logic/formula behind the tool" },
                        ["related_content"] = new { type = "string", description = "Supporting educational content (500+ words)" },
                        ["faq"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["question"] = new { type = "string" },
                                    ["answer"] = new { type = "string" }
                                }
                            }
                        }
                    }
                }),
                PromptTemplate = "You are a {niche} expert designing a free tool/calculator for {subtopic}. Describe the tool's purpose, inputs, outputs, and methodology clearly. The tool should solve a specific, common problem for the target audience. Write supporting educational content that explains the concepts behind the tool and helps users interpret results. Include clear usage instructions and FAQ.",
                UserPromptTemplate = "Design a free tool page: \"Free {subtopic} Tool for {niche}\". Target audience: {audience}. Define the tool's inputs, outputs, methodology, and supporting content. The tool should address: {pain_points}.",
                TitlePattern = "Free {subtopic} Tool for {niche}",
                MetaDescPattern = "Free {subtopic} tool for {niche}. Calculate, analyze, or generate results instantly. No signup required — includes methodology and expert guidance.",
                RendererSlug = "tool-page",
                IsSystem = true
            },

            // ── 8. faq ─────────────────────────────────────────────────
            new ContentSchema
            {
                Slug = "faq",
                Name = "FAQ",
                Description = "A comprehensive FAQ page answering the most common questions about a subtopic",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "sections", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs setting context and explaining who this FAQ is for" },
                        ["sections"] = new
                        {
                            type = "array",
                            minItems = 4,
                            items = new
                            {
                                type = "object",
                                required = new[] { "heading", "questions" },
                                properties = new Dictionary<string, object>
                                {
                                    ["heading"] = new { type = "string", description = "Section category (e.g., 'Getting Started', 'Pricing', 'Technical')" },
                                    ["questions"] = new
                                    {
                                        type = "array",
                                        minItems = 3,
                                        items = new
                                        {
                                            type = "object",
                                            required = new[] { "question", "answer" },
                                            properties = new Dictionary<string, object>
                                            {
                                                ["question"] = new { type = "string" },
                                                ["answer"] = new { type = "string", description = "Thorough 2-4 paragraph answer" },
                                                ["related_questions"] = new { type = "array", items = new { type = "string" } }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["conclusion"] = new { type = "string" },
                        ["still_have_questions"] = new { type = "string", description = "CTA for users with unanswered questions" }
                    }
                }),
                PromptTemplate = "You are a {niche} expert answering the most common questions about {subtopic}. Write answers that are thorough yet accessible — assume the reader has basic knowledge but needs expert guidance. Each answer should be 2-4 paragraphs and include practical advice. Organize questions into logical sections. Include 'related questions' to help readers discover more. Target 25-40 questions total across all sections.",
                UserPromptTemplate = "Create a comprehensive FAQ: \"{subtopic} FAQs for {niche}\". Target audience: {audience}. Address these pain points through Q&A: {pain_points}. Include 25-40 questions organized into 5-8 sections.",
                TitlePattern = "{subtopic} FAQs for {niche}",
                MetaDescPattern = "Get answers to the most common {subtopic} questions for {niche}. Expert answers organized by topic — from beginner basics to advanced techniques.",
                RendererSlug = "faq",
                IsSystem = true
            },

            // ── 9. glossary ─────────────────────────────────────────────
            new ContentSchema
            {
                Slug = "glossary",
                Name = "Glossary",
                Description = "A comprehensive glossary of terms, jargon, and concepts for a subtopic and niche",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "terms", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs explaining who this glossary is for and how to use it" },
                        ["terms"] = new
                        {
                            type = "array",
                            minItems = 30,
                            items = new
                            {
                                type = "object",
                                required = new[] { "term", "definition", "category" },
                                properties = new Dictionary<string, object>
                                {
                                    ["term"] = new { type = "string" },
                                    ["definition"] = new { type = "string", description = "Clear, jargon-free definition in 2-4 sentences" },
                                    ["category"] = new { type = "string", description = "Grouping category for the term" },
                                    ["example"] = new { type = "string", description = "Real-world usage example" },
                                    ["related_terms"] = new { type = "array", items = new { type = "string" } },
                                    ["also_known_as"] = new { type = "array", items = new { type = "string" } }
                                }
                            }
                        },
                        ["conclusion"] = new { type = "string" },
                        ["further_reading"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["title"] = new { type = "string" },
                                    ["description"] = new { type = "string" }
                                }
                            }
                        }
                    }
                }),
                PromptTemplate = "You are a {niche} educator creating a definitive glossary of {subtopic} terminology. Define each term in plain language that a newcomer can understand, while being precise enough for experts. Include real-world examples of how each term is used. Group terms by category and cross-reference related terms. Aim for 40-60 terms covering foundational through advanced concepts. Avoid circular definitions.",
                UserPromptTemplate = "Create a comprehensive glossary: \"{subtopic} Glossary for {niche}\". Target audience: {audience}. Define 40-60 terms from beginner to advanced, organized by category with examples and cross-references.",
                TitlePattern = "{subtopic} Glossary for {niche}",
                MetaDescPattern = "The complete {subtopic} glossary for {niche}. 50+ terms defined in plain language with examples, cross-references, and further reading.",
                RendererSlug = "glossary",
                IsSystem = true
            },

            // ── 10. template-pack ───────────────────────────────────────
            new ContentSchema
            {
                Slug = "template-pack",
                Name = "Template Pack",
                Description = "A collection of ready-to-use templates, frameworks, and swipe files for a subtopic",
                SchemaJson = JsonSerializer.Serialize(new
                {
                    type = "object",
                    required = new[] { "title", "intro", "templates", "how_to_customize", "conclusion" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "string" },
                        ["intro"] = new { type = "string", description = "2-3 paragraphs explaining what templates are included and how to use them" },
                        ["templates"] = new
                        {
                            type = "array",
                            minItems = 5,
                            items = new
                            {
                                type = "object",
                                required = new[] { "name", "purpose", "template_content", "how_to_use" },
                                properties = new Dictionary<string, object>
                                {
                                    ["name"] = new { type = "string" },
                                    ["purpose"] = new { type = "string", description = "When and why to use this template" },
                                    ["template_content"] = new { type = "string", description = "The actual template with placeholders marked as [PLACEHOLDER]" },
                                    ["how_to_use"] = new { type = "string", description = "Step-by-step customization instructions" },
                                    ["example_filled"] = new { type = "string", description = "Example with placeholders filled in for a real scenario" },
                                    ["variations"] = new { type = "array", items = new { type = "string" }, description = "Alternative versions for different contexts" }
                                }
                            }
                        },
                        ["how_to_customize"] = new { type = "string", description = "General guidance on adapting templates to specific situations" },
                        ["conclusion"] = new { type = "string" },
                        ["bonus_tips"] = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Extra tips for getting the most from these templates"
                        }
                    }
                }),
                PromptTemplate = "You are a {niche} expert creating a practical template pack for {subtopic}. Each template must be immediately usable — include the full template with clear [PLACEHOLDER] markers, step-by-step customization instructions, and a filled-in example. Templates should solve real problems the target audience faces daily. Include variations for different contexts. Focus on templates that save significant time and reduce errors.",
                UserPromptTemplate = "Create a template pack: \"{subtopic} Templates for {niche}\". Target audience: {audience}. Include 8-15 ready-to-use templates with placeholder text, customization guides, and filled-in examples. Address: {pain_points}.",
                TitlePattern = "{subtopic} Templates for {niche}",
                MetaDescPattern = "Free {subtopic} templates for {niche}. Ready-to-use templates with examples, customization guides, and expert tips. Copy, customize, and deploy.",
                RendererSlug = "template-pack",
                IsSystem = true
            },
        };
    }
}
