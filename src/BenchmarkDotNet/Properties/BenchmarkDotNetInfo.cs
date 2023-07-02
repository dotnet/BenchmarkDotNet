using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Properties
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class BenchmarkDotNetInfo
    {
#if PRERELEASE_NIGHTLY
        public const string PrereleaseLabel = "-nightly";
#elif PRERELEASE_DEVELOP
        public const string PrereleaseLabel = "-develop";
#else
        public const string PrereleaseLabel = "";
#endif

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        [SuppressMessage("ReSharper", "RedundantLogicalConditionalExpressionOperand")]
        private static readonly Lazy<string> FullVersionLazy = new Lazy<string>(() =>
        {
            string version = typeof(BenchmarkDotNetInfo).GetTypeInfo().Assembly.GetName().Version.ToString();
#pragma warning disable 162
            if (version.EndsWith(".0") && PrereleaseLabel == "")
                version = version.Substring(0, version.Length - 2);
            if (version.EndsWith(".0") && PrereleaseLabel == "-develop")
                version = version.Substring(0, version.Length - 1) + DateTime.Now.ToString("yyyyMMdd");
#pragma warning restore 162
            return version + PrereleaseLabel;
        });

        private static readonly Lazy<string> FullTitleLazy = new Lazy<string>(() => "BenchmarkDotNet v" + FullVersionLazy.Value);

        public static string FullVersion => FullVersionLazy.Value;

        public static string FullTitle => FullTitleLazy.Value;

        internal const string PublicKey =
            "00240000048000009400000006020000002400005253413100040000010001002970bbdfca4d12" +
            "9fc74b4845b239973f1b183684f0d7db5e1de7e085917e3656cf94884803cb800d85d5aae5838f" +
            "b3f8fd1f2829e8208c4f087afcfe970bce44037ba30a66749cd5514b410ca8a35e9c7d6eb86975" +
            "853c834c9ad25051537f9a05a0c540c5d84f2c7b32ab01619d84367fd424797ba3242f08b0e6ae" +
            "75f66dad";
    }
}