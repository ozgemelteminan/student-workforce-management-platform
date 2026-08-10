namespace StudentWorkforceManagement.Application.Common.Exceptions;

public sealed class ApplicationValidationException(IReadOnlyDictionary<string, string[]> errors)
    : StudentWorkforceApplicationException("One or more validation failures occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
