using System;
using System.Collections.Generic;

namespace Domain.Aggregates.Faturamento;

public class HospitalBill
{
    public Guid Id { get; private set; }
    public Guid BeneficiaryId { get; private set; }
    public string ExecutingEstablishment { get; private set; }
    private readonly List<BillItem> _items;
    public IReadOnlyCollection<BillItem> Items => _items.AsReadOnly();

    public HospitalBill(Guid id, Guid beneficiaryId, string executingEstablishment)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O id da conta hospitalar é inválido.", nameof(id));
        if (beneficiaryId == Guid.Empty)
            throw new ArgumentException("O id do beneficiário é inválido.", nameof(beneficiaryId));
        if (string.IsNullOrWhiteSpace(executingEstablishment))
            throw new ArgumentException("O estabelecimento executante não pode ser vazio.", nameof(executingEstablishment));

        Id = id;
        BeneficiaryId = beneficiaryId;
        ExecutingEstablishment = executingEstablishment;
        _items = new List<BillItem>();
    }

    public void AddItem(BillItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        _items.Add(item);
    }
}
