using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Aggregates.Autorizacoes;
using Domain.Repositories.Autorizacoes;

namespace Application.UseCases.Autorizacoes;

internal static class AuthorizationUseCaseSupport
{
    public static async Task<AuthorizationRequest> GetRequiredAuthorizationAsync(
        IAuthorizationRepository authorizationRepository,
        Guid authorizationId)
    {
        var authorization = await authorizationRepository.GetByIdAsync(authorizationId);
        if (authorization == null)
            throw new KeyNotFoundException("Solicitação de autorização não encontrada.");

        return authorization;
    }

    public static AuthorizationStatusDto ToStatusDto(AuthorizationRequest authorization)
    {
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
}
