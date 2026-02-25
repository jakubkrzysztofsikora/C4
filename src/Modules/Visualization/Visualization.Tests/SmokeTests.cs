namespace C4.Modules.Visualization.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void ModuleAssembly_Loads()
    {
        C4.Modules.Visualization.Domain.AssemblyReference.Assembly.Should().NotBeNull();
    }
}
