using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Identity.Api")]

namespace C4.Modules.Identity.Infrastructure;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
