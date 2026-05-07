using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class AuthorizationRequestDto
{
    public Guid BeneficiaryId { get; set; }
    public string PlanNumber { get; set; }
    public string ProcedureCode { get; set; }
    public string ClinicalJustification { get; set; }
    public string RequestingProfessional { get; set; }
    public string ExecutingEstablishment { get; set; }
    public DateTime ExpectedDate { get; set; }
    public List<string> MaterialsAndMedicines { get; set; }
    public bool IsUrgentOrEmergency { get; set; }
}
