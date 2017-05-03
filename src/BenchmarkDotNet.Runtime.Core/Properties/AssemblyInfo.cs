using System.Runtime.CompilerServices;

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet")]
#endif