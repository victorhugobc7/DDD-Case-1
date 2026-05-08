using System;
using System.Collections.Generic;

namespace Domain.ValueObjects.Auditoria;

public record Evidence
{
    public string DocumentUrl { get; init; }
    public string Description { get; init; }

    public Evidence(string documentUrl, string description)
    {
        if (string.IsNullOrWhiteSpace(documentUrl))
            throw new ArgumentException("A URL do documento de evidência não pode ser vazia.", nameof(documentUrl));

        DocumentUrl = documentUrl;
        Description = description ?? string.Empty;
    }
}
