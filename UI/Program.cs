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
        var repository = new AuthorizationRepository();
        var service = new AuthorizationService(repository);

        var requestDto = new AuthorizationRequestDto
        {
            BeneficiaryId = Guid.NewGuid(),
            PlanNumber = "123456",
            ProcedureCode = "9999",
            ClinicalJustification = "M54.5",
            RequestingProfessional = "CRM-12345",
            ExecutingEstablishment = "Hospital BemAli",
            ExpectedDate = DateTime.Now.AddDays(1),
            MaterialsAndMedicines = new List<string> { "Soro", "Dipirona" },
            IsUrgentOrEmergency = true
        };

        var authId = await service.RequestAuthorizationAsync(requestDto);
        Console.WriteLine($"Autorização feita com o id: {authId}");
    }
}
