using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace WebUI.Pages.Authorizations;

public class DetailsModel : PageModel
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IAuthorizationService authorizationService,
        ILogger<DetailsModel> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [BindProperty]
    public Guid AuthorizationId { get; set; }

    [BindProperty]
    public Dictionary<Guid, int> ApprovedQuantities { get; set; } = new();

    public AuthorizationStatusDto Authorization { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(Guid id)
    {
        Authorization = await _authorizationService.GetAuthorizationStatusAsync(id);
        AuthorizationId = id;
    }

    public async Task<IActionResult> OnPostApprovePartialAsync()
    {
        try
        {
            var approved = ApprovedQuantities
                .Where(item => item.Value > 0)
                .ToDictionary(item => item.Key, item => item.Value);

            await _authorizationService.ApproveAuthorizationPartiallyAsync(AuthorizationId, approved);
            return RedirectToPage(new { id = AuthorizationId });
        }
        catch (Exception ex)
        {
            Authorization = await _authorizationService.GetAuthorizationStatusAsync(AuthorizationId);
            _logger.LogError(ex, "Erro ao aprovar autorização parcialmente.");
            ErrorMessage = "Não foi possível aprovar parcialmente a autorização. Tente novamente.";
            return Page();
        }
    }
}
