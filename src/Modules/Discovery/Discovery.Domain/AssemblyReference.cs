using System.Reflection;

namespace C4.Modules.Discovery.Domain;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
