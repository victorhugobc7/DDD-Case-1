using System;

namespace Domain.ValueObjects.RedeCredenciada;

public record ProfessionalRegistry
{
    public string Value { get; init; }

    public ProfessionalRegistry(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("O registro profissional não pode ser vazio.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}
