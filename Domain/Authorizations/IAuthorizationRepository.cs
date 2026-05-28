using System;
using System.Threading.Tasks;
using Domain.Authorizations;

namespace Domain.Authorizations;

public interface IAuthorizationRepository
{
    Task<AuthorizationRequest?> GetByIdAsync(Guid id);
    Task AddAsync(AuthorizationRequest authorizationRequest);
    Task UpdateAsync(AuthorizationRequest authorizationRequest);
}
