namespace C4.Modules.Identity.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void ModuleAssembly_Loads()
    {
        C4.Modules.Identity.Domain.AssemblyReference.Assembly.Should().NotBeNull();
    }
}
