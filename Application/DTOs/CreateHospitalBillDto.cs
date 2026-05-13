using System;
using System.Collections.Generic;

namespace Application.DTOs;

public class CreateHospitalBillDto
{
    public Guid AuthorizationId { get; set; }
    public Dictionary<Guid, decimal> UnitValuesByItemId { get; set; } = new();
}
