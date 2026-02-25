namespace C4.Modules.Telemetry.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void ModuleAssembly_Loads()
    {
        C4.Modules.Telemetry.Domain.AssemblyReference.Assembly.Should().NotBeNull();
    }
}
