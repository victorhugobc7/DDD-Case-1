using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthorizationService
{
    Task<Guid> RequestAuthorizationAsync(AuthorizationRequestDto dto);
    Task ApproveAuthorizationAsync(Guid authorizationId);
    Task ApproveAuthorizationPartiallyAsync(Guid authorizationId, Dictionary<Guid, int> approvedQuantities);
    Task DenyAuthorizationAsync(Guid authorizationId, string reason);
}
