using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Properties;

[assembly: ComVisible(false)]

[assembly: Guid("fde49815-6c06-4a50-b6af-7291668955d6")]

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet")]
#endif