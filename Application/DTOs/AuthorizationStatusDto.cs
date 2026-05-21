using System;
using System.Collections.Generic;
using Domain.Enums.Autorizacoes;

namespace Application.DTOs;

public class AuthorizationStatusDto
{
    public Guid Id { get; set; }
    public AuthorizationStatus Status { get; set; }
    public bool RequiresPostPaymentAudit { get; set; }
    public string? DenialReason { get; set; }
    public string? PendingReason { get; set; }
    public List<AuthorizationItemStatusDto> Items { get; set; } = new();
}

public class AuthorizationItemStatusDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int ApprovedQuantity { get; set; }
}
