using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing membership plans/tiers with CRUD operations,
/// slug-based lookup, and active-only filtering.
/// </summary>
public class MembershipPlanService : IMembershipPlanService
{
    private readonly IDbConnection _db;
    private readonly ILogger<MembershipPlanService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MembershipPlanService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public MembershipPlanService(IDbConnection db, ILogger<MembershipPlanService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<MembershipPlan?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<MembershipPlan>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MembershipPlan>> GetAllAsync(Guid siteId, bool activeOnly = true)
    {
        Guard.Against.Default(siteId);

        if (activeOnly)
        {
            return await _db.QueryAsync<MembershipPlan>(
                "SELECT * FROM membership_plans WHERE site_id = @SiteId AND is_active = true ORDER BY sort_order, price",
                new { SiteId = siteId });
        }

        return await _db.QueryAsync<MembershipPlan>(
            "SELECT * FROM membership_plans WHERE site_id = @SiteId ORDER BY sort_order, price",
            new { SiteId = siteId });
    }

    /// <inheritdoc />
    public async Task<MembershipPlan?> GetBySlugAsync(Guid siteId, string slug)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(slug);

        var results = await _db.QueryAsync<MembershipPlan>(
            "SELECT * FROM membership_plans WHERE site_id = @SiteId AND slug = @Slug LIMIT 1",
            new { SiteId = siteId, Slug = slug });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<MembershipPlan> CreateAsync(MembershipPlan plan)
    {
        Guard.Against.Null(plan);
        Guard.Against.NullOrWhiteSpace(plan.Name);
        Guard.Against.NullOrWhiteSpace(plan.Slug);

        plan.Id = Guid.NewGuid();
        plan.CreatedAt = DateTime.UtcNow;

        await _db.InsertAsync(plan);
        return plan;
    }

    /// <inheritdoc />
    public async Task<MembershipPlan> UpdateAsync(MembershipPlan plan)
    {
        Guard.Against.Null(plan);
        Guard.Against.Default(plan.Id);

        await _db.UpdateAsync(plan);
        return plan;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var plan = await _db.GetAsync<MembershipPlan>(id);
        if (plan == null)
            return;

        await _db.DeleteAsync(plan);
    }
}
