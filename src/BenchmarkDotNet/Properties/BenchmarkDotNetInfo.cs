using System;
using System.Reflection;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Properties
{
    public class BenchmarkDotNetInfo
    {
        private static readonly Lazy<BenchmarkDotNetInfo> LazyInstance = new (() =>
        {
            var assembly = typeof(BenchmarkDotNetInfo).GetTypeInfo().Assembly;
            var assemblyVersion = assembly.GetName().Version;
            string informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion ?? "";
            return new BenchmarkDotNetInfo(assemblyVersion, RemoveVersionMetadata(informationVersion));
        });

        public static BenchmarkDotNetInfo Instance { get; } = LazyInstance.Value;

        public Version AssemblyVersion { get; }
        public string FullVersion { get; }

        public bool IsDevelop { get; }
        public bool IsNightly { get; }
        public bool IsRelease { get; }

        public string BrandTitle { get; }
        public string BrandVersion { get; }

        public BenchmarkDotNetInfo(Version assemblyVersion, string fullVersion)
        {
            AssemblyVersion = assemblyVersion;
            FullVersion = fullVersion;

            string versionPrefix = AssemblyVersion.Revision > 0
                ? AssemblyVersion.ToString()
                : AssemblyVersion.ToString(3);
            if (!FullVersion.StartsWith(versionPrefix))
                throw new ArgumentException($"Inconsistent versions: '{assemblyVersion}' and '{fullVersion}'");
            string versionSuffix = FullVersion.Substring(versionPrefix.Length).TrimStart('-');

            IsDevelop = versionSuffix.StartsWith("develop");
            IsNightly = AssemblyVersion.Revision > 0;
            IsRelease = versionSuffix.IsEmpty() && AssemblyVersion.Revision <= 0;

            string brandVersionSuffix = IsDevelop
                ? " (" + DateTime.Now.ToString("yyyy-MM-dd") + ")"
                : "";
            BrandVersion = FullVersion + brandVersionSuffix;
            BrandTitle = "BenchmarkDotNet v" + BrandVersion;
        }

        internal static string RemoveVersionMetadata(string version)
        {
            int index = version.IndexOf('+');
            return index >= 0 ? version.Substring(0, index) : version;
        }

        internal const string PublicKey =
            "00240000048000009400000006020000002400005253413100040000010001002970bbdfca4d12" +
            "9fc74b4845b239973f1b183684f0d7db5e1de7e085917e3656cf94884803cb800d85d5aae5838f" +
            "b3f8fd1f2829e8208c4f087afcfe970bce44037ba30a66749cd5514b410ca8a35e9c7d6eb86975" +
            "853c834c9ad25051537f9a05a0c540c5d84f2c7b32ab01619d84367fd424797ba3242f08b0e6ae" +
            "75f66dad";
    }
}