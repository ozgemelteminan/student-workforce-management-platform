using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.UnitTests.Foundation;

public sealed class CanonicalDomainNamesTests
{
    [Fact]
    public void Canonical_domain_entities_are_present_without_student_number()
    {
        Assert.NotNull(typeof(TaskAssignmentHistory));
        Assert.NotNull(typeof(TaskRequest));
        Assert.NotNull(typeof(SubmissionVersion));

        var domainAssembly = typeof(Student).Assembly;
        Assert.DoesNotContain(domainAssembly.GetTypes(), type => type.Name.Contains("StudentNumber", StringComparison.OrdinalIgnoreCase));
    }
}
