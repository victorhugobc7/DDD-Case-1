using Application.DTOs;
using Application.Services;
using Application.UseCases.Authorizations;
using Application.UseCases.Billing;
using Domain.Authorizations;
using Domain.Beneficiaries;
using Domain.Billing;
using Domain.Plans;
using Domain.Procedures;
using Domain.Audit;
using Domain.ProviderNetwork;
using Infra.Repositories;

namespace Tests;

public static class Program
{
    public static async Task Main()
    {
        var passed = 0;
        var failed = 0;

        foreach (var test in Tests())
        {
            try
            {
                await test.Run();
                passed++;
                Console.WriteLine($"[OK] {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"[FAIL] {test.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Resultado: {passed} passou/passaram, {failed} falhou/falharam.");

        if (failed > 0)
            Environment.ExitCode = 1;
    }

    private static IEnumerable<(string Name, Func<Task> Run)> Tests()
    {
        yield return ("solicitação sem itens é inválida", TestAuthorizationRequiresItems);
        yield return ("quantidade solicitada deve ser maior que zero", TestRequestedItemQuantity);
        yield return ("aprovação integral aprova todos os itens", TestFullApproval);
        yield return ("aprovação parcial controla quantidades por item", TestPartialApproval);
        yield return ("aprovação parcial rejeita quantidade acima da solicitada", TestPartialApprovalRejectsInvalidQuantity);
        yield return ("negativa exige justificativa", TestDenialRequiresReason);
        yield return ("urgência aprova e marca auditoria posterior", TestEmergencyRequiresPostPaymentAudit);
        yield return ("elegibilidade barra plano divergente", TestEligibilityPlanMismatch);
        yield return ("elegibilidade barra carência pendente", TestEligibilityGracePeriod);
        yield return ("elegibilidade barra idade fora da regra", TestEligibilityAge);
        yield return ("elegibilidade barra beneficiário inativo", TestEligibilityInactiveBeneficiary);
        yield return ("elegibilidade permite cenário válido", TestEligibilityAllowed);
        yield return ("value objects rejeitam valores inválidos", TestValueObjectsRejectInvalidValues);
        yield return ("administrative appeal valida ids e evidências", TestAdministrativeAppealRejectsInvalidData);
        yield return ("application coordena factory e repository", TestApplicationServiceFlow);
        yield return ("application exige elegibilidade na solicitação", TestApplicationRequiresEligibility);
        yield return ("faturamento cria conta para autorização aprovada integralmente", TestBillingFullApprovalCreatesHospitalBill);
        yield return ("faturamento parcial usa somente itens aprovados", TestBillingPartialApprovalUsesOnlyApprovedItems);
        yield return ("faturamento rejeita autorização pendente ou negada", TestBillingRejectsPendingOrDeniedAuthorization);
        yield return ("faturamento exige valor unitário válido por item aprovado", TestBillingRequiresUnitValueForApprovedItems);
        yield return ("money soma valores e rejeita moedas diferentes", TestMoneyOperations);
        yield return ("hospital bill calcula total e fecha conta", TestHospitalBillCloseBlocksChanges);
        yield return ("glosa passa pela conta hospitalar e aceita recurso", TestGlosaAppealFlow);
        yield return ("repositories persistem dados em SQLite", TestRepositoriesPersistInSqliteDatabase);
    }

    private static Task TestAuthorizationRequiresItems()
    {
        AssertThrows<ArgumentException>(() => CreateRequest(new List<RequestedItem>()));
        return Task.CompletedTask;
    }

    private static Task TestRequestedItemQuantity()
    {
        AssertThrows<ArgumentException>(() => new RequestedItem(Guid.NewGuid(), "Soro", 0));
        return Task.CompletedTask;
    }

    private static Task TestFullApproval()
    {
        var request = CreateRequest();

        request.ApproveFully();

        AssertEqual(AuthorizationStatus.AprovadaIntegralmente, request.Status);
        AssertTrue(request.Items.All(item => item.ApprovedQuantity == item.RequestedQuantity), "Todos os itens deveriam estar totalmente aprovados.");
        return Task.CompletedTask;
    }

    private static Task TestPartialApproval()
    {
        var request = CreateRequest();
        var firstItem = request.Items.First();
        var secondItem = request.Items.Last();

        request.ApprovePartially(new Dictionary<Guid, int> { [firstItem.Id] = 1 });

        AssertEqual(AuthorizationStatus.AprovadaParcialmente, request.Status);
        AssertEqual(1, firstItem.ApprovedQuantity);
        AssertEqual(0, secondItem.ApprovedQuantity);
        AssertThrows<InvalidOperationException>(() => request.ApproveFully());
        return Task.CompletedTask;
    }

    private static Task TestPartialApprovalRejectsInvalidQuantity()
    {
        var request = CreateRequest();
        var firstItem = request.Items.First();

        AssertThrows<ArgumentException>(() => request.ApprovePartially(new Dictionary<Guid, int> { [firstItem.Id] = firstItem.RequestedQuantity + 1 }));
        AssertEqual(AuthorizationStatus.Pendente, request.Status);
        AssertEqual(0, firstItem.ApprovedQuantity);
        return Task.CompletedTask;
    }

    private static Task TestDenialRequiresReason()
    {
        var request = CreateRequest();

        AssertThrows<ArgumentException>(() => request.Deny(GlosaReason.FaltaDeDocumentacao, ""));
        request.Deny(GlosaReason.FaltaDeDocumentacao, "Laudo clínico ausente.");

        AssertEqual(AuthorizationStatus.Negada, request.Status);
        AssertTrue(request.DenialReason?.Contains(nameof(GlosaReason.FaltaDeDocumentacao)) == true, "A negativa deveria registrar o motivo.");
        return Task.CompletedTask;
    }

    private static Task TestEmergencyRequiresPostPaymentAudit()
    {
        var request = AuthorizationRequestFactory.Create(
            Guid.NewGuid(),
            new PlanNumber("123456"),
            new ProcedureCode("8888"),
            new CidCode("S72.0"),
            new ProfessionalRegistry("CRM-54321"),
            "Pronto Atendimento BemAli",
            DateTime.Today,
            new List<RequestedItem> { new(Guid.NewGuid(), "Imobilizador", 1) },
            true);

        AssertEqual(AuthorizationStatus.AprovadaIntegralmente, request.Status);
        AssertTrue(request.RequiresPostPaymentAudit, "Urgência/emergência deve exigir auditoria posterior.");
        AssertTrue(request.Items.All(item => item.ApprovedQuantity == item.RequestedQuantity), "Itens de urgência deveriam estar aprovados.");
        return Task.CompletedTask;
    }

    private static Task TestEligibilityPlanMismatch()
    {
        var service = new EligibilityService();
        var beneficiary = CreateBeneficiary(Guid.NewGuid(), DateTime.Today.AddYears(-30), DateTime.Today.AddYears(-1));
        var plan = new Plan(Guid.NewGuid(), "999999", PlanType.Individual, 0);
        var procedure = CreateProcedure();

        AssertThrows<InvalidOperationException>(() => service.ValidateEligibility(beneficiary, plan, procedure, DateTime.Today));
        return Task.CompletedTask;
    }

    private static Task TestEligibilityGracePeriod()
    {
        var service = new EligibilityService();
        var planId = Guid.NewGuid();
        var beneficiary = CreateBeneficiary(planId, DateTime.Today.AddYears(-30), DateTime.Today.AddDays(-10));
        var plan = new Plan(planId, "123456", PlanType.Individual, 0);
        var procedure = CreateProcedure(type: ProcedureType.Cirurgia);

        plan.SetGracePeriod(ProcedureType.Cirurgia, 180);

        AssertThrows<InvalidOperationException>(() => service.ValidateEligibility(beneficiary, plan, procedure, DateTime.Today));
        return Task.CompletedTask;
    }

    private static Task TestEligibilityAge()
    {
        var service = new EligibilityService();
        var planId = Guid.NewGuid();
        var beneficiary = CreateBeneficiary(planId, DateTime.Today.AddYears(-10), DateTime.Today.AddYears(-1));
        var plan = new Plan(planId, "123456", PlanType.Individual, 0);
        var procedure = CreateProcedure(minimumAge: 18, maximumAge: 65);

        AssertThrows<InvalidOperationException>(() => service.ValidateEligibility(beneficiary, plan, procedure, DateTime.Today));
        return Task.CompletedTask;
    }

    private static Task TestEligibilityInactiveBeneficiary()
    {
        var service = new EligibilityService();
        var planId = Guid.NewGuid();
        var beneficiary = CreateBeneficiary(planId, DateTime.Today.AddYears(-30), DateTime.Today.AddYears(-1), BeneficiaryStatus.Inativo);
        var plan = new Plan(planId, "123456", PlanType.Individual, 0);
        var procedure = CreateProcedure();

        AssertThrows<InvalidOperationException>(() => service.ValidateEligibility(beneficiary, plan, procedure, DateTime.Today));
        return Task.CompletedTask;
    }

    private static Task TestEligibilityAllowed()
    {
        var service = new EligibilityService();
        var planId = Guid.NewGuid();
        var beneficiary = CreateBeneficiary(planId, DateTime.Today.AddYears(-30), DateTime.Today.AddYears(-1));
        var plan = new Plan(planId, "123456", PlanType.Individual, 0);
        var procedure = CreateProcedure(type: ProcedureType.ExameSimples, minimumAge: 18, maximumAge: 65);

        plan.SetGracePeriod(ProcedureType.ExameSimples, 30);
        service.ValidateEligibility(beneficiary, plan, procedure, DateTime.Today);

        return Task.CompletedTask;
    }

    private static Task TestValueObjectsRejectInvalidValues()
    {
        AssertThrows<ArgumentException>(() => new PlanNumber(""));
        AssertThrows<ArgumentException>(() => new ProcedureCode(""));
        AssertThrows<ArgumentException>(() => new CidCode("CID invalido"));
        AssertThrows<ArgumentException>(() => new ProfessionalRegistry(""));
        AssertThrows<ArgumentException>(() => new Evidence("", "Laudo"));
        return Task.CompletedTask;
    }

    private static Task TestAdministrativeAppealRejectsInvalidData()
    {
        var evidence = new List<Evidence>
        {
            new("https://example.test/laudo.pdf", "Laudo anexado")
        };

        AssertThrows<ArgumentException>(() =>
            AdministrativeAppeal.Restore(Guid.Empty, Guid.NewGuid(), evidence, AppealStatus.EmAnalise));
        AssertThrows<ArgumentException>(() =>
            AdministrativeAppeal.Restore(Guid.NewGuid(), Guid.Empty, evidence, AppealStatus.EmAnalise));
        AssertThrows<ArgumentException>(() =>
            AdministrativeAppeal.Restore(Guid.NewGuid(), Guid.NewGuid(), new List<Evidence>(), AppealStatus.EmAnalise));

        return Task.CompletedTask;
    }

    private static async Task TestApplicationServiceFlow()
    {
        var context = await CreateServicesWithReferenceDataAsync();
        var service = context.AuthorizationService;
        var dto = CreateAuthorizationDto(context.BeneficiaryId);

        var authorizationId = await service.RequestAuthorizationAsync(dto);
        var pending = await service.GetAuthorizationStatusAsync(authorizationId);

        AssertEqual(AuthorizationStatus.Pendente, pending.Status);
        AssertEqual(2, pending.Items.Count);

        await service.RegisterDocumentPendingAsync(authorizationId, "Laudo médico");
        var withPendingReason = await service.GetAuthorizationStatusAsync(authorizationId);
        AssertEqual("Laudo médico", withPendingReason.PendingReason);

        await service.DenyAuthorizationAsync(authorizationId, GlosaReason.FaltaDeDocumentacao, "Laudo médico não anexado.");
        var denied = await service.GetAuthorizationStatusAsync(authorizationId);

        AssertEqual(AuthorizationStatus.Negada, denied.Status);
        AssertTrue(denied.DenialReason?.Contains("Laudo médico não anexado.") == true, "A negativa deveria retornar detalhes.");
    }

    private static async Task TestApplicationRequiresEligibility()
    {
        var inactive = await CreateServicesWithReferenceDataAsync(status: BeneficiaryStatus.Inativo);
        await AssertThrowsAsync<InvalidOperationException>(() =>
            inactive.AuthorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(inactive.BeneficiaryId)));

        var urgentInactiveDto = CreateAuthorizationDto(inactive.BeneficiaryId);
        urgentInactiveDto.IsUrgentOrEmergency = true;
        await AssertThrowsAsync<InvalidOperationException>(() =>
            inactive.AuthorizationService.RequestAuthorizationAsync(urgentInactiveDto));

        var inGracePeriod = await CreateServicesWithReferenceDataAsync(joinDate: DateTime.Today.AddDays(-10), graceDays: 180);
        await AssertThrowsAsync<InvalidOperationException>(() =>
            inGracePeriod.AuthorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(inGracePeriod.BeneficiaryId)));

        var wrongAge = await CreateServicesWithReferenceDataAsync(procedureMinimumAge: 40, procedureMaximumAge: 65);
        await AssertThrowsAsync<InvalidOperationException>(() =>
            wrongAge.AuthorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(wrongAge.BeneficiaryId)));

        var planMismatch = await CreateServicesWithReferenceDataAsync();
        var alternativePlanRepository = new PlanRepository(planMismatch.DatabasePath);
        await alternativePlanRepository.AddAsync(new Plan(Guid.NewGuid(), "654321", PlanType.Individual, 0));
        var mismatchedPlanDto = CreateAuthorizationDto(planMismatch.BeneficiaryId);
        mismatchedPlanDto.PlanNumber = "654321";
        await AssertThrowsAsync<InvalidOperationException>(() =>
            planMismatch.AuthorizationService.RequestAuthorizationAsync(mismatchedPlanDto));
    }

    private static async Task TestBillingFullApprovalCreatesHospitalBill()
    {
        var context = await CreateServicesWithReferenceDataAsync();
        var authorizationService = context.AuthorizationService;
        var billingService = context.BillingService;
        var dto = CreateAuthorizationDto(context.BeneficiaryId);

        var authorizationId = await authorizationService.RequestAuthorizationAsync(dto);
        await authorizationService.ApproveAuthorizationAsync(authorizationId);

        var status = await authorizationService.GetAuthorizationStatusAsync(authorizationId);
        var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
        {
            AuthorizationId = authorizationId,
            UnitValuesByItemId = status.Items.ToDictionary(item => item.Id, item => MoneyDto(25m))
        });

        var bill = await billingService.GetHospitalBillAsync(billId);

        AssertEqual(dto.BeneficiaryId, bill.BeneficiaryId);
        AssertEqual(dto.ExecutingEstablishment, bill.ExecutingEstablishment);
        AssertEqual(2, bill.Items.Count);
        AssertTrue(bill.Items.All(item => item.ApprovedAuthorizationId == authorizationId), "Todos os itens deveriam apontar para a autorização aprovada.");
        AssertTrue(bill.Items.Any(item => item.Quantity == 2), "A quantidade solicitada deveria ser preservada no faturamento.");
        AssertEqual(75m, bill.TotalValue.Amount);
        AssertEqual("BRL", bill.TotalValue.Currency);
    }

