using System.Reflection;

namespace C4.Modules.Telemetry.Api;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
