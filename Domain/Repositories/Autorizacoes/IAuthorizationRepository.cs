using System;
using System.Threading.Tasks;
using Domain.Aggregates.Autorizacoes;

namespace Domain.Repositories.Autorizacoes;

public interface IAuthorizationRepository
{
    Task<AuthorizationRequest?> GetByIdAsync(Guid id);
    Task AddAsync(AuthorizationRequest authorizationRequest);
    Task UpdateAsync(AuthorizationRequest authorizationRequest);
}
