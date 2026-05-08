using System;
using Domain.Modules.Beneficiarios;
using Domain.Modules.Planos;
using Domain.Modules.Procedimentos;

namespace Domain.Modules.Autorizacoes;

public class EligibilityService
{
    public void ValidateEligibility(Beneficiary beneficiary, Plan plan, ProcedureCatalogItem procedure, DateTime requestDate)
    {
        if (beneficiary == null)
            throw new ArgumentNullException(nameof(beneficiary));
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));
        if (procedure == null)
            throw new ArgumentNullException(nameof(procedure));

        if (beneficiary.Status != BeneficiaryStatus.Ativo)
            throw new InvalidOperationException("Beneficiário inativo não pode ter autorização aprovada.");

        if (beneficiary.PlanId != plan.Id)
            throw new InvalidOperationException("O beneficiário não pertence ao plano informado.");

        if (!plan.IsGracePeriodFulfilled(procedure.Type, beneficiary.JoinDate, requestDate))
            throw new InvalidOperationException("O beneficiário ainda está em período de carência para este procedimento.");

        var age = beneficiary.CalculateAge(requestDate);
        if (!procedure.IsAgePermitted(age))
            throw new InvalidOperationException("A idade do beneficiário não é permitida para este procedimento.");
    }
}
