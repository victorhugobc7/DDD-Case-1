using System;
using System.Text.RegularExpressions;

namespace Domain.Modules.Procedimentos;

public record CidCode
{
    public string Value { get; init; }

    public CidCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("O código CID não pode ser vazio.", nameof(value));

        if (!Regex.IsMatch(value, @"^[A-Z][0-9]{2}(\.[0-9])?$"))
            throw new ArgumentException("O código CID fornecido é inválido.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}
