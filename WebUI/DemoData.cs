using Domain.Beneficiaries;
using Domain.Plans;
using Domain.Procedures;

namespace WebUI;

public static class DemoData
{
    public static readonly Guid BeneficiaryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid PlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string PlanNumber = "123456";
    public const string ProcedureCode = "9999";
    public const string CidCode = "M54.5";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var beneficiaryRepository = services.GetRequiredService<IBeneficiaryRepository>();
        var planRepository = services.GetRequiredService<IPlanRepository>();
        var procedureRepository = services.GetRequiredService<IProcedureCatalogRepository>();

        var plan = new Plan(PlanId, PlanNumber, PlanType.Individual, 0);
        plan.SetGracePeriod(ProcedureType.ExameSimples, 30);

        await planRepository.AddAsync(plan);
        await beneficiaryRepository.AddAsync(new Beneficiary(
            BeneficiaryId,
            "Maria Silva",
            DateTime.Today.AddYears(-30),
            DateTime.Today.AddYears(-1),
            PlanId));

        await procedureRepository.AddAsync(new ProcedureCatalogItem(
            new ProcedureCode(ProcedureCode),
            "Procedimento eletivo de demonstração",
            ProcedureType.ExameSimples,
            18,
            65));
    }
}
