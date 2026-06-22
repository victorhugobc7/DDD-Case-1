using System;
using Domain.Common;

namespace Domain.Common.ValueObjects;

public record ProcedureCode : ValueObject
{
    public string Value { get; }

    public ProcedureCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O código do procedimento não pode ser vazio.", nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value;
}
