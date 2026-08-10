using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.ArchitectureTests.Foundation;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_project_does_not_reference_outer_layers()
    {
        var referencedAssemblies = typeof(Student).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();

        Assert.DoesNotContain("StudentWorkforceManagement.Application", referencedAssemblies);
        Assert.DoesNotContain("StudentWorkforceManagement.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("StudentWorkforceManagement.Api", referencedAssemblies);
    }
}
