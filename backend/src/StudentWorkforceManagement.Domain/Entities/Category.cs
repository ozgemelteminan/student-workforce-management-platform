using StudentWorkforceManagement.Domain.Common;
using StudentWorkforceManagement.Domain.Enums;

namespace StudentWorkforceManagement.Domain.Entities;

public sealed class Category : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
