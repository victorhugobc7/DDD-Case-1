using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Authorizations;

namespace Application.UseCases.Authorizations;

public class ApproveAuthorizationPartiallyUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;

    public ApproveAuthorizationPartiallyUseCase(IAuthorizationRepository authorizationRepository)
    {
        _authorizationRepository = authorizationRepository;
    }

    public async Task ExecuteAsync(Guid authorizationId, Dictionary<Guid, int> approvedQuantities)
    {
        var authorization = await AuthorizationUseCaseSupport.GetRequiredAuthorizationAsync(
            _authorizationRepository,
            authorizationId);

        authorization.ApprovePartially(approvedQuantities);
        await _authorizationRepository.UpdateAsync(authorization);
    }
}
