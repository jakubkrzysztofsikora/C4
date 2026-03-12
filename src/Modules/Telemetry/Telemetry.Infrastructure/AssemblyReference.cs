using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Telemetry.Tests")]

namespace C4.Modules.Telemetry.Infrastructure;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
