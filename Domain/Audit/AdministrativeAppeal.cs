using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Audit;

namespace Domain.Audit;

public class AdministrativeAppeal
{
    public Guid Id { get; private set; }
    public Guid GlosaId { get; private set; }
    private readonly List<Evidence> _evidenceDocuments;
    public IReadOnlyCollection<Evidence> EvidenceDocuments => _evidenceDocuments.AsReadOnly();
    public AppealStatus Status { get; private set; }

    internal AdministrativeAppeal(Guid id, Guid glosaId, List<Evidence> evidenceDocuments)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O id do recurso é inválido.", nameof(id));
        if (glosaId == Guid.Empty)
            throw new ArgumentException("A glosa do recurso é inválida.", nameof(glosaId));
        if (evidenceDocuments == null || !evidenceDocuments.Any())
            throw new ArgumentException("Um recurso deve conter pelo menos uma evidência.", nameof(evidenceDocuments));

        Id = id;
        GlosaId = glosaId;
        _evidenceDocuments = evidenceDocuments;
        Status = AppealStatus.EmAnalise;
    }

    public static AdministrativeAppeal Restore(
        Guid id,
        Guid glosaId,
        List<Evidence> evidenceDocuments,
        AppealStatus status)
    {
        var appeal = new AdministrativeAppeal(id, glosaId, evidenceDocuments);
        appeal.Status = status;

        return appeal;
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
