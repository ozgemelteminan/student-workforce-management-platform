namespace StudentWorkforceManagement.Domain.Common;

public interface IHasConcurrencyToken
{
    byte[] RowVersion { get; set; }
}
