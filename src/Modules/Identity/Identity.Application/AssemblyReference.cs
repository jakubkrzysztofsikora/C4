using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Identity.Tests")]

namespace C4.Modules.Identity.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
