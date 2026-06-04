using System;

namespace Domain.Procedures;

public record ProcedureCode
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
