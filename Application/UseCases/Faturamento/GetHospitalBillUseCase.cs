using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Repositories.Faturamento;

namespace Application.UseCases.Faturamento;

public class GetHospitalBillUseCase
{
    private readonly IHospitalBillRepository _hospitalBillRepository;

    public GetHospitalBillUseCase(IHospitalBillRepository hospitalBillRepository)
    {
        _hospitalBillRepository = hospitalBillRepository;
    }

    public async Task<HospitalBillDto> ExecuteAsync(Guid billId)
    {
        var bill = await _hospitalBillRepository.GetByIdAsync(billId);
        if (bill == null)
            throw new KeyNotFoundException("Conta hospitalar não encontrada.");

        return BillingUseCaseSupport.ToDto(bill);
    }
}
