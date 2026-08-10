namespace StudentWorkforceManagement.Infrastructure.Security.RateLimiting;

public sealed class RateLimitOptions
{
    public int AuthenticatedRequestsPerMinutePerUser { get; init; } = 120;
    public int AnonymousRequestsPerMinutePerIp { get; init; } = 30;
    public int LoginFailedAttempts { get; init; } = 5;
    public TimeSpan LoginWindow { get; init; } = TimeSpan.FromMinutes(15);
    public int ForgotPasswordRequests { get; init; } = 3;
    public TimeSpan ForgotPasswordWindow { get; init; } = TimeSpan.FromHours(1);
    public int InvitationResendRequests { get; init; } = 5;
    public TimeSpan InvitationResendWindow { get; init; } = TimeSpan.FromHours(1);
    public int UploadInitiationRequestsPerMinutePerUser { get; init; } = 20;
}
