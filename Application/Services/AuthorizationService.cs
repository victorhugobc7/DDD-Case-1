using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Authorizations;
using Domain.Audit;

namespace Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly RequestAuthorizationUseCase _requestAuthorization;
    private readonly ApproveAuthorizationUseCase _approveAuthorization;
    private readonly ApproveAuthorizationPartiallyUseCase _approveAuthorizationPartially;
    private readonly DenyAuthorizationUseCase _denyAuthorization;
    private readonly RegisterDocumentPendingUseCase _registerDocumentPending;
    private readonly GetAuthorizationStatusUseCase _getAuthorizationStatus;

    public AuthorizationService(
        RequestAuthorizationUseCase requestAuthorization,
        ApproveAuthorizationUseCase approveAuthorization,
        ApproveAuthorizationPartiallyUseCase approveAuthorizationPartially,
        DenyAuthorizationUseCase denyAuthorization,
        RegisterDocumentPendingUseCase registerDocumentPending,
        GetAuthorizationStatusUseCase getAuthorizationStatus)
    {
        _requestAuthorization = requestAuthorization ?? throw new ArgumentNullException(nameof(requestAuthorization));
        _approveAuthorization = approveAuthorization ?? throw new ArgumentNullException(nameof(approveAuthorization));
        _approveAuthorizationPartially = approveAuthorizationPartially ?? throw new ArgumentNullException(nameof(approveAuthorizationPartially));
        _denyAuthorization = denyAuthorization ?? throw new ArgumentNullException(nameof(denyAuthorization));
        _registerDocumentPending = registerDocumentPending ?? throw new ArgumentNullException(nameof(registerDocumentPending));
        _getAuthorizationStatus = getAuthorizationStatus ?? throw new ArgumentNullException(nameof(getAuthorizationStatus));
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
