using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Billing;

namespace Application.UseCases.Billing;

public class FileGlosaAppealUseCase
{
    private readonly IHospitalBillRepository _hospitalBillRepository;

    public FileGlosaAppealUseCase(IHospitalBillRepository hospitalBillRepository)
    {
        _hospitalBillRepository = hospitalBillRepository;
    }

    public async Task ExecuteAsync(FileGlosaAppealDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var bill = await _hospitalBillRepository.GetByIdAsync(dto.HospitalBillId)
            ?? throw new KeyNotFoundException("Conta hospitalar não encontrada.");

        bill.FileAppeal(
            dto.BillItemId,
            dto.GlosaId,
            BillingUseCaseSupport.ToEvidence(dto.EvidenceDocuments));

        await _hospitalBillRepository.UpdateAsync(bill);
    }
}
