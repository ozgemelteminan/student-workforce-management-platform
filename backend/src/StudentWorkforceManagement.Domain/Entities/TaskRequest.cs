using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class TaskRequest : Entity, IHasConcurrencyToken
{
    public Guid TaskId { get; set; }
    public Guid RequestedById { get; set; }
    public RequestType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? RequestedDeadline { get; set; }
    public Guid? SuggestedStudentId { get; set; }
    public RequestStatus Status { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? ReviewerComment { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
