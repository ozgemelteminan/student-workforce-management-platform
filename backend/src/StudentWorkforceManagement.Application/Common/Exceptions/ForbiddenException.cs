namespace StudentWorkforceManagement.Application.Common.Exceptions;

public sealed class ForbiddenException(string message = "The current user is not authorized to perform this action.")
    : StudentWorkforceApplicationException(message);
