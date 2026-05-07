using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces;

public interface IAuthorizationRepository
{
    Task<AuthorizationRequest?> GetByIdAsync(Guid id);
    Task AddAsync(AuthorizationRequest authorizationRequest);
    Task UpdateAsync(AuthorizationRequest authorizationRequest);
}
