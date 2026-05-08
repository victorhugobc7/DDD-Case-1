using System;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Modules.Autorizacoes;

namespace Application.UseCases.Autorizacoes;

public class GetAuthorizationStatusUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;

    public GetAuthorizationStatusUseCase(IAuthorizationRepository authorizationRepository)
    {
        _authorizationRepository = authorizationRepository;
    }

    public async Task<AuthorizationStatusDto> ExecuteAsync(Guid authorizationId)
    {
        var authorization = await AuthorizationUseCaseSupport.GetRequiredAuthorizationAsync(
            _authorizationRepository,
            authorizationId);

        return AuthorizationUseCaseSupport.ToStatusDto(authorization);
    }
}
