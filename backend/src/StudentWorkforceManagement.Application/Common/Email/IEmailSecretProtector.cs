namespace StudentWorkforceManagement.Application.Common.Email;

public interface IEmailSecretProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}
