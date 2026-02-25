using System.Reflection;

namespace C4.Modules.Discovery.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
