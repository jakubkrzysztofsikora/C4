namespace C4.Modules.Telemetry.Domain.Metrics;

public static class ServiceHealthStatusExtensions
{
    public static ServiceHealthStatus FromScore(double value) => value switch
    {
        >= 0.8 => ServiceHealthStatus.Green,
        >= 0.5 => ServiceHealthStatus.Yellow,
        _ => ServiceHealthStatus.Red
    };
}
