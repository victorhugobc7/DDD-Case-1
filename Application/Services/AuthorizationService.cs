using System;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Factories;
using Domain.Interfaces;

namespace Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IAuthorizationRepository _authorizationRepository;

    public AuthorizationService(IAuthorizationRepository authorizationRepository)
    {
        _authorizationRepository = authorizationRepository;
    }

    public async Task<Guid> RequestAuthorizationAsync(AuthorizationRequestDto dto)
    {
        var authorization = AuthorizationRequestFactory.Create(
            dto.BeneficiaryId,
            dto.PlanNumber,
            dto.ProcedureCode,
            dto.ClinicalJustification,
            dto.RequestingProfessional,
            dto.ExecutingEstablishment,
            dto.ExpectedDate,
            dto.MaterialsAndMedicines,
            dto.IsUrgentOrEmergency
        );

        await _authorizationRepository.AddAsync(authorization);
        
        return authorization.Id;
    }

    public async Task ApproveAuthorizationAsync(Guid authorizationId)
    {
        var authorization = await _authorizationRepository.GetByIdAsync(authorizationId);
        if (authorization != null)
        {
            authorization.ApproveFully();
            await _authorizationRepository.UpdateAsync(authorization);
        }
    }

    public async Task ApproveAuthorizationPartiallyAsync(Guid authorizationId, System.Collections.Generic.Dictionary<Guid, int> approvedQuantities)
    {
        var authorization = await _authorizationRepository.GetByIdAsync(authorizationId);
        if (authorization != null)
        {
            authorization.ApprovePartially(approvedQuantities);
            await _authorizationRepository.UpdateAsync(authorization);
        }
    }

    public async Task DenyAuthorizationAsync(Guid authorizationId, string reason)
    {
        var authorization = await _authorizationRepository.GetByIdAsync(authorizationId);
        if (authorization != null)
        {
            authorization.Deny(reason);
            await _authorizationRepository.UpdateAsync(authorization);
        }
    }
}
