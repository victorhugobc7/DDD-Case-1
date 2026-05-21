using System.Linq;
using Application.DTOs;
using Domain.Aggregates.Faturamento;

namespace Application.UseCases.Faturamento;

internal static class BillingUseCaseSupport
{
    public static HospitalBillDto ToDto(HospitalBill bill)
    {
        return new HospitalBillDto
        {
            Id = bill.Id,
            BeneficiaryId = bill.BeneficiaryId,
            ExecutingEstablishment = bill.ExecutingEstablishment,
            TotalValue = bill.Items.Sum(item => item.TotalValue),
            Items = bill.Items
                .Select(item => new HospitalBillItemDto
                {
                    Id = item.Id,
                    ApprovedAuthorizationId = item.ApprovedAuthorizationId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitValue = item.UnitValue,
                    TotalValue = item.TotalValue
                })
                .ToList()
        };
    }
}