    private static async Task TestBillingPartialApprovalUsesOnlyApprovedItems()
    {
        var context = await CreateServicesWithReferenceDataAsync();
        var authorizationService = context.AuthorizationService;
        var billingService = context.BillingService;
        var authorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(context.BeneficiaryId));

        var pending = await authorizationService.GetAuthorizationStatusAsync(authorizationId);
        var approvedItem = pending.Items.First();

        await authorizationService.ApproveAuthorizationPartiallyAsync(
            authorizationId,
            new Dictionary<Guid, int> { [approvedItem.Id] = 1 });

        var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
        {
            AuthorizationId = authorizationId,
            UnitValuesByItemId = new Dictionary<Guid, MoneyDto> { [approvedItem.Id] = MoneyDto(12.50m) }
        });

        var bill = await billingService.GetHospitalBillAsync(billId);
        var billItem = bill.Items.Single();

        AssertEqual(1, bill.Items.Count);
        AssertEqual(authorizationId, billItem.ApprovedAuthorizationId);
        AssertEqual(approvedItem.Description, billItem.Description);
        AssertEqual(1, billItem.Quantity);
        AssertEqual(12.50m, bill.TotalValue.Amount);
    }

    private static async Task TestBillingRejectsPendingOrDeniedAuthorization()
    {
        var context = await CreateServicesWithReferenceDataAsync();
        var authorizationService = context.AuthorizationService;
        var billingService = context.BillingService;

        var pendingAuthorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(context.BeneficiaryId));
        var pending = await authorizationService.GetAuthorizationStatusAsync(pendingAuthorizationId);

        await AssertThrowsAsync<InvalidOperationException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = pendingAuthorizationId,
                UnitValuesByItemId = pending.Items.ToDictionary(item => item.Id, item => MoneyDto(10m))
            }));

        var deniedAuthorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(context.BeneficiaryId));
        await authorizationService.DenyAuthorizationAsync(
            deniedAuthorizationId,
            GlosaReason.FaltaDeDocumentacao,
            "Laudo médico não anexado.");
        var denied = await authorizationService.GetAuthorizationStatusAsync(deniedAuthorizationId);

        await AssertThrowsAsync<InvalidOperationException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = deniedAuthorizationId,
                UnitValuesByItemId = denied.Items.ToDictionary(item => item.Id, item => MoneyDto(10m))
            }));
    }

    private static async Task TestBillingRequiresUnitValueForApprovedItems()
    {
        var context = await CreateServicesWithReferenceDataAsync();
        var authorizationService = context.AuthorizationService;
        var billingService = context.BillingService;

        var authorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(context.BeneficiaryId));
        await authorizationService.ApproveAuthorizationAsync(authorizationId);
        var status = await authorizationService.GetAuthorizationStatusAsync(authorizationId);

        await AssertThrowsAsync<ArgumentException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = authorizationId,
                UnitValuesByItemId = new Dictionary<Guid, MoneyDto>()
            }));

        await AssertThrowsAsync<ArgumentException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = authorizationId,
                UnitValuesByItemId = status.Items.ToDictionary(item => item.Id, item => MoneyDto(-1m))
            }));
    }

    private static Task TestMoneyOperations()
    {
        var first = new Money(10m, "brl");
        var second = new Money(15m, "BRL");

        AssertEqual(25m, first.Add(second).Amount);
        AssertEqual("BRL", first.Currency);
        AssertThrows<InvalidOperationException>(() => first.Add(new Money(1m, "USD")));
        AssertThrows<ArgumentException>(() => new Money(-1m));
        AssertThrows<ArgumentException>(() => new Money(1m, ""));
        return Task.CompletedTask;
    }

    private static Task TestHospitalBillCloseBlocksChanges()
    {
        var bill = new HospitalBill(Guid.NewGuid(), Guid.NewGuid(), "Hospital BemAli");
        var item = new BillItem(Guid.NewGuid(), Guid.NewGuid(), "Soro", 2, new Money(25m));

        bill.AddItem(item);
        AssertEqual(50m, bill.TotalValue.Amount);
        AssertThrows<InvalidOperationException>(() =>
            bill.AddItem(new BillItem(Guid.NewGuid(), Guid.NewGuid(), "Medicamento em dólar", 1, new Money(10m, "USD"))));

        bill.Close();

        AssertEqual(HospitalBillStatus.Closed, bill.Status);
        AssertThrows<InvalidOperationException>(() =>
            bill.AddItem(new BillItem(Guid.NewGuid(), Guid.NewGuid(), "Dipirona", 1, new Money(10m))));
        AssertThrows<InvalidOperationException>(() =>
            bill.ApplyGlosaToItem(item.Id, GlosaReason.FaltaDeDocumentacao, "Documento ausente."));
        return Task.CompletedTask;
    }

    private static async Task TestGlosaAppealFlow()
    {
        var context = await CreateServicesWithReferenceDataAsync();
        var authorizationService = context.AuthorizationService;
        var billingService = context.BillingService;

        var authorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto(context.BeneficiaryId));
        await authorizationService.ApproveAuthorizationAsync(authorizationId);
        var status = await authorizationService.GetAuthorizationStatusAsync(authorizationId);

        var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
        {
            AuthorizationId = authorizationId,
            UnitValuesByItemId = status.Items.ToDictionary(item => item.Id, item => MoneyDto(20m))
        });

        var bill = await billingService.GetHospitalBillAsync(billId);
        var itemId = bill.Items.First().Id;
        var glosaId = await billingService.ApplyGlosaToItemAsync(new ApplyGlosaDto
        {
            HospitalBillId = billId,
            BillItemId = itemId,
            Reason = GlosaReason.FaltaDeDocumentacao,
            Details = "Laudo ausente."
        });

        await AssertThrowsAsync<InvalidOperationException>(() =>
            billingService.ApplyGlosaToItemAsync(new ApplyGlosaDto
            {
                HospitalBillId = billId,
                BillItemId = Guid.NewGuid(),
                Reason = GlosaReason.FaltaDeDocumentacao,
                Details = "Item inexistente."
            }));

        await AssertThrowsAsync<ArgumentException>(() =>
            billingService.FileGlosaAppealAsync(new FileGlosaAppealDto
            {
                HospitalBillId = billId,
                BillItemId = itemId,
                GlosaId = glosaId,
                EvidenceDocuments = new List<EvidenceDto>()
            }));

        await billingService.FileGlosaAppealAsync(new FileGlosaAppealDto
        {
            HospitalBillId = billId,
            BillItemId = itemId,
            GlosaId = glosaId,
            EvidenceDocuments = new List<EvidenceDto>
            {
                new()
                {
                    DocumentUrl = "https://example.test/laudo.pdf",
                    Description = "Laudo anexado"
                }
            }
        });

        await AssertThrowsAsync<InvalidOperationException>(() =>
            billingService.FileGlosaAppealAsync(new FileGlosaAppealDto
            {
                HospitalBillId = billId,
                BillItemId = itemId,
                GlosaId = glosaId,
                EvidenceDocuments = new List<EvidenceDto>
                {
                    new()
                    {
                        DocumentUrl = "https://example.test/laudo-duplicado.pdf",
                        Description = "Duplicado"
                    }
                }
            }));

        var reloaded = await billingService.GetHospitalBillAsync(billId);
        var reloadedGlosa = reloaded.Items.SelectMany(item => item.Glosas).Single(glosa => glosa.Id == glosaId);

        AssertTrue(reloadedGlosa.Appeal != null, "A glosa deveria ter recurso administrativo persistido.");
        AssertEqual(1, reloadedGlosa.Appeal!.EvidenceDocuments.Count);
        AssertEqual("https://example.test/laudo.pdf", reloadedGlosa.Appeal.EvidenceDocuments.Single().DocumentUrl);
    }

    private static async Task TestRepositoriesPersistInSqliteDatabase()
    {
        var databasePath = CreateTestDatabasePath();

        try
        {
            var authorizationRepository = new AuthorizationRepository(databasePath);
            var hospitalBillRepository = new HospitalBillRepository(databasePath);
            var beneficiaryRepository = new BeneficiaryRepository(databasePath);
            var planRepository = new PlanRepository(databasePath);
            var procedureRepository = new ProcedureCatalogRepository(databasePath);
            var beneficiaryId = await SeedReferenceDataAsync(
                beneficiaryRepository,
                planRepository,
                procedureRepository);
            var authorizationService = CreateAuthorizationService(
                authorizationRepository,
                beneficiaryRepository,
                planRepository,
                procedureRepository);
            var billingService = CreateBillingService(authorizationRepository, hospitalBillRepository);
            var dto = CreateAuthorizationDto(beneficiaryId);

            var authorizationId = await authorizationService.RequestAuthorizationAsync(dto);
            await authorizationService.ApproveAuthorizationAsync(authorizationId);
            var status = await authorizationService.GetAuthorizationStatusAsync(authorizationId);

            var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = authorizationId,
                UnitValuesByItemId = status.Items.ToDictionary(item => item.Id, item => MoneyDto(25m, "USD"))
            });

            await billingService.CloseHospitalBillAsync(billId);

            var reloadedAuthorizationRepository = new AuthorizationRepository(databasePath);
            var reloadedHospitalBillRepository = new HospitalBillRepository(databasePath);

            var reloadedAuthorization = await reloadedAuthorizationRepository.GetByIdAsync(authorizationId);
            var reloadedBill = await reloadedHospitalBillRepository.GetByIdAsync(billId);

            AssertTrue(reloadedAuthorization != null, "A autorização deveria ser recarregada do SQLite.");
            AssertTrue(reloadedBill != null, "A conta hospitalar deveria ser recarregada do SQLite.");
            AssertEqual(AuthorizationStatus.AprovadaIntegralmente, reloadedAuthorization!.Status);
            AssertEqual(2, reloadedAuthorization.Items.Count);
            AssertTrue(reloadedAuthorization.Items.All(item => item.ApprovedQuantity == item.RequestedQuantity), "As quantidades aprovadas deveriam persistir.");
            AssertEqual(dto.BeneficiaryId, reloadedBill!.BeneficiaryId);
            AssertEqual(2, reloadedBill.Items.Count);
            AssertEqual(75m, reloadedBill.TotalValue.Amount);
            AssertEqual("USD", reloadedBill.TotalValue.Currency);
            AssertTrue(reloadedBill.Items.All(item => item.UnitValue.Currency == "USD"), "A moeda dos itens deveria persistir.");
            AssertEqual(HospitalBillStatus.Closed, reloadedBill.Status);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    private static async Task<(
        AuthorizationService AuthorizationService,
        BillingService BillingService,
        Guid BeneficiaryId,
        string DatabasePath)> CreateServicesWithReferenceDataAsync(
        BeneficiaryStatus status = BeneficiaryStatus.Ativo,
        DateTime? joinDate = null,
        int graceDays = 30,
        int? procedureMinimumAge = 18,
        int? procedureMaximumAge = 65)
    {
        var databasePath = CreateTestDatabasePath();
        var authorizationRepository = new AuthorizationRepository(databasePath);
        var hospitalBillRepository = new HospitalBillRepository(databasePath);
        var beneficiaryRepository = new BeneficiaryRepository(databasePath);
        var planRepository = new PlanRepository(databasePath);
        var procedureRepository = new ProcedureCatalogRepository(databasePath);

        var beneficiaryId = await SeedReferenceDataAsync(
            beneficiaryRepository,
            planRepository,
            procedureRepository,
            status,
            joinDate,
            graceDays,
            procedureMinimumAge,
            procedureMaximumAge);

        return (
            CreateAuthorizationService(
                authorizationRepository,
                beneficiaryRepository,
                planRepository,
                procedureRepository),
            CreateBillingService(authorizationRepository, hospitalBillRepository),
            beneficiaryId,
            databasePath);
    }

    private static AuthorizationService CreateAuthorizationService(
        IAuthorizationRepository authorizationRepository,
        IBeneficiaryRepository beneficiaryRepository,
        IPlanRepository planRepository,
        IProcedureCatalogRepository procedureRepository)
    {
        return new AuthorizationService(
            new RequestAuthorizationUseCase(
                authorizationRepository,
                beneficiaryRepository,
                planRepository,
                procedureRepository,
                new EligibilityService()),
            new ApproveAuthorizationUseCase(authorizationRepository),
            new ApproveAuthorizationPartiallyUseCase(authorizationRepository),
            new DenyAuthorizationUseCase(authorizationRepository),
            new RegisterDocumentPendingUseCase(authorizationRepository),
            new GetAuthorizationStatusUseCase(authorizationRepository));
    }

    private static BillingService CreateBillingService(
        IAuthorizationRepository authorizationRepository,
        IHospitalBillRepository hospitalBillRepository)
    {
        return new BillingService(
            new CreateHospitalBillFromAuthorizationUseCase(authorizationRepository, hospitalBillRepository),
            new GetHospitalBillUseCase(hospitalBillRepository),
            new ApplyGlosaToHospitalBillItemUseCase(hospitalBillRepository),
            new FileGlosaAppealUseCase(hospitalBillRepository),
            new CloseHospitalBillUseCase(hospitalBillRepository));
    }

    private static async Task<Guid> SeedReferenceDataAsync(
        BeneficiaryRepository beneficiaryRepository,
        PlanRepository planRepository,
        ProcedureCatalogRepository procedureRepository,
        BeneficiaryStatus status = BeneficiaryStatus.Ativo,
        DateTime? joinDate = null,
        int graceDays = 30,
        int? procedureMinimumAge = 18,
        int? procedureMaximumAge = 65)
    {
        var planId = Guid.NewGuid();
        var plan = new Plan(planId, "123456", PlanType.Individual, 0);
        plan.SetGracePeriod(ProcedureType.ExameSimples, graceDays);

        var beneficiary = new Beneficiary(
            Guid.NewGuid(),
            "Maria Silva",
            DateTime.Today.AddYears(-30),
            joinDate ?? DateTime.Today.AddYears(-1),
            planId,
            status);

        var procedure = new ProcedureCatalogItem(
            new ProcedureCode("9999"),
            "Procedimento de teste",
            ProcedureType.ExameSimples,
            procedureMinimumAge,
            procedureMaximumAge);

        await planRepository.AddAsync(plan);
        await beneficiaryRepository.AddAsync(beneficiary);
        await procedureRepository.AddAsync(procedure);

        return beneficiary.Id;
    }

    private static AuthorizationRequestDto CreateAuthorizationDto(Guid beneficiaryId)
    {
        return new AuthorizationRequestDto
        {
            BeneficiaryId = beneficiaryId,
            PlanNumber = "123456",
            ProcedureCode = "9999",
            CidCode = "M54.5",
            RequestingProfessional = "CRM-12345",
            ExecutingEstablishment = "Hospital BemAli",
            ExpectedDate = DateTime.Today.AddDays(1),
            RequestedItems = new List<RequestedItemDto>
            {
                new() { Description = "Soro", Quantity = 2, ItemType = "Material" },
                new() { Description = "Dipirona", Quantity = 1, ItemType = "Medicamento" }
            },
            IsUrgentOrEmergency = false
        };
    }

    private static MoneyDto MoneyDto(decimal amount, string currency = "BRL")
    {
        return new MoneyDto
        {
            Amount = amount,
            Currency = currency
        };
    }

    private static string CreateTestDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"ddd-case-1-{Guid.NewGuid():N}.db");
    }

    private static void DeleteDatabaseFiles(string databasePath)
    {
        foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static AuthorizationRequest CreateRequest()
    {
        return CreateRequest(new List<RequestedItem>
        {
            new(Guid.NewGuid(), "Soro", 2),
            new(Guid.NewGuid(), "Dipirona", 1)
        });
    }

    private static AuthorizationRequest CreateRequest(List<RequestedItem> items)
    {
        return new AuthorizationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PlanNumber("123456"),
            new ProcedureCode("9999"),
            new CidCode("M54.5"),
            new ProfessionalRegistry("CRM-12345"),
            "Hospital BemAli",
            DateTime.Today.AddDays(1),
            items,
            false);
    }

    private static Beneficiary CreateBeneficiary(Guid planId, DateTime birthDate, DateTime joinDate, BeneficiaryStatus status = BeneficiaryStatus.Ativo)
    {
        return new Beneficiary(Guid.NewGuid(), "Maria Silva", birthDate, joinDate, planId, status);
    }

    private static ProcedureCatalogItem CreateProcedure(
        ProcedureType type = ProcedureType.ExameSimples,
        int? minimumAge = null,
        int? maximumAge = null)
    {
        return new ProcedureCatalogItem(new ProcedureCode("9999"), "Procedimento de teste", type, minimumAge, maximumAge);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Esperado: {expected}. Atual: {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Esperava {typeof(TException).Name}, mas recebeu {ex.GetType().Name}.");
        }

        throw new InvalidOperationException($"Esperava exceção {typeof(TException).Name}.");
    }

    private static async Task AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Esperava {typeof(TException).Name}, mas recebeu {ex.GetType().Name}.");
        }

        throw new InvalidOperationException($"Esperava exceção {typeof(TException).Name}.");
    }
}
