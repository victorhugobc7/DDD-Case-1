using System;

namespace Domain.Modules.Beneficiarios;

public class Beneficiary
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTime BirthDate { get; private set; }
    public DateTime JoinDate { get; private set; }
    public Guid PlanId { get; private set; }
    public BeneficiaryStatus Status { get; private set; }
    
    public Beneficiary(Guid id, string name, DateTime birthDate, DateTime joinDate, Guid planId, BeneficiaryStatus status = BeneficiaryStatus.Ativo)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O id do beneficiário é inválido.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do beneficiário não pode ser vazio.", nameof(name));
        if (birthDate.Date > DateTime.Today)
            throw new ArgumentException("A data de nascimento não pode ser futura.", nameof(birthDate));
        if (planId == Guid.Empty)
            throw new ArgumentException("O id do plano é inválido.", nameof(planId));

        Id = id;
        Name = name;
        BirthDate = birthDate;
        JoinDate = joinDate;
        PlanId = planId;
        Status = status;
    }

    public int CalculateAge(DateTime currentDate)
    {
        var age = currentDate.Year - BirthDate.Year;
        if (currentDate.Date < BirthDate.Date.AddYears(age))
        {
            age--;
        }
        return age;
    }

    public void ChangePlan(Guid newPlanId)
    {
        if (newPlanId == Guid.Empty)
            throw new ArgumentException("O id do plano é inválido.", nameof(newPlanId));

        PlanId = newPlanId;
    }

    public void Activate()
    {
        Status = BeneficiaryStatus.Ativo;
    }

    public void Suspend()
    {
        Status = BeneficiaryStatus.Inativo;
    }
}
