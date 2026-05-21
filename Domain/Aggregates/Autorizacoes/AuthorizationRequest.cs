using System;
using System.Linq;
using System.Collections.Generic;
using Domain.Enums.Auditoria;
using Domain.Enums.Autorizacoes;
using Domain.ValueObjects.Planos;
using Domain.ValueObjects.Procedimentos;
using Domain.ValueObjects.RedeCredenciada;

namespace Domain.Aggregates.Autorizacoes;

public class AuthorizationRequest
{
    public Guid Id { get; private set; }
    public Guid BeneficiaryId { get; private set; }
    public PlanNumber PlanNumber { get; private set; }
    public ProcedureCode ProcedureCode { get; private set; }
    public CidCode ClinicalJustification { get; private set; }
    public ProfessionalRegistry RequestingProfessional { get; private set; }
    public string ExecutingEstablishment { get; private set; }
    public DateTime ExpectedDate { get; private set; }
    private readonly List<RequestedItem> _items;
    public IReadOnlyCollection<RequestedItem> Items => _items.AsReadOnly();
    public AuthorizationStatus Status { get; private set; }
    public bool IsUrgentOrEmergency { get; private set; }
    public bool RequiresPostPaymentAudit { get; private set; }
    public string? DenialReason { get; private set; }
    public string? PendingReason { get; private set; }

    public AuthorizationRequest(Guid id, Guid beneficiaryId, PlanNumber planNumber, ProcedureCode procedureCode, 
        CidCode clinicalJustification, ProfessionalRegistry requestingProfessional, string executingEstablishment, 
        DateTime expectedDate, List<RequestedItem> items, bool isUrgentOrEmergency)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O id da solicitação é inválido.", nameof(id));
        if (beneficiaryId == Guid.Empty)
            throw new ArgumentException("O id do beneficiário é inválido.", nameof(beneficiaryId));
        if (planNumber == null)
            throw new ArgumentNullException(nameof(planNumber));
        if (procedureCode == null)
            throw new ArgumentNullException(nameof(procedureCode));
        if (clinicalJustification == null)
            throw new ArgumentNullException(nameof(clinicalJustification));
        if (requestingProfessional == null)
            throw new ArgumentNullException(nameof(requestingProfessional));
        if (string.IsNullOrWhiteSpace(executingEstablishment))
            throw new ArgumentException("O estabelecimento executante não pode ser vazio.", nameof(executingEstablishment));
        if (items == null || !items.Any())
            throw new ArgumentException("A requisição deve conter pelo menos um item.", nameof(items));

        Id = id;
        BeneficiaryId = beneficiaryId;
        PlanNumber = planNumber;
        ProcedureCode = procedureCode;
        ClinicalJustification = clinicalJustification;
        RequestingProfessional = requestingProfessional;
        ExecutingEstablishment = executingEstablishment;
        ExpectedDate = expectedDate;
        _items = items;
        IsUrgentOrEmergency = isUrgentOrEmergency;
        Status = AuthorizationStatus.Pendente;
        RequiresPostPaymentAudit = false;
    }

    public static AuthorizationRequest Restore(
        Guid id,
        Guid beneficiaryId,
        PlanNumber planNumber,
        ProcedureCode procedureCode,
        CidCode clinicalJustification,
        ProfessionalRegistry requestingProfessional,
        string executingEstablishment,
        DateTime expectedDate,
        List<RequestedItem> items,
        AuthorizationStatus status,
        bool isUrgentOrEmergency,
        bool requiresPostPaymentAudit,
        string? denialReason,
        string? pendingReason)
    {
        var authorization = new AuthorizationRequest(
            id,
            beneficiaryId,
            planNumber,
            procedureCode,
            clinicalJustification,
            requestingProfessional,
            executingEstablishment,
            expectedDate,
            items,
            isUrgentOrEmergency);

        authorization.Status = status;
        authorization.RequiresPostPaymentAudit = requiresPostPaymentAudit;
        authorization.DenialReason = denialReason;
        authorization.PendingReason = pendingReason;

        return authorization;
    }

    public void SetAsEmergencyException()
    {
        EnsurePending();

        RequiresPostPaymentAudit = true;
        Status = AuthorizationStatus.AprovadaIntegralmente;
        foreach (var item in _items)
        {
            item.ApproveFully();
        }
    }

    public void ApproveFully()
    {
        EnsurePending();

        Status = AuthorizationStatus.AprovadaIntegralmente;
        foreach (var item in _items)
        {
            item.ApproveFully();
        }
    }

    public void ApprovePartially(Dictionary<Guid, int> approvedQuantities)
    {
        EnsurePending();

        if (approvedQuantities == null || !approvedQuantities.Any())
            throw new ArgumentException("A aprovação parcial deve informar pelo menos um item autorizado.", nameof(approvedQuantities));

        var unknownItemIds = approvedQuantities.Keys.Except(_items.Select(item => item.Id)).ToList();
        if (unknownItemIds.Any())
            throw new ArgumentException("A aprovação parcial contém item que não pertence à solicitação.", nameof(approvedQuantities));

        foreach (var approvedQuantity in approvedQuantities)
        {
            var requestedItem = _items.Single(item => item.Id == approvedQuantity.Key);
            if (approvedQuantity.Value < 0 || approvedQuantity.Value > requestedItem.RequestedQuantity)
                throw new ArgumentException("A quantidade aprovada é inválida.", nameof(approvedQuantities));
        }

        if (approvedQuantities.Values.All(quantity => quantity == 0))
            throw new InvalidOperationException("A aprovação parcial deve autorizar ao menos uma quantidade.");

        foreach (var item in _items)
        {
            if (approvedQuantities.TryGetValue(item.Id, out int quantity))
            {
                item.ApprovePartially(quantity);
            }
            else
            {
                item.Deny();
            }
        }

        Status = AuthorizationStatus.AprovadaParcialmente;
    }

    public void Deny(GlosaReason reason, string details)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("A negativa deve possuir justificativa.", nameof(details));

        foreach (var item in _items)
        {
            item.Deny();
        }
        Status = AuthorizationStatus.Negada;
        DenialReason = $"{reason}: {details}";
    }

    public void RegisterDocumentPending(string missingDocuments)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(missingDocuments))
            throw new ArgumentException("A pendência deve informar os documentos faltantes.", nameof(missingDocuments));

        PendingReason = missingDocuments;
    }

    private void EnsurePending()
    {
        if (Status != AuthorizationStatus.Pendente)
            throw new InvalidOperationException("Somente solicitações pendentes podem receber decisão.");
    }
}
