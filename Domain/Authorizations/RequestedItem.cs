using System;

namespace Domain.Authorizations;

public class RequestedItem
{
    public Guid Id { get; private set; }
    public string Description { get; private set; }
    public int RequestedQuantity { get; private set; }
    public int ApprovedQuantity { get; private set; }

    public RequestedItem(Guid id, string description, int requestedQuantity)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O id do item solicitado é inválido.", nameof(id));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descrição não pode ser vazia.", nameof(description));
        if (requestedQuantity <= 0)
            throw new ArgumentException("A quantidade solicitada deve ser maior que zero.", nameof(requestedQuantity));

        Id = id;
        Description = description;
        RequestedQuantity = requestedQuantity;
        ApprovedQuantity = 0;
    }

    public static RequestedItem Restore(Guid id, string description, int requestedQuantity, int approvedQuantity)
    {
        var item = new RequestedItem(id, description, requestedQuantity);
        item.ApprovePartially(approvedQuantity);

        return item;
    }

    internal void ApproveFully()
    {
        ApprovedQuantity = RequestedQuantity;
    }

    internal void ApprovePartially(int quantity)
    {
        if (quantity < 0 || quantity > RequestedQuantity)
            throw new ArgumentException("A quantidade aprovada inválida.", nameof(quantity));

        ApprovedQuantity = quantity;
    }

    internal void Deny()
    {
        ApprovedQuantity = 0;
    }
}
