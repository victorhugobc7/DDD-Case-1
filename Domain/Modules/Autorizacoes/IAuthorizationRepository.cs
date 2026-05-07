using System;
using System.Threading.Tasks;

namespace Domain.Modules.Autorizacoes;

public interface IAuthorizationRepository
{
    Task<AuthorizationRequest?> GetByIdAsync(Guid id);
    Task AddAsync(AuthorizationRequest authorizationRequest);
    Task UpdateAsync(AuthorizationRequest authorizationRequest);
}
