using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Domain.Entities;

public class Plan
{
    public Guid Id { get; private set; }
    public string Number { get; private set; }
    public PlanType Type { get; private set; }
    public decimal CopayPercentage { get; private set; }
    private readonly Dictionary<ProcedureType, int> _gracePeriodsInDays;
    public IReadOnlyDictionary<ProcedureType, int> GracePeriodsInDays => _gracePeriodsInDays;
    
    public Plan(Guid id, string number, PlanType type, decimal copayPercentage)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("O número do plano não pode ser vazio.", nameof(number));
        if (copayPercentage < 0 || copayPercentage > 100)
            throw new ArgumentException("A porcentagem de coparticipação deve estar entre 0 e 100.", nameof(copayPercentage));

        Id = id;
        Number = number;
        Type = type;
        CopayPercentage = copayPercentage;
        _gracePeriodsInDays = new Dictionary<ProcedureType, int>();
    }

    public void SetGracePeriod(ProcedureType procedureType, int days)
    {
        if (days < 0)
            throw new ArgumentException("A carência não pode ser negativa.", nameof(days));

        _gracePeriodsInDays[procedureType] = days;
    }

    public bool IsGracePeriodFulfilled(ProcedureType procedureType, DateTime joinDate, DateTime requestDate)
    {
        if (!_gracePeriodsInDays.TryGetValue(procedureType, out var daysRequired))
        {
            return true;
        }

        return (requestDate - joinDate).TotalDays >= daysRequired;
    }
}
