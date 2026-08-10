using StudentWorkforceManagement.Api.Contracts.V1;

namespace StudentWorkforceManagement.IntegrationTests.Foundation;

public sealed class ApiVersionFoundationTests
{
    [Fact]
    public void Api_version_prefix_is_v1()
    {
        Assert.Equal("/api/v1", ApiRoutes.VersionPrefix);
    }
}
