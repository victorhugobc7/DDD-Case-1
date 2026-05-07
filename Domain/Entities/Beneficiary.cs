using System;

namespace Domain.Entities;

public class Beneficiary
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTime BirthDate { get; private set; }
    public DateTime JoinDate { get; private set; }
    public Guid PlanId { get; private set; }
    
    public Beneficiary(Guid id, string name, DateTime birthDate, DateTime joinDate, Guid planId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do beneficiário não pode ser vazio.", nameof(name));
        if (planId == Guid.Empty)
            throw new ArgumentException("O id do plano é inválido.", nameof(planId));

        Id = id;
        Name = name;
        BirthDate = birthDate;
        JoinDate = joinDate;
        PlanId = planId;
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
}
