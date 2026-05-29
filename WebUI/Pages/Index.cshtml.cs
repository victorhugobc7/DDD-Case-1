using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebUI.Pages;

public class IndexModel : PageModel
{
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    [BindProperty]
    public AuthorizationForm Input { get; set; } = AuthorizationForm.CreateDefault();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        Input = AuthorizationForm.CreateDefault();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var authorizationId = await _authorizationService.RequestAuthorizationAsync(Input.ToDto());
            return RedirectToPage("/Authorizations/Details", new { id = authorizationId });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public class AuthorizationForm
    {
        public Guid BeneficiaryId { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public string ProcedureCode { get; set; } = string.Empty;
        public string CidCode { get; set; } = string.Empty;
        public string RequestingProfessional { get; set; } = string.Empty;
        public string ExecutingEstablishment { get; set; } = string.Empty;
        public DateTime ExpectedDate { get; set; }
        public bool IsUrgentOrEmergency { get; set; }
        public string FirstItemDescription { get; set; } = string.Empty;
        public int FirstItemQuantity { get; set; }
        public string SecondItemDescription { get; set; } = string.Empty;
        public int SecondItemQuantity { get; set; }

        public static AuthorizationForm CreateDefault()
        {
            return new AuthorizationForm
            {
                BeneficiaryId = DemoData.BeneficiaryId,
                PlanNumber = DemoData.PlanNumber,
                ProcedureCode = DemoData.ProcedureCode,
                CidCode = DemoData.CidCode,
                RequestingProfessional = "CRM-12345",
                ExecutingEstablishment = "Hospital BemAli",
                ExpectedDate = DateTime.Today.AddDays(1),
                FirstItemDescription = "Soro",
                FirstItemQuantity = 2,
                SecondItemDescription = "Dipirona",
                SecondItemQuantity = 1
            };
        }

        public AuthorizationRequestDto ToDto()
        {
            return new AuthorizationRequestDto
            {
                BeneficiaryId = BeneficiaryId,
                PlanNumber = PlanNumber,
                ProcedureCode = ProcedureCode,
                CidCode = CidCode,
                RequestingProfessional = RequestingProfessional,
                ExecutingEstablishment = ExecutingEstablishment,
                ExpectedDate = ExpectedDate,
                IsUrgentOrEmergency = IsUrgentOrEmergency,
                RequestedItems = new List<RequestedItemDto>
                {
                    new()
                    {
                        Description = FirstItemDescription,
                        Quantity = FirstItemQuantity,
                        ItemType = "Material"
                    },
                    new()
                    {
                        Description = SecondItemDescription,
                        Quantity = SecondItemQuantity,
                        ItemType = "Medicamento"
                    }
                }
            };
        }
    }
}
