using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Services;
using Domain.Enums;
using Infra.Repositories;

namespace UI;

class Program
{
    static async Task Main(string[] args)
    {
        var repository = new AuthorizationRepository();
        var service = new AuthorizationService(repository);

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

        var electiveAuthorizationId = await service.RequestAuthorizationAsync(electiveRequestDto);
        var pendingStatus = await service.GetAuthorizationStatusAsync(electiveAuthorizationId);
        Console.WriteLine($"Solicitação criada: {pendingStatus.Id} - Status: {pendingStatus.Status}");

        var firstItem = pendingStatus.Items.First();
        await service.ApproveAuthorizationPartiallyAsync(
            electiveAuthorizationId,
            new Dictionary<Guid, int> { [firstItem.Id] = 1 });

        var partialStatus = await service.GetAuthorizationStatusAsync(electiveAuthorizationId);
        Console.WriteLine($"Após análise: {partialStatus.Status}");

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

        var emergencyAuthorizationId = await service.RequestAuthorizationAsync(emergencyRequestDto);
        var emergencyStatus = await service.GetAuthorizationStatusAsync(emergencyAuthorizationId);
        Console.WriteLine($"Urgência: {emergencyStatus.Status} - Auditoria posterior: {emergencyStatus.RequiresPostPaymentAudit}");
    }
}
