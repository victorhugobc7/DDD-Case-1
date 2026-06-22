using System;
using Domain.Common;

namespace Domain.Common.ValueObjects;

public record PlanNumber : ValueObject
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
