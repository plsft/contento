using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IUserService userService, ILogger<IndexModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Role { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public IEnumerable<User> Users { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        var pageSize = 20;

        try
        {
            if (!string.IsNullOrEmpty(Role))
            {
                Users = await _userService.GetByRoleAsync(Role, page: Page, pageSize: pageSize);
                TotalCount = await _userService.GetTotalCountAsync(Role);
            }
            else
            {
                Users = await _userService.GetAllAsync(page: Page, pageSize: pageSize);
                TotalCount = await _userService.GetTotalCountAsync();
            }
            TotalPages = (int)Math.Ceiling(TotalCount / (double)pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _userService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user in {Page}", nameof(IndexModel));
        }

        return RedirectToPage(new { role = Role, page = Page });
    }
}
