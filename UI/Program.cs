using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.UseCases.Authorizations;
using Application.UseCases.Billing;
using Domain.Beneficiaries;
using Domain.Plans;
using Domain.Procedures;
using Domain.Authorizations;
using Domain.Billing;
using Infra.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace UI;

class Program
{
    static async Task Main(string[] args)
    {
        using var serviceProvider = CreateServiceProvider();

        var beneficiaryRepository = serviceProvider.GetRequiredService<IBeneficiaryRepository>();
        var planRepository = serviceProvider.GetRequiredService<IPlanRepository>();
        var procedureCatalogRepository = serviceProvider.GetRequiredService<IProcedureCatalogRepository>();
        var beneficiaryId = await SeedReferenceDataAsync(
            beneficiaryRepository,
            planRepository,
            procedureCatalogRepository);

        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var billingService = serviceProvider.GetRequiredService<IBillingService>();

        var electiveRequestDto = new AuthorizationRequestDto
        {
            BeneficiaryId = beneficiaryId,
            PlanNumber = "123456",
            ProcedureCode = "9999",
            CidCode = "M54.5",
            RequestingProfessional = "CRM-12345",
            ExecutingEstablishment = "Hospital BemAli",
            ExpectedDate = DateTime.Now.AddDays(1),
            RequestedItems = new List<RequestedItemDto>
            {
                new() { Description = "Soro", Quantity = 2, ItemType = "Material" },
                new() { Description = "Dipirona", Quantity = 1, ItemType = "Medicamento" }
            },
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
                .ToDictionary(item => item.Id, item => new MoneyDto { Amount = 25.50m, Currency = "BRL" })
        });

        var bill = await billingService.GetHospitalBillAsync(billId);
        Console.WriteLine($"Conta hospitalar criada: {bill.Id} - Itens: {bill.Items.Count} - Total: {bill.TotalValue.Amount:0.00} {bill.TotalValue.Currency}");

        var emergencyRequestDto = new AuthorizationRequestDto
        {
            BeneficiaryId = beneficiaryId,
            PlanNumber = "123456",
            ProcedureCode = "8888",
            CidCode = "S72.0",
            RequestingProfessional = "CRM-54321",
            ExecutingEstablishment = "Pronto Atendimento BemAli",
            ExpectedDate = DateTime.Now,
            RequestedItems = new List<RequestedItemDto>
            {
                new() { Description = "Imobilizador", Quantity = 1, ItemType = "Material" },
                new() { Description = "Analgésico", Quantity = 1, ItemType = "Medicamento" }
            },
            IsUrgentOrEmergency = true
        };

        var emergencyAuthorizationId = await authorizationService.RequestAuthorizationAsync(emergencyRequestDto);
        var emergencyStatus = await authorizationService.GetAuthorizationStatusAsync(emergencyAuthorizationId);
        Console.WriteLine($"Urgência: {emergencyStatus.Status} - Auditoria posterior: {emergencyStatus.RequiresPostPaymentAudit}");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IAuthorizationRepository, AuthorizationRepository>();
        services.AddSingleton<IHospitalBillRepository, HospitalBillRepository>();
        services.AddSingleton<IBeneficiaryRepository, BeneficiaryRepository>();
        services.AddSingleton<IPlanRepository, PlanRepository>();
        services.AddSingleton<IProcedureCatalogRepository, ProcedureCatalogRepository>();
        services.AddSingleton<EligibilityService>();
        services.AddSingleton<RequestAuthorizationUseCase>();
        services.AddSingleton<ApproveAuthorizationUseCase>();
        services.AddSingleton<ApproveAuthorizationPartiallyUseCase>();
        services.AddSingleton<DenyAuthorizationUseCase>();
        services.AddSingleton<RegisterDocumentPendingUseCase>();
        services.AddSingleton<GetAuthorizationStatusUseCase>();
        services.AddSingleton<CreateHospitalBillFromAuthorizationUseCase>();
        services.AddSingleton<GetHospitalBillUseCase>();
        services.AddSingleton<ApplyGlosaToHospitalBillItemUseCase>();
        services.AddSingleton<FileGlosaAppealUseCase>();
        services.AddSingleton<CloseHospitalBillUseCase>();
        services.AddSingleton<IAuthorizationService, AuthorizationService>();
        services.AddSingleton<IBillingService, BillingService>();

        return services.BuildServiceProvider();
    }

    private static async Task<Guid> SeedReferenceDataAsync(
        IBeneficiaryRepository beneficiaryRepository,
        IPlanRepository planRepository,
        IProcedureCatalogRepository procedureCatalogRepository)
    {
        var planId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var beneficiaryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var plan = new Plan(planId, "123456", PlanType.Individual, 0);
        plan.SetGracePeriod(ProcedureType.ExameSimples, 30);
        plan.SetGracePeriod(ProcedureType.Cirurgia, 0);

        await planRepository.AddAsync(plan);
        await beneficiaryRepository.AddAsync(new Beneficiary(
            beneficiaryId,
            "Maria Silva",
            DateTime.Today.AddYears(-30),
            DateTime.Today.AddYears(-1),
            planId));

        await procedureCatalogRepository.AddAsync(new ProcedureCatalogItem(
            new ProcedureCode("9999"),
            "Procedimento eletivo de teste",
            ProcedureType.ExameSimples,
            18,
            65));

        await procedureCatalogRepository.AddAsync(new ProcedureCatalogItem(
            new ProcedureCode("8888"),
            "Atendimento de urgência de teste",
            ProcedureType.Cirurgia,
            18,
            65));

        return beneficiaryId;
    }
}
