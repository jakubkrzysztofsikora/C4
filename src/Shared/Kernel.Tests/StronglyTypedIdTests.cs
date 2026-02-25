using C4.Shared.Kernel;

namespace C4.Shared.Kernel.Tests;

public sealed class StronglyTypedIdTests
{
    [Fact]
    public void New_GeneratesUniqueId()
    {
        var first = TestId.New();
        var second = TestId.New();

        first.Should().NotBe(second);
    }

    [Fact]
    public void Equality_SameValue_ReturnsTrue()
    {
        var value = Guid.NewGuid();

        var first = new TestId(value);
        var second = new TestId(value);

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_DifferentValue_ReturnsFalse()
    {
        var first = TestId.New();
        var second = TestId.New();

        first.Should().NotBe(second);
    }

    private sealed record TestId(Guid Value) : StronglyTypedGuidId(Value)
    {
        public static TestId New() => new(Guid.NewGuid());
    }
}
