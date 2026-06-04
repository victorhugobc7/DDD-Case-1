using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Authorizations;
using Domain.Beneficiaries;
using Domain.Plans;
using Domain.Procedures;
using Domain.ProviderNetwork;

namespace Application.UseCases.Authorizations;

public class RequestAuthorizationUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IBeneficiaryRepository _beneficiaryRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IProcedureCatalogRepository _procedureCatalogRepository;
    private readonly EligibilityService _eligibilityService;

    public RequestAuthorizationUseCase(
        IAuthorizationRepository authorizationRepository,
        IBeneficiaryRepository beneficiaryRepository,
        IPlanRepository planRepository,
        IProcedureCatalogRepository procedureCatalogRepository,
        EligibilityService eligibilityService)
    {
        _authorizationRepository = authorizationRepository;
        _beneficiaryRepository = beneficiaryRepository;
        _planRepository = planRepository;
        _procedureCatalogRepository = procedureCatalogRepository;
        _eligibilityService = eligibilityService;
    }

    public async Task<Guid> ExecuteAsync(AuthorizationRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var planNumber = new PlanNumber(dto.PlanNumber);
        var procedureCode = new ProcedureCode(dto.ProcedureCode);
        var cidCode = new CidCode(dto.CidCode);

        var beneficiary = await _beneficiaryRepository.GetByIdAsync(dto.BeneficiaryId)
            ?? throw new KeyNotFoundException("Beneficiário não encontrado.");
        var plan = await _planRepository.GetByNumberAsync(planNumber.Value)
            ?? throw new KeyNotFoundException("Plano não encontrado.");
        var procedure = await _procedureCatalogRepository.GetByCodeAsync(procedureCode)
            ?? throw new KeyNotFoundException("Procedimento não encontrado.");

        _eligibilityService.ValidateEligibility(beneficiary, plan, procedure, dto.ExpectedDate);

        var requestedItems = CreateRequestedItems(dto.RequestedItems);

        var authorization = AuthorizationRequestFactory.Create(
            dto.BeneficiaryId,
            planNumber,
            procedureCode,
            cidCode,
            new ProfessionalRegistry(dto.RequestingProfessional),
            dto.ExecutingEstablishment,
            dto.ExpectedDate,
            requestedItems,
            dto.IsUrgentOrEmergency
        );

        await _authorizationRepository.AddAsync(authorization);

        return authorization.Id;
    }

    private static List<RequestedItem> CreateRequestedItems(List<RequestedItemDto> requestedItems)
    {
        if (requestedItems == null || !requestedItems.Any())
            throw new ArgumentException(
                "A solicitação deve conter pelo menos um material, medicamento ou item solicitado.",
                nameof(requestedItems));

        return requestedItems
            .Select(item =>
            {
                if (item == null)
                    throw new ArgumentException("Item solicitado inválido.", nameof(requestedItems));

                return new RequestedItem(Guid.NewGuid(), item.Description, item.Quantity);
            })
            .ToList();
    }
}
