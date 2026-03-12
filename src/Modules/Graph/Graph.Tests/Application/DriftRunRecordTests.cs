using C4.Shared.Kernel.Contracts;

namespace C4.Modules.Graph.Tests.Application;

[Trait("Category", "Unit")]
public sealed class DriftRunRecordTests
{
    [Fact]
    public void DriftRunRecord_Creation_SetsAllProperties()
    {
        var lastRunAt = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var record = new DriftRunRecord(lastRunAt, DriftRunStatus.Completed, null, 3);

        using var scope = new FluentAssertions.Execution.AssertionScope();
        record.LastRunAtUtc.Should().Be(lastRunAt);
        record.Status.Should().Be(DriftRunStatus.Completed);
        record.Error.Should().BeNull();
        record.DriftedCount.Should().Be(3);
    }

    [Fact]
    public void DriftRunRecord_WithError_SetsErrorProperty()
    {
        var record = new DriftRunRecord(null, DriftRunStatus.Failed, "Connection timeout", 0);

        using var scope = new FluentAssertions.Execution.AssertionScope();
        record.Status.Should().Be(DriftRunStatus.Failed);
        record.Error.Should().Be("Connection timeout");
        record.LastRunAtUtc.Should().BeNull();
    }

    [Fact]
    public void DriftRunStatus_HasExpectedValues()
    {
        using var scope = new FluentAssertions.Execution.AssertionScope();
        ((int)DriftRunStatus.NotRun).Should().Be(0);
        Enum.IsDefined(typeof(DriftRunStatus), DriftRunStatus.NotRun).Should().BeTrue();
        Enum.IsDefined(typeof(DriftRunStatus), DriftRunStatus.Running).Should().BeTrue();
        Enum.IsDefined(typeof(DriftRunStatus), DriftRunStatus.Completed).Should().BeTrue();
        Enum.IsDefined(typeof(DriftRunStatus), DriftRunStatus.Failed).Should().BeTrue();
    }
}
