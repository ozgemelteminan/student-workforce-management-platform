namespace StudentWorkforceManagement.Infrastructure.Security.RateLimiting;

public sealed record RateLimitPolicy(string Name, int PermitLimit, TimeSpan Window, string PartitionKey);
