using Microsoft.AspNetCore.Identity;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Security.Password;

public sealed class IdentityPasswordService(IPasswordHasher<User> passwordHasher) : IPasswordService
{
    public bool VerifyPassword(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public string HashPassword(User user, string password) => passwordHasher.HashPassword(user, password);
}
