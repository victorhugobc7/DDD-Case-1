using System;
using System.Threading.Tasks;
using Domain.Repositories.Autorizacoes;

namespace Application.UseCases.Autorizacoes;

public class ApproveAuthorizationUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;

    public ApproveAuthorizationUseCase(IAuthorizationRepository authorizationRepository)
    {
        _authorizationRepository = authorizationRepository;
    }

    public async Task ExecuteAsync(Guid authorizationId)
    {
        var authorization = await AuthorizationUseCaseSupport.GetRequiredAuthorizationAsync(
            _authorizationRepository,
            authorizationId);

        authorization.ApproveFully();
        await _authorizationRepository.UpdateAsync(authorization);
    }
}
