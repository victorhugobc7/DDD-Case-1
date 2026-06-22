using System;
using System.Collections.Generic;
using Domain.Common;
using Domain.Common.Enums;
using Domain.Faturamento.ValueObjects;
using Domain.Faturamento.Enums;

namespace Domain.Faturamento.Entities;

public class Glosa : Entity
{
    public Guid BillItemId { get; private set; }
    public GlosaReason Reason { get; private set; }
    public string Details { get; private set; }
    public AdministrativeAppeal? Appeal { get; private set; }
    public bool IsClawback { get; private set; }

    internal Glosa(Guid id, Guid billItemId, GlosaReason reason, string details, bool isClawback = false)
        : base(id)
    {
        if (billItemId == Guid.Empty)
            throw new ArgumentException("O item glosado é inválido.", nameof(billItemId));
        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("A glosa deve possuir justificativa.", nameof(details));

        BillItemId = billItemId;
        Reason = reason;
        Details = details;
        IsClawback = isClawback;
        Appeal = null;
    }

    public static Glosa Restore(
        Guid id,
        Guid billItemId,
        GlosaReason reason,
        string details,
        bool isClawback,
        AdministrativeAppeal? appeal = null)
    {
        var glosa = new Glosa(id, billItemId, reason, details, isClawback);
        glosa.Appeal = appeal;

        return glosa;
    }

    internal void FileAppeal(List<Evidence> evidenceDocuments)
    {
        if (Appeal != null)
        {
            throw new InvalidOperationException("Esta glosa já possui um recurso ativo associado. Apenas um recurso é permitido por negativa.");
        }

        Appeal = new AdministrativeAppeal(Guid.NewGuid(), Id, evidenceDocuments);
    }
}
