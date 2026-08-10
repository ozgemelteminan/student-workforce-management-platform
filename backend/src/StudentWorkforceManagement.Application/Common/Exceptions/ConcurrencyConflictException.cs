namespace StudentWorkforceManagement.Application.Common.Exceptions;

public sealed class ConcurrencyConflictException(string message = "The resource was changed by another operation. Refresh and retry.")
    : ConflictException(message);
