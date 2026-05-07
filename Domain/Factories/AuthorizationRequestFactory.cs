using System;
using System.Linq;
using System.Collections.Generic;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Factories;

public static class AuthorizationRequestFactory
{
    public static AuthorizationRequest Create(
        Guid beneficiaryId, 
        string planNumber, 
        string procedureCode, 
        string clinicalJustification, 
        string requestingProfessional, 
        string executingEstablishment, 
        DateTime expectedDate, 
        List<string> materialsAndMedicines, 
        bool isUrgentOrEmergency)
    {
        var planNumberVo = new PlanNumber(planNumber);
        var procedureCodeVo = new ProcedureCode(procedureCode);
        var cidCodeVo = new CidCode(clinicalJustification);
        var professionalRegistryVo = new ProfessionalRegistry(requestingProfessional);

        var requestedItems = materialsAndMedicines
            .Select(m => new RequestedItem(Guid.NewGuid(), m, 1))
            .ToList();

        var request = new AuthorizationRequest(
            Guid.NewGuid(),
            beneficiaryId,
            planNumberVo,
            procedureCodeVo,
            cidCodeVo,
            professionalRegistryVo,
            executingEstablishment,
            expectedDate,
            requestedItems,
            isUrgentOrEmergency
        );

        if (request.IsUrgentOrEmergency)
        {
            request.SetAsEmergencyException();
        }

        return request;
    }
}