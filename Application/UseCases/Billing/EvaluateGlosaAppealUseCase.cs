using System;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Billing;

namespace Application.UseCases.Billing;

public class EvaluateGlosaAppealUseCase
{
    private readonly IHospitalBillRepository _hospitalBillRepository;

    public EvaluateGlosaAppealUseCase(IHospitalBillRepository hospitalBillRepository)
    {
        _hospitalBillRepository = hospitalBillRepository ?? throw new ArgumentNullException(nameof(hospitalBillRepository));
    }

    public async Task ExecuteAsync(EvaluateGlosaAppealDto dto)
    {
        var hospitalBill = await _hospitalBillRepository.GetByIdAsync(dto.HospitalBillId)
            ?? throw new KeyNotFoundException("Fatura hospitalar não encontrada.");

        hospitalBill.EvaluateGlosaAppeal(dto.BillItemId, dto.GlosaId, dto.Approve);

        await _hospitalBillRepository.UpdateAsync(hospitalBill);
    }
}
