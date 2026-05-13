using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Domain.Aggregates.Autorizacoes;
using Domain.Repositories.Autorizacoes;

namespace Infra.Repositories;

public class AuthorizationRepository : IAuthorizationRepository
{
    private readonly ConcurrentDictionary<Guid, AuthorizationRequest> _database = new();

    public Task<AuthorizationRequest?> GetByIdAsync(Guid id)
    {
        _database.TryGetValue(id, out var authorizationRequest);
        return Task.FromResult(authorizationRequest);
    }

    public Task AddAsync(AuthorizationRequest authorizationRequest)
    {
        _database[authorizationRequest.Id] = authorizationRequest;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AuthorizationRequest authorizationRequest)
    {
        _database[authorizationRequest.Id] = authorizationRequest;
        return Task.CompletedTask;
    }
}
