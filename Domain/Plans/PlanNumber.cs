using System;

namespace Domain.Plans;

public record PlanNumber
{
    public string Value { get; }

    public PlanNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O número do plano não pode ser nulo.", nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value;
}
