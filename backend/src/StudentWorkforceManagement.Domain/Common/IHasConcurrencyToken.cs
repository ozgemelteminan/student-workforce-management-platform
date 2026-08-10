namespace StudentWorkforceManagement.Domain.Common;

public interface IHasConcurrencyToken
{
    Guid ConcurrencyToken { get; set; }
}
