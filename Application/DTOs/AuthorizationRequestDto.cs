using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class AuthorizationRequestDto
{
    public Guid BeneficiaryId { get; set; }
    public string PlanNumber { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public string ClinicalJustification { get; set; } = string.Empty;
    public string RequestingProfessional { get; set; } = string.Empty;
    public string ExecutingEstablishment { get; set; } = string.Empty;
    public DateTime ExpectedDate { get; set; }
    public List<string> MaterialsAndMedicines { get; set; } = new();
    public bool IsUrgentOrEmergency { get; set; }
}
