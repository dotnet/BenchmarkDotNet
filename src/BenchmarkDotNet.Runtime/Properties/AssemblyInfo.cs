using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Properties;

[assembly: Guid("95f5d645-19e3-432f-95d4-c5ea374dd15b")]
[assembly: CLSCompliant(true)] // for some reason this setting is not taken from common.props file

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet.Toolchains.Roslyn,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Samples,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.Windows,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Uap,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Runtime.Uap,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Runtime.Classic,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Runtime.Core,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet.Toolchains.Roslyn")]
[assembly: InternalsVisibleTo("BenchmarkDotNet")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Samples")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.Windows")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Uap")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Runtime.Uap")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Runtime.Classic")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Runtime.Core")]
#endif