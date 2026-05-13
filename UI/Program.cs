using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Services;
using Infra.Repositories;

namespace UI;

class Program
{
    static async Task Main(string[] args)
    {
        var authorizationRepository = new AuthorizationRepository();
        var hospitalBillRepository = new HospitalBillRepository();
        var authorizationService = new AuthorizationService(authorizationRepository);
        var billingService = new BillingService(authorizationRepository, hospitalBillRepository);

        var electiveRequestDto = new AuthorizationRequestDto
        {
            BeneficiaryId = Guid.NewGuid(),
            PlanNumber = "123456",
            ProcedureCode = "9999",
            ClinicalJustification = "M54.5",
            RequestingProfessional = "CRM-12345",
            ExecutingEstablishment = "Hospital BemAli",
            ExpectedDate = DateTime.Now.AddDays(1),
            MaterialsAndMedicines = new List<string> { "Soro", "Dipirona" },
            IsUrgentOrEmergency = false
        };

        var electiveAuthorizationId = await authorizationService.RequestAuthorizationAsync(electiveRequestDto);
        var pendingStatus = await authorizationService.GetAuthorizationStatusAsync(electiveAuthorizationId);
        Console.WriteLine($"Solicitação criada: {pendingStatus.Id} - Status: {pendingStatus.Status}");

        var firstItem = pendingStatus.Items.First();
        await authorizationService.ApproveAuthorizationPartiallyAsync(
            electiveAuthorizationId,
            new Dictionary<Guid, int> { [firstItem.Id] = 1 });

        var partialStatus = await authorizationService.GetAuthorizationStatusAsync(electiveAuthorizationId);
        Console.WriteLine($"Após análise: {partialStatus.Status}");

        var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
        {
            AuthorizationId = electiveAuthorizationId,
            UnitValuesByItemId = partialStatus.Items
                .Where(item => item.ApprovedQuantity > 0)
                .ToDictionary(item => item.Id, item => 25.50m)
        });

        var bill = await billingService.GetHospitalBillAsync(billId);
        Console.WriteLine($"Conta hospitalar criada: {bill.Id} - Itens: {bill.Items.Count} - Total: {bill.TotalValue:0.00}");

        var emergencyRequestDto = new AuthorizationRequestDto
        {
            BeneficiaryId = Guid.NewGuid(),
            PlanNumber = "123456",
            ProcedureCode = "8888",
            ClinicalJustification = "S72.0",
            RequestingProfessional = "CRM-54321",
            ExecutingEstablishment = "Pronto Atendimento BemAli",
            ExpectedDate = DateTime.Now,
            MaterialsAndMedicines = new List<string> { "Imobilizador", "Analgésico" },
            IsUrgentOrEmergency = true
        };

        var emergencyAuthorizationId = await authorizationService.RequestAuthorizationAsync(emergencyRequestDto);
        var emergencyStatus = await authorizationService.GetAuthorizationStatusAsync(emergencyAuthorizationId);
        Console.WriteLine($"Urgência: {emergencyStatus.Status} - Auditoria posterior: {emergencyStatus.RequiresPostPaymentAudit}");
    }
}
