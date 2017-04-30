using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Portability
{
    public abstract class RuntimeInformation
    {
        internal const string Unknown = "?";
        internal const string DebugConfigurationName = "DEBUG";
        internal const string ReleaseConfigurationName = "RELEASE";

        public static readonly RuntimeInformation Current;

        static RuntimeInformation()
        {
            var typeInfo = typeof(RuntimeInformation).GetTypeInfo();

            var location = typeInfo.Assembly.ReadProperty<Assembly, string>("Location");

            var directoryPath = Path.GetDirectoryName(location);

            var runtimeSpecificDll = new DirectoryInfo(directoryPath).EnumerateFileSystemInfos("BenchmarkDotNet.Runtime*.dll").Single();

            string assemblyQualifiedName = typeInfo.AssemblyQualifiedName;

            var runtimeSpecificImplementation = Type.GetType(assemblyQualifiedName.Replace("BenchmarkDotNet.Core", runtimeSpecificDll.Name));

            var instanceField = runtimeSpecificImplementation.GetRuntimeFields().Single(field => field.IsStatic && field.Name == "Instance");

            Current = (RuntimeInformation)instanceField.GetValue(null);
        }

        public virtual bool IsMono => false;

        public abstract bool IsWindows { get; }
        public abstract bool IsLinux { get; }
        public abstract bool IsMac { get; }

        public virtual string OsVersion => $"{Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystem} {Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystemVersion}";

        public virtual string ExecutableExtension => IsWindows ? ".exe" : string.Empty;
        public virtual string ScriptFileExtension => IsWindows ? ".bat" : ".sh";

        public virtual string Architecture => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

        public virtual Platform CurrentPlatform
        {
            get
            {
                switch (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture)
                {
                    case System.Runtime.InteropServices.Architecture.X86:
                        return Platform.X86;
                    case System.Runtime.InteropServices.Architecture.X64:
                        return Platform.X64;
                    case System.Runtime.InteropServices.Architecture.Arm:
                        return Platform.ARM;
                    case System.Runtime.InteropServices.Architecture.Arm64:
                        return Platform.ARM64;
                    default:
                        throw new PlatformNotSupportedException();
                }
            }
        }

        public abstract string GetProcessorName();

        public abstract Runtime CurrentRuntime { get; }
        public abstract string GetRuntimeVersion();

        public abstract bool HasRyuJit { get; }

        public abstract string JitInfo { get; }

        public Jit CurrentJit => HasRyuJit ? Jit.RyuJit : Jit.LegacyJit;

        public string JitModulesInfo => JitModules.Any() ? string.Join(";", JitModules.Select(m => m.Name + "-v" + m.Version)) : Unknown;
        
        protected virtual IEnumerable<JitModule> JitModules => Array.Empty<JitModule>();

        public abstract string GetConfiguration();

        public IntPtr GetCurrentAffinity()
        {
            try
            {
                return Process.GetCurrentProcess().ProcessorAffinity;
            }
            catch (PlatformNotSupportedException)
            {
                return default(IntPtr);
            }
        }

        public class JitModule
        {
            public string Name { get; }
            public string Version { get; }

            public JitModule(string name, string version)
            {
                Name = name;
                Version = version;
            }
        }
    }
}