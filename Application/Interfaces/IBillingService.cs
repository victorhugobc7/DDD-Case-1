using System;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

public interface IBillingService
{
    Task<Guid> CreateHospitalBillFromAuthorizationAsync(CreateHospitalBillDto dto);
    Task<HospitalBillDto> GetHospitalBillAsync(Guid billId);
    Task<Guid> ApplyGlosaToItemAsync(ApplyGlosaDto dto);
    Task FileGlosaAppealAsync(FileGlosaAppealDto dto);
    Task EvaluateGlosaAppealAsync(EvaluateGlosaAppealDto dto);
    Task CloseHospitalBillAsync(Guid billId);
}
