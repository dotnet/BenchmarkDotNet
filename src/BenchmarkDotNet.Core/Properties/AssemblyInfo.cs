using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Properties;

[assembly: AssemblyTitle(BenchmarkDotNetInfo.Title + ".Core")]
[assembly: AssemblyProduct(BenchmarkDotNetInfo.Title + ".Core")]
[assembly: AssemblyDescription(BenchmarkDotNetInfo.Description + ".Core")]
[assembly: AssemblyCopyright(BenchmarkDotNetInfo.Copyright)]
[assembly: AssemblyVersion(BenchmarkDotNetInfo.Version)]
[assembly: AssemblyFileVersion(BenchmarkDotNetInfo.Version)]

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("95f5d645-19e3-432f-95d4-c5ea374dd15b")]

[assembly: CLSCompliant(true)]

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet.Toolchains.Roslyn,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Samples,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet.Toolchains.Roslyn")]
[assembly: InternalsVisibleTo("BenchmarkDotNet")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Samples")]
#endif