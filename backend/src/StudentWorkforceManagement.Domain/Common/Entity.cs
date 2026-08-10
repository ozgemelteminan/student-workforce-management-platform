namespace StudentWorkforceManagement.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
