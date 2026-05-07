using System;
using System.Collections.Generic;

namespace Domain.Entities;

public class HospitalBill
{
    public Guid Id { get; private set; }
    public Guid BeneficiaryId { get; private set; }
    public string ExecutingEstablishment { get; private set; }
    private readonly List<BillItem> _items;
    public IReadOnlyCollection<BillItem> Items => _items.AsReadOnly();

    public HospitalBill(Guid id, Guid beneficiaryId, string executingEstablishment)
    {
        Id = id;
        BeneficiaryId = beneficiaryId;
        ExecutingEstablishment = executingEstablishment;
        _items = new List<BillItem>();
    }

    public void AddItem(BillItem item)
    {
        _items.Add(item);
    }
}
