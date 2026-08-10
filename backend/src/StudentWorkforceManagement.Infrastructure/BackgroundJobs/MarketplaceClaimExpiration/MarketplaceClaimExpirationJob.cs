using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Common.Time;
using StudentWorkforceManagement.Domain.Enums;
using StudentWorkforceManagement.Infrastructure.BackgroundJobs;
using StudentWorkforceManagement.Infrastructure.Persistence;

namespace StudentWorkforceManagement.Infrastructure.BackgroundJobs.MarketplaceClaimExpiration;

public sealed class MarketplaceClaimExpirationJob(ApplicationDbContext dbContext, IUtcClock clock, IOptions<BackgroundJobOptions> options, ILogger<MarketplaceClaimExpirationJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var claims = await dbContext.MarketplaceClaims
            .Where(claim => claim.Status == MarketplaceClaimStatus.PENDING && claim.ExpiresAt != null && claim.ExpiresAt <= now)
            .OrderBy(claim => claim.ExpiresAt)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);
        foreach (var claim in claims)
        {
            claim.Status = MarketplaceClaimStatus.EXPIRED;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("MarketplaceClaimExpirationJob expired {Count} claims", claims.Count);
        return claims.Count;
    }
}
