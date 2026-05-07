using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;

namespace Domain.Entities;

public class BillItem
{
    public Guid Id { get; private set; }
    public Guid ApprovedAuthorizationId { get; private set; }
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitValue { get; private set; }
    public decimal TotalValue => Quantity * UnitValue;

    private readonly List<Glosa> _glosas;
    public IReadOnlyCollection<Glosa> Glosas => _glosas.AsReadOnly();

    public BillItem(Guid id, Guid approvedAuthorizationId, string description, int quantity, decimal unitValue)
    {
        if (approvedAuthorizationId == Guid.Empty)
            throw new ArgumentException("Um item de conta deve estar ligado a uma Autorização Aprovada válida.", nameof(approvedAuthorizationId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição do item de conta não pode ser vazia.", nameof(description));

        Id = id;
        ApprovedAuthorizationId = approvedAuthorizationId;
        Description = description;
        Quantity = quantity;
        UnitValue = unitValue;
        _glosas = new List<Glosa>();
    }

    public void ApplyGlosa(GlosaReason reason, string details)
    {
        _glosas.Add(new Glosa(Guid.NewGuid(), Id, reason, details, false));
    }

    public void ApplyClawbackAuditGlosa(GlosaReason reason, string auditDetails)
    {
        _glosas.Add(new Glosa(Guid.NewGuid(), Id, reason, auditDetails, true));
    }
}
