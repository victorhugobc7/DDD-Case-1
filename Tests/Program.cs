using Application.DTOs;
using Application.Services;
using Domain.Modules.Auditoria;
using Domain.Modules.Autorizacoes;
using Domain.Modules.Beneficiarios;
using Domain.Modules.Planos;
using Domain.Modules.Procedimentos;
using Domain.Modules.RedeCredenciada;
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
            "123456",
            "8888",
            "S72.0",
            "CRM-54321",
            "Pronto Atendimento BemAli",
            DateTime.Today,
            new List<string> { "Imobilizador" },
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
        var repository = new AuthorizationRepository();
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
}
