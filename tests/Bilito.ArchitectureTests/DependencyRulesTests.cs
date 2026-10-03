using System.Reflection;

namespace Bilito.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void DomainDoesNotReferenceInfrastructureOrApi()
    {
        var references = ReferencedAssemblyNames(typeof(Bilito.Identity.Domain.AssemblyMarker).Assembly);

        Assert.DoesNotContain("Bilito.Identity.Infrastructure", references);
        Assert.DoesNotContain("Bilito.Api", references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var references = ReferencedAssemblyNames(typeof(Bilito.Identity.Application.Abstractions.IIdentityModuleMarker).Assembly);

        Assert.DoesNotContain("Bilito.Identity.Infrastructure", references);
        Assert.DoesNotContain("Bilito.Api", references);
    }

    private static HashSet<string> ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .ToHashSet(StringComparer.Ordinal);
}
