namespace C4.Modules.Discovery.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void ModuleAssembly_Loads()
    {
        C4.Modules.Discovery.Domain.AssemblyReference.Assembly.Should().NotBeNull();
    }
}
