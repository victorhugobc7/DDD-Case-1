using System;
using Domain.Procedures;

namespace Domain.Procedures;

public class ProcedureCatalogItem
{
    public ProcedureCode Code { get; private set; }
    public string Description { get; private set; }
    public ProcedureType Type { get; private set; }
    public int? MinimumAge { get; private set; }
    public int? MaximumAge { get; private set; }

    public ProcedureCatalogItem(ProcedureCode code, string description, ProcedureType type, int? minimumAge, int? maximumAge)
    {
        if (code == null)
            throw new ArgumentNullException(nameof(code));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição do procedimento não pode ser vazia.", nameof(description));
        if (minimumAge.HasValue && maximumAge.HasValue && minimumAge.Value > maximumAge.Value)
            throw new ArgumentException("A idade mínima não pode ser maior que a idade máxima.");

        Code = code;
        Description = description;
        Type = type;
        MinimumAge = minimumAge;
        MaximumAge = maximumAge;
    }

    public bool IsAgePermitted(int age)
    {
        if (MinimumAge.HasValue && age < MinimumAge.Value) return false;
        if (MaximumAge.HasValue && age > MaximumAge.Value) return false;
        return true;
    }
}
