using System;
using System.Linq;
using Domain.Enums;
using System.Collections.Generic;
using Domain.ValueObjects;

namespace Domain.Entities;

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
    public string DenialReason { get; private set; }

    public AuthorizationRequest(Guid id, Guid beneficiaryId, PlanNumber planNumber, ProcedureCode procedureCode, 
        CidCode clinicalJustification, ProfessionalRegistry requestingProfessional, string executingEstablishment, 
        DateTime expectedDate, List<RequestedItem> items, bool isUrgentOrEmergency)
    {
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

    public void SetAsEmergencyException()
    {
        RequiresPostPaymentAudit = true;
        Status = AuthorizationStatus.AprovadaIntegralmente;
        foreach (var item in _items)
        {
            item.ApproveFully();
        }
    }

    public void ApproveFully()
    {
        Status = AuthorizationStatus.AprovadaIntegralmente;
        foreach (var item in _items)
        {
            item.ApproveFully();
        }
    }

    public void ApprovePartially(Dictionary<Guid, int> approvedQuantities)
    {
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
        foreach (var item in _items)
        {
            item.Deny();
        }
        Status = AuthorizationStatus.Negada;
        DenialReason = $"{reason}: {details}";
    }
}
