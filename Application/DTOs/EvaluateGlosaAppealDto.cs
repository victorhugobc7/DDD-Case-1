using System;

namespace Application.DTOs;

public class EvaluateGlosaAppealDto
{
    public Guid HospitalBillId { get; set; }
    public Guid BillItemId { get; set; }
    public Guid GlosaId { get; set; }
    public bool Approve { get; set; }
}
