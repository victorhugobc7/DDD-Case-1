using System;
using Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using Domain.ValueObjects;

namespace Domain.Entities;

public class AdministrativeAppeal
{
    public Guid Id { get; private set; }
    public Guid GlosaId { get; private set; }
    private readonly List<Evidence> _evidenceDocuments;
    public IReadOnlyCollection<Evidence> EvidenceDocuments => _evidenceDocuments.AsReadOnly();
    public AppealStatus Status { get; private set; }

    internal AdministrativeAppeal(Guid id, Guid glosaId, List<Evidence> evidenceDocuments)
    {
        if (evidenceDocuments == null || !evidenceDocuments.Any())
            throw new ArgumentException("Um recurso deve conter pelo menos uma evidência.", nameof(evidenceDocuments));

        Id = id;
        GlosaId = glosaId;
        _evidenceDocuments = evidenceDocuments;
        Status = AppealStatus.EmAnalise;
    }

    public void MaintainGlosa()
    {
        if (Status != AppealStatus.EmAnalise)
            throw new InvalidOperationException("Apenas recursos em análise podem ser processados.");
            
        Status = AppealStatus.GlosaMantida;
    }

    public void RevertGlosa()
    {
        if (Status != AppealStatus.EmAnalise)
            throw new InvalidOperationException("Apenas recursos em análise podem ser processados.");

        Status = AppealStatus.GlosaRevertida;
    }
}
