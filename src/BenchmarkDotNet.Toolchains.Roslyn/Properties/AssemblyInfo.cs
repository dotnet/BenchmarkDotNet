using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Properties;

[assembly: AssemblyTitle(BenchmarkDotNetInfo.Title + ".Toolchains.Roslyn")]
[assembly: AssemblyProduct(BenchmarkDotNetInfo.Title + ".Toolchains.Roslyn")]
[assembly: AssemblyDescription(BenchmarkDotNetInfo.Description + ".Toolchains.Roslyn")]
[assembly: AssemblyCopyright(BenchmarkDotNetInfo.Copyright)]
[assembly: AssemblyVersion(BenchmarkDotNetInfo.Version)]
[assembly: AssemblyFileVersion(BenchmarkDotNetInfo.Version)]

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("f5785871-7e2c-4894-85de-a28eacb1d9b5")]

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet")]
#endif