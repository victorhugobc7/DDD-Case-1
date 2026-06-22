using System.Linq;
using Application.DTOs;
using Domain.Common.ValueObjects;
using Domain.Faturamento;
using Domain.Faturamento.Entities;
using Domain.Faturamento.ValueObjects;

namespace Application.UseCases.Billing;

internal static class BillingUseCaseSupport
{
    public static HospitalBillDto ToDto(HospitalBill bill)
    {
        return new HospitalBillDto
        {
            Id = bill.Id,
            BeneficiaryId = bill.BeneficiaryId,
            ExecutingEstablishment = bill.ExecutingEstablishment,
            Status = bill.Status,
            TotalValue = ToDto(bill.TotalValue),
            Items = bill.Items
                .Select(item => new HospitalBillItemDto
                {
                    Id = item.Id,
                    ApprovedAuthorizationId = item.ApprovedAuthorizationId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitValue = ToDto(item.UnitValue),
                    TotalValue = ToDto(item.TotalValue),
                    Glosas = item.Glosas
                        .Select(glosa => new HospitalBillItemGlosaDto
                        {
                            Id = glosa.Id,
                            BillItemId = glosa.BillItemId,
                            Reason = glosa.Reason,
                            Details = glosa.Details,
                            IsClawback = glosa.IsClawback,
                            Appeal = glosa.Appeal == null ? null : ToDto(glosa.Appeal)
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public static Money ToMoney(MoneyDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Money(dto.Amount, dto.Currency);
    }

    public static List<Evidence> ToEvidence(List<EvidenceDto> evidenceDocuments)
    {
        if (evidenceDocuments == null)
            throw new ArgumentNullException(nameof(evidenceDocuments));

        return evidenceDocuments
            .Select(evidence => new Evidence(evidence.DocumentUrl, evidence.Description))
            .ToList();
    }

    private static MoneyDto ToDto(Money money)
    {
        return new MoneyDto
        {
            Amount = money.Amount,
            Currency = money.Currency
        };
    }

    private static AdministrativeAppealDto ToDto(AdministrativeAppeal appeal)
    {
        return new AdministrativeAppealDto
        {
            Id = appeal.Id,
            GlosaId = appeal.GlosaId,
            Status = appeal.Status,
            EvidenceDocuments = appeal.EvidenceDocuments
                .Select(evidence => new EvidenceDto
                {
                    DocumentUrl = evidence.DocumentUrl,
                    Description = evidence.Description
                })
                .ToList()
        };
    }
}
