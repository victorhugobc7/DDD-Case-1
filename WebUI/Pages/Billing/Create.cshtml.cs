using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace WebUI.Pages.Billing;

public class CreateModel : PageModel
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IBillingService _billingService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IAuthorizationService authorizationService,
        IBillingService billingService,
        ILogger<CreateModel> logger)
    {
        _authorizationService = authorizationService;
        _billingService = billingService;
        _logger = logger;
    }

    [BindProperty]
    public Guid AuthorizationId { get; set; }

    [BindProperty]
    public Dictionary<Guid, decimal> UnitValuesByItemId { get; set; } = new();

    public AuthorizationStatusDto Authorization { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(Guid authorizationId)
    {
        Authorization = await _authorizationService.GetAuthorizationStatusAsync(authorizationId);
        AuthorizationId = authorizationId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var billId = await _billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = AuthorizationId,
                UnitValuesByItemId = UnitValuesByItemId.ToDictionary(
                    item => item.Key,
                    item => new MoneyDto { Amount = item.Value, Currency = "BRL" })
            });

            return RedirectToPage("/Billing/Details", new { id = billId });
        }
        catch (Exception ex)
        {
            Authorization = await _authorizationService.GetAuthorizationStatusAsync(AuthorizationId);
            _logger.LogError(ex, "Erro ao criar faturamento.");
            ErrorMessage = "Não foi possível criar o faturamento. Tente novamente.";
            return Page();
        }
    }
}
