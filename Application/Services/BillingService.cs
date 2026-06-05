using System;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Billing;

namespace Application.Services;

public class BillingService : IBillingService
{
    private readonly CreateHospitalBillFromAuthorizationUseCase _createHospitalBillFromAuthorization;
    private readonly GetHospitalBillUseCase _getHospitalBill;
    private readonly ApplyGlosaToHospitalBillItemUseCase _applyGlosaToItem;
    private readonly FileGlosaAppealUseCase _fileGlosaAppeal;
    private readonly EvaluateGlosaAppealUseCase _evaluateGlosaAppeal;
    private readonly CloseHospitalBillUseCase _closeHospitalBill;

    public BillingService(
        CreateHospitalBillFromAuthorizationUseCase createHospitalBillFromAuthorization,
        GetHospitalBillUseCase getHospitalBill,
        ApplyGlosaToHospitalBillItemUseCase applyGlosaToItem,
        FileGlosaAppealUseCase fileGlosaAppeal,
        EvaluateGlosaAppealUseCase evaluateGlosaAppeal,
        CloseHospitalBillUseCase closeHospitalBill)
    {
        _createHospitalBillFromAuthorization = createHospitalBillFromAuthorization ?? throw new ArgumentNullException(nameof(createHospitalBillFromAuthorization));
        _getHospitalBill = getHospitalBill ?? throw new ArgumentNullException(nameof(getHospitalBill));
        _applyGlosaToItem = applyGlosaToItem ?? throw new ArgumentNullException(nameof(applyGlosaToItem));
        _fileGlosaAppeal = fileGlosaAppeal ?? throw new ArgumentNullException(nameof(fileGlosaAppeal));
        _evaluateGlosaAppeal = evaluateGlosaAppeal ?? throw new ArgumentNullException(nameof(evaluateGlosaAppeal));
        _closeHospitalBill = closeHospitalBill ?? throw new ArgumentNullException(nameof(closeHospitalBill));
    }

    public async Task<Guid> CreateHospitalBillFromAuthorizationAsync(CreateHospitalBillDto dto)
    {
        return await _createHospitalBillFromAuthorization.ExecuteAsync(dto);
    }

    public async Task<HospitalBillDto> GetHospitalBillAsync(Guid billId)
    {
        return await _getHospitalBill.ExecuteAsync(billId);
    }

    public async Task<Guid> ApplyGlosaToItemAsync(ApplyGlosaDto dto)
    {
        return await _applyGlosaToItem.ExecuteAsync(dto);
    }

    public async Task FileGlosaAppealAsync(FileGlosaAppealDto dto)
    {
        await _fileGlosaAppeal.ExecuteAsync(dto);
    }

    public async Task EvaluateGlosaAppealAsync(EvaluateGlosaAppealDto dto)
    {
        await _evaluateGlosaAppeal.ExecuteAsync(dto);
    }

    public async Task CloseHospitalBillAsync(Guid billId)
    {
        await _closeHospitalBill.ExecuteAsync(billId);
    }
}
