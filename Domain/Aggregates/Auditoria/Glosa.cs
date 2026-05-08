using System;
using System.Collections.Generic;
using Domain.Enums.Auditoria;
using Domain.ValueObjects.Auditoria;

namespace Domain.Aggregates.Auditoria;

public class Glosa
{
    public Guid Id { get; private set; }
    public Guid BillItemId { get; private set; }
    public GlosaReason Reason { get; private set; }
    public string Details { get; private set; }
    public AdministrativeAppeal? Appeal { get; private set; }
    public bool IsClawback { get; private set; }

    internal Glosa(Guid id, Guid billItemId, GlosaReason reason, string details, bool isClawback = false)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O id da glosa é inválido.", nameof(id));
        if (billItemId == Guid.Empty)
            throw new ArgumentException("O item glosado é inválido.", nameof(billItemId));
        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("A glosa deve possuir justificativa.", nameof(details));

        Id = id;
        BillItemId = billItemId;
        Reason = reason;
        Details = details;
        IsClawback = isClawback;
        Appeal = null;
    }

    public void FileAppeal(List<Evidence> evidenceDocuments)
    {
        if (Appeal != null)
        {
            throw new InvalidOperationException("Esta glosa já possui um recurso ativo associado. Apenas um recurso é permitido por negativa.");
        }

        Appeal = new AdministrativeAppeal(Guid.NewGuid(), Id, evidenceDocuments);
    }
}
