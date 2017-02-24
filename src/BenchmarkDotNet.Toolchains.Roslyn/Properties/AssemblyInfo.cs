using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Properties;

[assembly: Guid("f5785871-7e2c-4894-85de-a28eacb1d9b5")]
[assembly: CLSCompliant(true)]

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet")]
#endif