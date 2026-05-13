using Application.DTOs;
using Application.Services;
using Domain.Aggregates.Autorizacoes;
using Domain.Aggregates.Beneficiarios;
using Domain.Aggregates.Planos;
using Domain.Aggregates.Procedimentos;
using Domain.Enums.Auditoria;
using Domain.Enums.Autorizacoes;
using Domain.Enums.Beneficiarios;
using Domain.Enums.Planos;
using Domain.Enums.Procedimentos;
using Domain.Factories.Autorizacoes;
using Domain.Services.Autorizacoes;
using Domain.ValueObjects.Planos;
using Domain.ValueObjects.Procedimentos;
using Domain.ValueObjects.RedeCredenciada;
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
        yield return ("application coordena factory e repository", TestApplicationServiceFlow);
        yield return ("faturamento cria conta para autorização aprovada integralmente", TestBillingFullApprovalCreatesHospitalBill);
        yield return ("faturamento parcial usa somente itens aprovados", TestBillingPartialApprovalUsesOnlyApprovedItems);
        yield return ("faturamento rejeita autorização pendente ou negada", TestBillingRejectsPendingOrDeniedAuthorization);
        yield return ("faturamento exige valor unitário válido por item aprovado", TestBillingRequiresUnitValueForApprovedItems);
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

    private static async Task TestApplicationServiceFlow()
    {
        var repository = new AuthorizationRepository(CreateTestDatabasePath());
        var service = new AuthorizationService(repository);
        var dto = new AuthorizationRequestDto
        {
            BeneficiaryId = Guid.NewGuid(),
            PlanNumber = "123456",
            ProcedureCode = "9999",
            ClinicalJustification = "M54.5",
            RequestingProfessional = "CRM-12345",
            ExecutingEstablishment = "Hospital BemAli",
            ExpectedDate = DateTime.Today.AddDays(1),
            MaterialsAndMedicines = new List<string> { "Soro", "Dipirona" },
            IsUrgentOrEmergency = false
        };

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

    private static async Task TestBillingFullApprovalCreatesHospitalBill()
    {
        var (authorizationService, billingService) = CreateBillingServices();
        var dto = CreateAuthorizationDto();

        var authorizationId = await authorizationService.RequestAuthorizationAsync(dto);
        await authorizationService.ApproveAuthorizationAsync(authorizationId);

        var status = await authorizationService.GetAuthorizationStatusAsync(authorizationId);
        var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
        {
            AuthorizationId = authorizationId,
            UnitValuesByItemId = status.Items.ToDictionary(item => item.Id, item => 25m)
        });

        var bill = await billingService.GetHospitalBillAsync(billId);

        AssertEqual(dto.BeneficiaryId, bill.BeneficiaryId);
        AssertEqual(dto.ExecutingEstablishment, bill.ExecutingEstablishment);
        AssertEqual(2, bill.Items.Count);
        AssertTrue(bill.Items.All(item => item.ApprovedAuthorizationId == authorizationId), "Todos os itens deveriam apontar para a autorização aprovada.");
        AssertTrue(bill.Items.All(item => item.Quantity == 1), "Todos os itens deveriam faturar a quantidade aprovada.");
        AssertEqual(50m, bill.TotalValue);
    }

    private static async Task TestBillingPartialApprovalUsesOnlyApprovedItems()
    {
        var (authorizationService, billingService) = CreateBillingServices();
        var authorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto());

        var pending = await authorizationService.GetAuthorizationStatusAsync(authorizationId);
        var approvedItem = pending.Items.First();

        await authorizationService.ApproveAuthorizationPartiallyAsync(
            authorizationId,
            new Dictionary<Guid, int> { [approvedItem.Id] = 1 });

        var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
        {
            AuthorizationId = authorizationId,
            UnitValuesByItemId = new Dictionary<Guid, decimal> { [approvedItem.Id] = 12.50m }
        });

        var bill = await billingService.GetHospitalBillAsync(billId);
        var billItem = bill.Items.Single();

        AssertEqual(1, bill.Items.Count);
        AssertEqual(authorizationId, billItem.ApprovedAuthorizationId);
        AssertEqual(approvedItem.Description, billItem.Description);
        AssertEqual(1, billItem.Quantity);
        AssertEqual(12.50m, bill.TotalValue);
    }

    private static async Task TestBillingRejectsPendingOrDeniedAuthorization()
    {
        var (authorizationService, billingService) = CreateBillingServices();

        var pendingAuthorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto());
        var pending = await authorizationService.GetAuthorizationStatusAsync(pendingAuthorizationId);

        await AssertThrowsAsync<InvalidOperationException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = pendingAuthorizationId,
                UnitValuesByItemId = pending.Items.ToDictionary(item => item.Id, item => 10m)
            }));

        var deniedAuthorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto());
        await authorizationService.DenyAuthorizationAsync(
            deniedAuthorizationId,
            GlosaReason.FaltaDeDocumentacao,
            "Laudo médico não anexado.");
        var denied = await authorizationService.GetAuthorizationStatusAsync(deniedAuthorizationId);

        await AssertThrowsAsync<InvalidOperationException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = deniedAuthorizationId,
                UnitValuesByItemId = denied.Items.ToDictionary(item => item.Id, item => 10m)
            }));
    }

    private static async Task TestBillingRequiresUnitValueForApprovedItems()
    {
        var (authorizationService, billingService) = CreateBillingServices();

        var authorizationId = await authorizationService.RequestAuthorizationAsync(CreateAuthorizationDto());
        await authorizationService.ApproveAuthorizationAsync(authorizationId);
        var status = await authorizationService.GetAuthorizationStatusAsync(authorizationId);

        await AssertThrowsAsync<ArgumentException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = authorizationId,
                UnitValuesByItemId = new Dictionary<Guid, decimal>()
            }));

        await AssertThrowsAsync<ArgumentException>(() =>
            billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = authorizationId,
                UnitValuesByItemId = status.Items.ToDictionary(item => item.Id, item => -1m)
            }));
    }

    private static async Task TestRepositoriesPersistInSqliteDatabase()
    {
        var databasePath = CreateTestDatabasePath();

        try
        {
            var authorizationRepository = new AuthorizationRepository(databasePath);
            var hospitalBillRepository = new HospitalBillRepository(databasePath);
            var authorizationService = new AuthorizationService(authorizationRepository);
            var billingService = new BillingService(authorizationRepository, hospitalBillRepository);
            var dto = CreateAuthorizationDto();

            var authorizationId = await authorizationService.RequestAuthorizationAsync(dto);
            await authorizationService.ApproveAuthorizationAsync(authorizationId);
            var status = await authorizationService.GetAuthorizationStatusAsync(authorizationId);

            var billId = await billingService.CreateHospitalBillFromAuthorizationAsync(new CreateHospitalBillDto
            {
                AuthorizationId = authorizationId,
                UnitValuesByItemId = status.Items.ToDictionary(item => item.Id, item => 25m)
            });

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
            AssertEqual(50m, reloadedBill.Items.Sum(item => item.TotalValue));
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    private static (AuthorizationService AuthorizationService, BillingService BillingService) CreateBillingServices()
    {
        var databasePath = CreateTestDatabasePath();
        var authorizationRepository = new AuthorizationRepository(databasePath);
        var hospitalBillRepository = new HospitalBillRepository(databasePath);

        return (
            new AuthorizationService(authorizationRepository),
            new BillingService(authorizationRepository, hospitalBillRepository));
    }

    private static AuthorizationRequestDto CreateAuthorizationDto()
    {
        return new AuthorizationRequestDto
        {
            BeneficiaryId = Guid.NewGuid(),
            PlanNumber = "123456",
            ProcedureCode = "9999",
            ClinicalJustification = "M54.5",
            RequestingProfessional = "CRM-12345",
            ExecutingEstablishment = "Hospital BemAli",
            ExpectedDate = DateTime.Today.AddDays(1),
            MaterialsAndMedicines = new List<string> { "Soro", "Dipirona" },
            IsUrgentOrEmergency = false
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
