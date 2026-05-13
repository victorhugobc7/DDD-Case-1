using System;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Faturamento;
using Domain.Repositories.Autorizacoes;
using Domain.Repositories.Faturamento;

namespace Application.Services;

public class BillingService : IBillingService
{
    private readonly CreateHospitalBillFromAuthorizationUseCase _createHospitalBillFromAuthorization;
    private readonly GetHospitalBillUseCase _getHospitalBill;

    public BillingService(
        IAuthorizationRepository authorizationRepository,
        IHospitalBillRepository hospitalBillRepository)
    {
        _createHospitalBillFromAuthorization = new CreateHospitalBillFromAuthorizationUseCase(
            authorizationRepository,
            hospitalBillRepository);
        _getHospitalBill = new GetHospitalBillUseCase(hospitalBillRepository);
    }

    public async Task<Guid> CreateHospitalBillFromAuthorizationAsync(CreateHospitalBillDto dto)
    {
        return await _createHospitalBillFromAuthorization.ExecuteAsync(dto);
    }

    public async Task<HospitalBillDto> GetHospitalBillAsync(Guid billId)
    {
        return await _getHospitalBill.ExecuteAsync(billId);
    }
}
