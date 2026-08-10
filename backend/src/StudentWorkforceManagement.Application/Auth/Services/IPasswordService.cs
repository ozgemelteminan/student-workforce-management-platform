using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Application.Auth.Services;

public interface IPasswordService
{
    bool VerifyPassword(User user, string password);
    string HashPassword(User user, string password);
}
