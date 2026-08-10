using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Domain.ValueObjects;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskRequest : AuditableEntity, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }
    public Guid RequestedById { get; set; }
    public Student? RequestedBy { get; set; }
    public RequestType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? CurrentDeadline { get; set; }
    public DateTimeOffset? RequestedDeadline { get; set; }
    public Guid? SuggestedStudentId { get; set; }
    public Student? SuggestedStudent { get; set; }
    public RequestStatus Status { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ReviewerComment { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
