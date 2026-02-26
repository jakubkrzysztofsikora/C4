using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Feedback.Api")]
[assembly: InternalsVisibleTo("Feedback.Tests")]

namespace C4.Modules.Feedback.Infrastructure;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
