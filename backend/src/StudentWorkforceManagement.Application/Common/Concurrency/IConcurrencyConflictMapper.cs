namespace StudentWorkforceManagement.Application.Common.Concurrency;

public interface IConcurrencyConflictMapper
{
    string CreateConflictMessage(string resourceName);
}
