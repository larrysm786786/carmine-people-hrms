using CarminePeople.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace CarminePeople.Infrastructure.Storage;

public class DocumentStorageOptions
{
    public string RootPath { get; set; } = "App_Data/documents";
    public string PublicBaseUrl { get; set; } = "http://localhost:5080/internal-documents";
}

// Dev/local-disk implementation of IDocumentStorage, private (not served directly by
// static file middleware). Swap for an Azure Blob Storage implementation of the same
// interface when a storage account is provisioned (see docs/deployment.md) — no
// Application-layer code needs to change since callers only depend on IDocumentStorage.
public class LocalFileDocumentStorage : IDocumentStorage
{
    private readonly DocumentStorageOptions _options;

    public LocalFileDocumentStorage(IOptions<DocumentStorageOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(_options.RootPath);
    }

    public async Task<string> SaveAsync(string suggestedFileName, Stream content, CancellationToken ct)
    {
        var safeName = Path.GetFileName(suggestedFileName);
        var storageKey = $"{Guid.NewGuid()}_{safeName}";
        var fullPath = Path.Combine(_options.RootPath, storageKey);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return storageKey;
    }

    public Task<string> GetAccessUrlAsync(string storagePath, TimeSpan validFor, CancellationToken ct)
    {
        // Local-disk stand-in for a short-lived SAS URL: points at DocumentsController's
        // authenticated streaming route (which calls OpenReadAsync below) rather than a
        // directly-fetchable static path, since there's no static-file server to sign a
        // URL for. Swap for a real signed/expiring SAS URL when moving to Azure Blob.
        return Task.FromResult($"{_options.PublicBaseUrl}/{Uri.EscapeDataString(storagePath)}");
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_options.RootPath, storagePath);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_options.RootPath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}
