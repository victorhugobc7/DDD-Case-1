using System;
using System.Collections.Generic;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Glosa
{
    public Guid Id { get; private set; }
    public Guid BillItemId { get; private set; }
    public GlosaReason Reason { get; private set; }
    public string Details { get; private set; }
    public AdministrativeAppeal Appeal { get; private set; }
    public bool IsClawback { get; private set; }

    internal Glosa(Guid id, Guid billItemId, GlosaReason reason, string details, bool isClawback = false)
    {
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
