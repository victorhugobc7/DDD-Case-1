using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Modules.Auditoria;
using Domain.Modules.Autorizacoes;

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
        var authorization = await GetRequiredAuthorizationAsync(authorizationId);
        authorization.ApproveFully();
        await _authorizationRepository.UpdateAsync(authorization);
    }

    public async Task ApproveAuthorizationPartiallyAsync(Guid authorizationId, Dictionary<Guid, int> approvedQuantities)
    {
        var authorization = await GetRequiredAuthorizationAsync(authorizationId);
        authorization.ApprovePartially(approvedQuantities);
        await _authorizationRepository.UpdateAsync(authorization);
    }

    public async Task DenyAuthorizationAsync(Guid authorizationId, GlosaReason reason, string details)
    {
        var authorization = await GetRequiredAuthorizationAsync(authorizationId);
        authorization.Deny(reason, details);
        await _authorizationRepository.UpdateAsync(authorization);
    }

    public async Task RegisterDocumentPendingAsync(Guid authorizationId, string missingDocuments)
    {
        var authorization = await GetRequiredAuthorizationAsync(authorizationId);
        authorization.RegisterDocumentPending(missingDocuments);
        await _authorizationRepository.UpdateAsync(authorization);
    }

    public async Task<AuthorizationStatusDto> GetAuthorizationStatusAsync(Guid authorizationId)
    {
        var authorization = await GetRequiredAuthorizationAsync(authorizationId);

        return new AuthorizationStatusDto
        {
            Id = authorization.Id,
            Status = authorization.Status,
            RequiresPostPaymentAudit = authorization.RequiresPostPaymentAudit,
            DenialReason = authorization.DenialReason,
            PendingReason = authorization.PendingReason,
            Items = authorization.Items
                .Select(item => new AuthorizationItemStatusDto
                {
                    Id = item.Id,
                    Description = item.Description,
                    RequestedQuantity = item.RequestedQuantity,
                    ApprovedQuantity = item.ApprovedQuantity
                })
                .ToList()
        };
    }

    private async Task<AuthorizationRequest> GetRequiredAuthorizationAsync(Guid authorizationId)
    {
        var authorization = await _authorizationRepository.GetByIdAsync(authorizationId);
        if (authorization == null)
            throw new KeyNotFoundException("Solicitação de autorização não encontrada.");

        return authorization;
    }
}
