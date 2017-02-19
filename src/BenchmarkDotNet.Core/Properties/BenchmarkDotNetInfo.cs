using System;
using System.Reflection;

namespace BenchmarkDotNet.Properties
{
    public static class BenchmarkDotNetInfo
    {
        public static readonly Lazy<string> FullVersion = new Lazy<string>(() => typeof(BenchmarkDotNetInfo).GetTypeInfo().Assembly.GetName().Version.ToString());
        public static readonly Lazy<string> FullTitle = new Lazy<string>(() => "BenchmarkDotNet v" + FullVersion.Value);

        internal const string PublicKey =
            "00240000048000009400000006020000002400005253413100040000010001002970bbdfca4d12" +
            "9fc74b4845b239973f1b183684f0d7db5e1de7e085917e3656cf94884803cb800d85d5aae5838f" +
            "b3f8fd1f2829e8208c4f087afcfe970bce44037ba30a66749cd5514b410ca8a35e9c7d6eb86975" +
            "853c834c9ad25051537f9a05a0c540c5d84f2c7b32ab01619d84367fd424797ba3242f08b0e6ae" +
            "75f66dad";
    }
}