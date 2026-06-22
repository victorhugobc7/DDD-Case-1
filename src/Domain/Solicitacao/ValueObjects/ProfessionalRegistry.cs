using System;
using Domain.Common;

namespace Domain.Solicitacao.ValueObjects;

public record ProfessionalRegistry : ValueObject
{
    public string Value { get; }

    public ProfessionalRegistry(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("O registro profissional não pode ser vazio.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}
