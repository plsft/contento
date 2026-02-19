using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing membership plans/tiers.
/// </summary>
public interface IMembershipPlanService
{
    Task<MembershipPlan?> GetByIdAsync(Guid id);
    Task<IEnumerable<MembershipPlan>> GetAllAsync(Guid siteId, bool activeOnly = true);
    Task<MembershipPlan?> GetBySlugAsync(Guid siteId, string slug);
    Task<MembershipPlan> CreateAsync(MembershipPlan plan);
    Task<MembershipPlan> UpdateAsync(MembershipPlan plan);
    Task DeleteAsync(Guid id);
}
