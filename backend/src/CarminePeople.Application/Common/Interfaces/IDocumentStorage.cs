namespace CarminePeople.Application.Common.Interfaces;

public interface IDocumentStorage
{
    // Stores the file content under a storage-provider-specific key and returns that key.
    Task<string> SaveAsync(string suggestedFileName, Stream content, CancellationToken ct);

    // Returns a short-lived, non-public access URL for the stored file (SAS URL on Azure Blob).
    // On the local-disk implementation this points at an authenticated API route rather than
    // a directly-fetchable URL, since there's no static-file server to sign a URL for.
    Task<string> GetAccessUrlAsync(string storagePath, TimeSpan validFor, CancellationToken ct);

    // Opens the raw file content for streaming through an authenticated endpoint. Only used
    // by the local-disk implementation's download route; a cloud-blob implementation would
    // typically not need this since GetAccessUrlAsync already returns a fetchable SAS URL.
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct);

    Task DeleteAsync(string storagePath, CancellationToken ct);
}
