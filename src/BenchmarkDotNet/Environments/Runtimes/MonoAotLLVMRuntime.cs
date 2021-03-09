using System;
using System.ComponentModel;
using System.IO;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class MonoAotLLVMRuntime : Runtime, IEquatable<MonoAotLLVMRuntime>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly MonoAotLLVMRuntime Default = new MonoAotLLVMRuntime();

        public FileInfo AOTCompilerPath { get; }

        /// <summary>
        /// creates new instance of MonoAotLLVMRuntime
        /// </summary>
        public MonoAotLLVMRuntime(FileInfo aotCompilerPath, string msBuildMoniker = "net6.0", string displayName = "MonoAOTLLVM") : base(RuntimeMoniker.MonoAOTLLVM, msBuildMoniker, displayName)
        {
            if (aotCompilerPath == null)
                throw new ArgumentNullException(paramName: nameof(aotCompilerPath));
            if (aotCompilerPath.IsNotNullButDoesNotExist())
                throw new FileNotFoundException($"Provided {nameof(aotCompilerPath)} file: \"{aotCompilerPath.FullName}\" doest NOT exist");

            AOTCompilerPath = aotCompilerPath;
        }

        // this ctor exists only for the purpose of having .Default property that returns something consumable by RuntimeInformation.GetCurrentRuntime()
        private MonoAotLLVMRuntime(string msBuildMoniker = "net6.0", string displayName = "MonoAOTLLVM") : base(RuntimeMoniker.MonoAOTLLVM, msBuildMoniker, displayName)
        {
            AOTCompilerPath = new FileInfo("fake");
        }

        public override bool Equals(object obj)
            => obj is MonoAotLLVMRuntime other && Equals(other);

        public bool Equals(MonoAotLLVMRuntime other)
            => other != null && base.Equals(other) && other.AOTCompilerPath == AOTCompilerPath;

        public override int GetHashCode()
            => base.GetHashCode() ^ AOTCompilerPath.GetHashCode();
    }
}
