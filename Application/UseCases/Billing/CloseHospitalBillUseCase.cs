using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Billing;

namespace Application.UseCases.Billing;

public class CloseHospitalBillUseCase
{
    private readonly IHospitalBillRepository _hospitalBillRepository;

    public CloseHospitalBillUseCase(IHospitalBillRepository hospitalBillRepository)
    {
        _hospitalBillRepository = hospitalBillRepository;
    }

    public async Task ExecuteAsync(Guid hospitalBillId)
    {
        var bill = await _hospitalBillRepository.GetByIdAsync(hospitalBillId)
            ?? throw new KeyNotFoundException("Conta hospitalar não encontrada.");

        bill.Close();
        await _hospitalBillRepository.UpdateAsync(bill);
    }
}
