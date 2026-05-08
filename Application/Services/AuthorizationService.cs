using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Autorizacoes;
using Domain.Modules.Auditoria;
using Domain.Modules.Autorizacoes;

namespace Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly RequestAuthorizationUseCase _requestAuthorization;
    private readonly ApproveAuthorizationUseCase _approveAuthorization;
    private readonly ApproveAuthorizationPartiallyUseCase _approveAuthorizationPartially;
    private readonly DenyAuthorizationUseCase _denyAuthorization;
    private readonly RegisterDocumentPendingUseCase _registerDocumentPending;
    private readonly GetAuthorizationStatusUseCase _getAuthorizationStatus;

    public AuthorizationService(IAuthorizationRepository authorizationRepository)
    {
        _requestAuthorization = new RequestAuthorizationUseCase(authorizationRepository);
        _approveAuthorization = new ApproveAuthorizationUseCase(authorizationRepository);
        _approveAuthorizationPartially = new ApproveAuthorizationPartiallyUseCase(authorizationRepository);
        _denyAuthorization = new DenyAuthorizationUseCase(authorizationRepository);
        _registerDocumentPending = new RegisterDocumentPendingUseCase(authorizationRepository);
        _getAuthorizationStatus = new GetAuthorizationStatusUseCase(authorizationRepository);
    }

    public async Task<Guid> RequestAuthorizationAsync(AuthorizationRequestDto dto)
    {
        return await _requestAuthorization.ExecuteAsync(dto);
    }

    public async Task ApproveAuthorizationAsync(Guid authorizationId)
    {
        await _approveAuthorization.ExecuteAsync(authorizationId);
    }

    public async Task ApproveAuthorizationPartiallyAsync(Guid authorizationId, Dictionary<Guid, int> approvedQuantities)
    {
        await _approveAuthorizationPartially.ExecuteAsync(authorizationId, approvedQuantities);
    }

    public async Task DenyAuthorizationAsync(Guid authorizationId, GlosaReason reason, string details)
    {
        await _denyAuthorization.ExecuteAsync(authorizationId, reason, details);
    }

    public async Task RegisterDocumentPendingAsync(Guid authorizationId, string missingDocuments)
    {
        await _registerDocumentPending.ExecuteAsync(authorizationId, missingDocuments);
    }

    public async Task<AuthorizationStatusDto> GetAuthorizationStatusAsync(Guid authorizationId)
    {
        return await _getAuthorizationStatus.ExecuteAsync(authorizationId);
    }
}
