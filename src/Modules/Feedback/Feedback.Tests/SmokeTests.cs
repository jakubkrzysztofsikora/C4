namespace C4.Modules.Feedback.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void ModuleAssembly_Loads()
    {
        C4.Modules.Feedback.Domain.AssemblyReference.Assembly.Should().NotBeNull();
    }
}
