namespace C4.Modules.Graph.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void ModuleAssembly_Loads()
    {
        C4.Modules.Graph.Domain.AssemblyReference.Assembly.Should().NotBeNull();
    }
}
