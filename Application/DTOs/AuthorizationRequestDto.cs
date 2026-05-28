using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class AuthorizationRequestDto
{
    public Guid BeneficiaryId { get; set; }
    public string PlanNumber { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public string CidCode { get; set; } = string.Empty;
    public string RequestingProfessional { get; set; } = string.Empty;
    public string ExecutingEstablishment { get; set; } = string.Empty;
    public DateTime ExpectedDate { get; set; }
    public List<RequestedItemDto> RequestedItems { get; set; } = new();
    public bool IsUrgentOrEmergency { get; set; }
}

public class RequestedItemDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ItemType { get; set; }
}
