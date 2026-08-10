using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Feedback : Entity
{
    public Guid TaskId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CreatedById { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}
