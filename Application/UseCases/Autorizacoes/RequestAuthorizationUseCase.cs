using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Modules.Autorizacoes;
using Domain.Modules.Planos;
using Domain.Modules.Procedimentos;
using Domain.Modules.RedeCredenciada;

namespace Application.UseCases.Autorizacoes;

public class RequestAuthorizationUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;

    public RequestAuthorizationUseCase(IAuthorizationRepository authorizationRepository)
    {
        _authorizationRepository = authorizationRepository;
    }

    public async Task<Guid> ExecuteAsync(AuthorizationRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var requestedItems = CreateRequestedItems(dto.MaterialsAndMedicines);

        var authorization = AuthorizationRequestFactory.Create(
            dto.BeneficiaryId,
            new PlanNumber(dto.PlanNumber),
            new ProcedureCode(dto.ProcedureCode),
            new CidCode(dto.ClinicalJustification),
            new ProfessionalRegistry(dto.RequestingProfessional),
            dto.ExecutingEstablishment,
            dto.ExpectedDate,
            requestedItems,
            dto.IsUrgentOrEmergency
        );

        await _authorizationRepository.AddAsync(authorization);

        return authorization.Id;
    }

    private static List<RequestedItem> CreateRequestedItems(List<string> materialsAndMedicines)
    {
        if (materialsAndMedicines == null || !materialsAndMedicines.Any())
            throw new ArgumentException(
                "A solicitação deve conter pelo menos um material, medicamento ou item solicitado.",
                nameof(materialsAndMedicines));

        return materialsAndMedicines
            .Select(materialOrMedicine => new RequestedItem(Guid.NewGuid(), materialOrMedicine, 1))
            .ToList();
    }
}
