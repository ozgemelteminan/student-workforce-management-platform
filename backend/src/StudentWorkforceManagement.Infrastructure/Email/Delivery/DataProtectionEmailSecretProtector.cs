using Microsoft.AspNetCore.DataProtection;
using StudentWorkforceManagement.Application.Common.Email;

namespace StudentWorkforceManagement.Infrastructure.Email.Delivery;

public sealed class DataProtectionEmailSecretProtector(IDataProtectionProvider dataProtectionProvider) : IEmailSecretProtector
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("StudentWorkforceManagement.EmailTemplateSecrets.v1");

    public string Protect(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return _protector.Protect(secret);
    }

    public string Unprotect(string protectedSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedSecret);
        return _protector.Unprotect(protectedSecret);
    }
}
