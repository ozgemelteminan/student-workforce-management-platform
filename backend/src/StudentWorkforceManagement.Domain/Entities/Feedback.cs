using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Feedback : AuditableEntity
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}
