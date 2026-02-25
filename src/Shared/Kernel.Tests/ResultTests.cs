using C4.Shared.Kernel;

namespace C4.Shared.Kernel.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithValue_ReturnsSuccessResult()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_WithError_ReturnsFailureResult()
    {
        var error = new Error("sample", "failure");

        var result = Result<int>.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_OnSuccess_CallsNext()
    {
        var result = Result<int>.Success(5);

        var bound = result.Bind(value => Result<string>.Success($"{value}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("5");
    }

    [Fact]
    public void Bind_OnFailure_ShortCircuits()
    {
        var error = new Error("sample", "failure");
        var result = Result<int>.Failure(error);

        var bound = result.Bind(value => Result<string>.Success($"{value}"));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }
}
