using System;
using System.Collections.Generic;
using Domain.Aggregates.Autorizacoes;
using Domain.ValueObjects.Planos;
using Domain.ValueObjects.Procedimentos;
using Domain.ValueObjects.RedeCredenciada;

namespace Domain.Factories.Autorizacoes;

public static class AuthorizationRequestFactory
{
    public static AuthorizationRequest Create(
        Guid beneficiaryId,
        PlanNumber planNumber,
        ProcedureCode procedureCode,
        CidCode clinicalJustification,
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
            clinicalJustification,
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
