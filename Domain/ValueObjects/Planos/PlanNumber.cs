using System;

namespace Domain.ValueObjects.Planos;

public record PlanNumber
{
    public string Value { get; init; }

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
