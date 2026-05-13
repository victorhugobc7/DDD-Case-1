using System;
using System.Threading.Tasks;
using Domain.Repositories.Autorizacoes;

namespace Application.UseCases.Autorizacoes;

public class RegisterDocumentPendingUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;

    public RegisterDocumentPendingUseCase(IAuthorizationRepository authorizationRepository)
    {
        _authorizationRepository = authorizationRepository;
    }

    public async Task ExecuteAsync(Guid authorizationId, string missingDocuments)
    {
        var authorization = await AuthorizationUseCaseSupport.GetRequiredAuthorizationAsync(
            _authorizationRepository,
            authorizationId);

        authorization.RegisterDocumentPending(missingDocuments);
        await _authorizationRepository.UpdateAsync(authorization);
    }
}
