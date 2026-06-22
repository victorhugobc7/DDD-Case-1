using System;
using System.Collections.Generic;
using Domain.Common.Enums;
using Domain.Faturamento.Enums;

namespace Application.DTOs;

public class MoneyDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
}

public class HospitalBillDto
{
    public Guid Id { get; set; }
    public Guid BeneficiaryId { get; set; }
    public string ExecutingEstablishment { get; set; } = string.Empty;
    public HospitalBillStatus Status { get; set; }
    public MoneyDto TotalValue { get; set; } = new();
    public List<HospitalBillItemDto> Items { get; set; } = new();
}

public class HospitalBillItemDto
{
    public Guid Id { get; set; }
    public Guid ApprovedAuthorizationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public MoneyDto UnitValue { get; set; } = new();
    public MoneyDto TotalValue { get; set; } = new();
    public List<HospitalBillItemGlosaDto> Glosas { get; set; } = new();
}

public class HospitalBillItemGlosaDto
{
    public Guid Id { get; set; }
    public Guid BillItemId { get; set; }
    public GlosaReason Reason { get; set; }
    public string Details { get; set; } = string.Empty;
    public bool IsClawback { get; set; }
    public AdministrativeAppealDto? Appeal { get; set; }
}

public class AdministrativeAppealDto
{
    public Guid Id { get; set; }
    public Guid GlosaId { get; set; }
    public AppealStatus Status { get; set; }
    public List<EvidenceDto> EvidenceDocuments { get; set; } = new();
}

public class EvidenceDto
{
    public string DocumentUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ApplyGlosaDto
{
    public Guid HospitalBillId { get; set; }
    public Guid BillItemId { get; set; }
    public GlosaReason Reason { get; set; }
    public string Details { get; set; } = string.Empty;
}

public class FileGlosaAppealDto
{
    public Guid HospitalBillId { get; set; }
    public Guid BillItemId { get; set; }
    public Guid GlosaId { get; set; }
    public List<EvidenceDto> EvidenceDocuments { get; set; } = new();
}
