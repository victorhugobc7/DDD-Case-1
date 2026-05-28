using System;
using System.Collections.Generic;

namespace Domain.Audit;

public record Evidence
{
    public string DocumentUrl { get; }
    public string Description { get; }

    public Evidence(string documentUrl, string description)
    {
        if (string.IsNullOrWhiteSpace(documentUrl))
            throw new ArgumentException("A URL do documento de evidência não pode ser vazia.", nameof(documentUrl));

        DocumentUrl = documentUrl;
        Description = description ?? string.Empty;
    }
}
