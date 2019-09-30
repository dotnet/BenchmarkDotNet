using System;
using System.IO;
using System.Reflection;
// THIS TYPE MUST NOT HAVE ANY DEPENDENCIES TO BENCHMARKDOTNET, VALUE TUPLE, IMMUTABLE TYPES OR ANYTHING COMPLEX!!

/// <summary>
/// Sometimes NuGet/VS/other tool does not generate the right assembly binding redirects
/// or just for any other magical reasons
/// our users get FileNotFoundException when trying to run their benchmarks
///
/// We want our users to be happy and we try to help the .NET framework when it fails to load an assembly
///
/// It's not recommended to copy this code OR reuse it anywhere. It's an UGLY WORKAROUND.
///
/// If one day we can remove it, the person doing that should celebrate!!
/// </summary>
// ReSharper disable once CheckNamespace I did it on purpose to show that it MUST not touch any BenchmarkDotNet stuff
internal class DirtyAssemblyResolveHelper : IDisposable
{
    internal static IDisposable Create() => new DirtyAssemblyResolveHelper();

    private DirtyAssemblyResolveHelper() => AppDomain.CurrentDomain.AssemblyResolve += HelpTheFrameworkToResolveTheAssembly;

    public void Dispose() => AppDomain.CurrentDomain.AssemblyResolve -= HelpTheFrameworkToResolveTheAssembly;

    /// <summary>
    /// according to https://msdn.microsoft.com/en-us/library/ff527268(v=vs.110).aspx
    /// "the handler is invoked whenever the runtime fails to bind to an assembly by name."
    /// </summary>
    /// <returns>not null when we find it manually, null when can't help</returns>
    private Assembly HelpTheFrameworkToResolveTheAssembly(object sender, ResolveEventArgs args)
    {
        var fullName = new AssemblyName(args.Name);
        string simpleName = fullName.Name;

        string guessedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{simpleName}.dll");

        if (!File.Exists(guessedPath))
            return null; // we can't help, and we also don't call Assembly.Load which if fails comes back here, creates endless loop and causes StackOverflow

        // the file is right there, but has most probably different version and there is no assembly redirect
        // so we just load it and ignore the version mismatch
        // we can at least try because benchmarks are not executed in the Host process,
        // so even if we load some bad version of the assembly
        // we might still produce the right exe with proper references

        // we warn the user about that, in case some Super User want to be aware of that
        Console.WriteLine($"// Wrong assembly binding redirects for {simpleName}, loading it from disk anyway.");

        return Assembly.LoadFrom(guessedPath);
    }
}
