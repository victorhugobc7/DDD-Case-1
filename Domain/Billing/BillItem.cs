using System;
using System.Collections.Generic;
using Domain.Audit;
using Domain.Billing;

namespace Domain.Billing;

public class BillItem
{
    public Guid Id { get; private set; }
    public Guid ApprovedAuthorizationId { get; private set; }
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitValue { get; private set; }
    public Money TotalValue => UnitValue.Multiply(Quantity);

    private readonly List<Glosa> _glosas;
    public IReadOnlyCollection<Glosa> Glosas => _glosas.AsReadOnly();

    public BillItem(Guid id, Guid approvedAuthorizationId, string description, int quantity, Money unitValue)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O id do item de conta é inválido.", nameof(id));
        if (approvedAuthorizationId == Guid.Empty)
            throw new ArgumentException("Um item de conta deve estar ligado a uma Autorização Aprovada válida.", nameof(approvedAuthorizationId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição do item de conta não pode ser vazia.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentException("A quantidade do item de conta deve ser maior que zero.", nameof(quantity));
        if (unitValue == null)
            throw new ArgumentNullException(nameof(unitValue));

        Id = id;
        ApprovedAuthorizationId = approvedAuthorizationId;
        Description = description;
        Quantity = quantity;
        UnitValue = unitValue;
        _glosas = new List<Glosa>();
    }

    public static BillItem Restore(
        Guid id,
        Guid approvedAuthorizationId,
        string description,
        int quantity,
        Money unitValue,
        List<Glosa> glosas)
    {
        var item = new BillItem(id, approvedAuthorizationId, description, quantity, unitValue);

        if (glosas != null)
            item._glosas.AddRange(glosas);

        return item;
    }

    internal Glosa ApplyGlosa(GlosaReason reason, string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("A glosa deve possuir justificativa.", nameof(details));

        var glosa = new Glosa(Guid.NewGuid(), Id, reason, details, false);
        _glosas.Add(glosa);

        return glosa;
    }

    internal Glosa ApplyClawbackAuditGlosa(GlosaReason reason, string auditDetails)
    {
        if (string.IsNullOrWhiteSpace(auditDetails))
            throw new ArgumentException("A glosa de auditoria deve possuir justificativa.", nameof(auditDetails));

        var glosa = new Glosa(Guid.NewGuid(), Id, reason, auditDetails, true);
        _glosas.Add(glosa);

        return glosa;
    }

    internal void FileAppeal(Guid glosaId, List<Evidence> evidenceDocuments)
    {
        var glosa = _glosas.Find(x => x.Id == glosaId)
            ?? throw new InvalidOperationException("Glosa não encontrada para o item informado.");

        glosa.FileAppeal(evidenceDocuments);
    }
}
