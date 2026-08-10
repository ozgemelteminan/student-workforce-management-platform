namespace StudentWorkforceManagement.Application.Common.Security;

public interface ISecureTokenGenerator
{
    string GenerateToken();
    string HashToken(string rawToken);
}
