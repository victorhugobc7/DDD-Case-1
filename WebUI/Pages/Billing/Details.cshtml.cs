using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebUI.Pages.Billing;

public class DetailsModel : PageModel
{
    private readonly IBillingService _billingService;

    public DetailsModel(IBillingService billingService)
    {
        _billingService = billingService;
    }

    public HospitalBillDto Bill { get; private set; } = new();

    public async Task OnGetAsync(Guid id)
    {
        Bill = await _billingService.GetHospitalBillAsync(id);
    }
}
