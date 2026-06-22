using System;
using System.Collections.Generic;
using Domain.Common.ValueObjects;
using Domain.Solicitacao;
using Domain.Solicitacao.ValueObjects;
using Domain.Solicitacao.Entities;

namespace Domain.Solicitacao.Factories;

public static class AuthorizationRequestFactory
{
    public static AuthorizationRequest Create(
        Guid beneficiaryId,
        PlanNumber planNumber,
        ProcedureCode procedureCode,
        CidCode cidCode,
        ProfessionalRegistry requestingProfessional,
        string executingEstablishment,
        DateTime expectedDate,
        List<RequestedItem> items,
        bool isUrgentOrEmergency)
    {
        var request = new AuthorizationRequest(
            Guid.NewGuid(),
            beneficiaryId,
            planNumber,
            procedureCode,
            cidCode,
            requestingProfessional,
            executingEstablishment,
            expectedDate,
            items,
            isUrgentOrEmergency
        );

        if (request.IsUrgentOrEmergency)
        {
            request.SetAsEmergencyException();
        }

        return request;
    }
}
