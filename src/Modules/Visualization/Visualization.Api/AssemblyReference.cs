using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Visualization.Tests")]

namespace C4.Modules.Visualization.Api;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
