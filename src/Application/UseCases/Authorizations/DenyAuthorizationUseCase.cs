using System;
using System.Threading.Tasks;
using Domain.Common.Enums;
using Domain.Solicitacao.Interfaces;

namespace Application.UseCases.Authorizations;

public class DenyAuthorizationUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;

    public DenyAuthorizationUseCase(IAuthorizationRepository authorizationRepository)
    {
        _authorizationRepository = authorizationRepository;
    }

    public async Task ExecuteAsync(Guid authorizationId, GlosaReason reason, string details)
    {
        var authorization = await AuthorizationUseCaseSupport.GetRequiredAuthorizationAsync(
            _authorizationRepository,
            authorizationId);

        authorization.Deny(reason, details);
        await _authorizationRepository.UpdateAsync(authorization);
    }
}
