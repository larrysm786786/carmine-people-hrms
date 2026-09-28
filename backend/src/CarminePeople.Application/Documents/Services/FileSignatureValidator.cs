namespace CarminePeople.Application.Documents.Services;

// Validates that a file's actual content matches its claimed Content-Type via magic-byte
// (file signature) sniffing, since the client-supplied Content-Type header is trivially
// spoofable (e.g. uploading a renamed .html file labeled as image/png). Only covers the
// types DocumentService actually allowlists.
public static class FileSignatureValidator
{
    public static async Task<bool> MatchesAsync(Stream content, string contentType, CancellationToken ct)
    {
        if (!content.CanSeek) return true; // can't sniff a non-seekable stream; caller must ensure seekability upstream

        var header = new byte[8];
        var startPosition = content.Position;
        var bytesRead = await content.ReadAsync(header, 0, header.Length, ct);
        content.Position = startPosition;

        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => bytesRead >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46, // %PDF
            "image/png" => bytesRead >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            "image/jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            // Legacy .doc (OLE compound file) and modern .docx (a zip/PK archive) share this
            // content-type-agnostic pair of signatures; both are accepted for either MIME type
            // since browsers are inconsistent about which they send for .docx.
            "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" =>
                (bytesRead >= 4 && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0) // OLE (.doc)
                || (bytesRead >= 4 && header[0] == 0x50 && header[1] == 0x4B && (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07)), // ZIP (.docx)
            _ => false,
        };
    }
}
