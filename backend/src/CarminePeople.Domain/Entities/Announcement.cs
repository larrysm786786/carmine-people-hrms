using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class Announcement : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // References EmployeeDocument records uploaded via the existing documents pipeline
    // (DocumentType.Other), so image/attachment storage and access control reuse the same
    // authenticated-download infrastructure rather than a separate mechanism.
    public Guid? ImageDocumentId { get; set; }
    public Guid? AttachmentDocumentId { get; set; }

    public AnnouncementAudienceType AudienceType { get; set; } = AnnouncementAudienceType.All;

    // Holds department/location/employee IDs depending on AudienceType. JSON array of GUIDs.
    public string? AudienceFilterJson { get; set; }

    public DateTime PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public Guid CreatedByEmployeeId { get; set; }
    public Employee CreatedByEmployee { get; set; } = null!;
}
