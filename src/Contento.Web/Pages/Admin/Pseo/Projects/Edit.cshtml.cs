using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Pseo.Projects;

public class EditModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IPseoProjectService projectService,
        ILogger<EditModel> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    public PseoProject Project { get; set; } = null!;
    public bool DnsVerified { get; set; }

    [BindProperty]
    public string ProjectName { get; set; } = string.Empty;

    [BindProperty]
    public string RootDomain { get; set; } = string.Empty;

    [BindProperty]
    public string ProjectSubdomain { get; set; } = string.Empty;

    [BindProperty]
    public string? HeaderHtml { get; set; }

    [BindProperty]
    public string? FooterHtml { get; set; }

    [BindProperty]
    public string? CustomCss { get; set; }

    [BindProperty]
    public string BackLinkText { get; set; } = "Back to our site";

    [BindProperty]
    public string? BackLinkUrl { get; set; }

    [BindProperty]
    public string? CtaHtml { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null) return RedirectToPage("Index");

            Project = project;
            ProjectName = project.Name;
            RootDomain = project.RootDomain;
            ProjectSubdomain = project.Subdomain;
            HeaderHtml = project.HeaderHtml;
            FooterHtml = project.FooterHtml;
            CustomCss = project.CustomCss;
            BackLinkText = project.BackLinkText;
            BackLinkUrl = project.BackLinkUrl;
            CtaHtml = project.CtaHtml;

            DnsVerified = await _projectService.CheckDnsAsync(project.Fqdn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load project in {Page}", nameof(EditModel));
            return RedirectToPage("Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null) return RedirectToPage("Index");

            project.Name = ProjectName;
            project.RootDomain = RootDomain;
            project.Subdomain = ProjectSubdomain;
            project.Fqdn = $"{ProjectSubdomain}.{RootDomain}";
            project.BackLinkText = BackLinkText;
            project.BackLinkUrl = BackLinkUrl;
            project.CtaHtml = CtaHtml;

            await _projectService.UpdateAsync(project);
            await _projectService.UpdateChromeAsync(id, HeaderHtml, FooterHtml, CustomCss);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCheckDnsAsync(Guid id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project != null)
            {
                var verified = await _projectService.CheckDnsAsync(project.Fqdn);
                if (verified)
                {
                    await _projectService.UpdateStatusAsync(id, "active");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check DNS in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }
}
