using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Enums.Auditoria;

namespace Application.Interfaces;

public interface IAuthorizationService
{
    Task<Guid> RequestAuthorizationAsync(AuthorizationRequestDto dto);
    Task ApproveAuthorizationAsync(Guid authorizationId);
    Task ApproveAuthorizationPartiallyAsync(Guid authorizationId, Dictionary<Guid, int> approvedQuantities);
    Task DenyAuthorizationAsync(Guid authorizationId, GlosaReason reason, string details);
    Task RegisterDocumentPendingAsync(Guid authorizationId, string missingDocuments);
    Task<AuthorizationStatusDto> GetAuthorizationStatusAsync(Guid authorizationId);
}
