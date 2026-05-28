using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Audit;
using Domain.Billing;

namespace Domain.Billing;

public class HospitalBill
{
    public Guid Id { get; private set; }
    public Guid BeneficiaryId { get; private set; }
    public string ExecutingEstablishment { get; private set; }
    public HospitalBillStatus Status { get; private set; }
    private readonly List<BillItem> _items;
    public IReadOnlyCollection<BillItem> Items => _items.AsReadOnly();
    public Money TotalValue => _items
        .Select(item => item.TotalValue)
        .DefaultIfEmpty(new Money(0))
        .Aggregate((current, next) => current.Add(next));

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
        Status = HospitalBillStatus.Open;
        _items = new List<BillItem>();
    }

    public static HospitalBill Restore(
        Guid id,
        Guid beneficiaryId,
        string executingEstablishment,
        HospitalBillStatus status,
        List<BillItem> items)
    {
        var bill = new HospitalBill(id, beneficiaryId, executingEstablishment);
        bill._items.AddRange(items ?? new List<BillItem>());
        bill.Status = status;

        return bill;
    }

    public void AddItem(BillItem item)
    {
        EnsureOpen();

        if (item == null)
            throw new ArgumentNullException(nameof(item));
        if (_items.Any() && _items[0].UnitValue.Currency != item.UnitValue.Currency)
            throw new InvalidOperationException("Não é possível adicionar itens com moedas diferentes na mesma conta hospitalar.");

        _items.Add(item);
    }

    public Glosa ApplyGlosaToItem(Guid itemId, GlosaReason reason, string details)
    {
        EnsureOpen();

        var item = FindItem(itemId);
        return item.ApplyGlosa(reason, details);
    }

    public Glosa ApplyClawbackAuditGlosaToItem(Guid itemId, GlosaReason reason, string auditDetails)
    {
        EnsureOpen();

        var item = FindItem(itemId);
        return item.ApplyClawbackAuditGlosa(reason, auditDetails);
    }

    public void FileAppeal(Guid itemId, Guid glosaId, List<Evidence> evidenceDocuments)
    {
        EnsureOpen();

        var item = FindItem(itemId);
        item.FileAppeal(glosaId, evidenceDocuments);
    }

    public void Close()
    {
        EnsureOpen();

        if (!_items.Any())
            throw new InvalidOperationException("Uma conta hospitalar precisa ter itens para ser fechada.");

        Status = HospitalBillStatus.Closed;
    }

    private BillItem FindItem(Guid itemId)
    {
        return _items.SingleOrDefault(item => item.Id == itemId)
            ?? throw new InvalidOperationException("Item de conta hospitalar não encontrado.");
    }

    private void EnsureOpen()
    {
        if (Status != HospitalBillStatus.Open)
            throw new InvalidOperationException("Conta hospitalar fechada não pode ser alterada.");
    }
}
