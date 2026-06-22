using System;
using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Common.Enums;

namespace Domain.Procedimento;

public class ProcedureCatalogItem : AggregateRoot
{
    public ProcedureCode Code { get; private set; }
    public string Description { get; private set; }
    public ProcedureType Type { get; private set; }
    public int? MinimumAge { get; private set; }
    public int? MaximumAge { get; private set; }

    public ProcedureCatalogItem(Guid id, ProcedureCode code, string description, ProcedureType type, int? minimumAge = null, int? maximumAge = null)
        : base(id)
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
