using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Faturamento.Interfaces;

namespace Application.UseCases.Billing;

public class ApplyGlosaToHospitalBillItemUseCase
{
    private readonly IHospitalBillRepository _hospitalBillRepository;

    public ApplyGlosaToHospitalBillItemUseCase(IHospitalBillRepository hospitalBillRepository)
    {
        _hospitalBillRepository = hospitalBillRepository;
    }

    public async Task<Guid> ExecuteAsync(ApplyGlosaDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var bill = await _hospitalBillRepository.GetByIdAsync(dto.HospitalBillId)
            ?? throw new KeyNotFoundException("Conta hospitalar não encontrada.");

        var glosa = bill.ApplyGlosaToItem(dto.BillItemId, dto.Reason, dto.Details);
        await _hospitalBillRepository.UpdateAsync(bill);

        return glosa.Id;
    }
}
