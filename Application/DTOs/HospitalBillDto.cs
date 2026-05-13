using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class HospitalBillDto
{
    public Guid Id { get; set; }
    public Guid BeneficiaryId { get; set; }
    public string ExecutingEstablishment { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public List<HospitalBillItemDto> Items { get; set; } = new();
}

public class HospitalBillItemDto
{
    public Guid Id { get; set; }
    public Guid ApprovedAuthorizationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
}
