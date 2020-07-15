using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class WasmRuntime : Runtime, IEquatable<WasmRuntime>
    {
        public static readonly WasmRuntime Default = new WasmRuntime("Wasm");

        public WasmRuntime(string name) : base(RuntimeMoniker.Wasm, "net5.0", name)
        {
        }
        
        public WasmRuntime(string name, string msBuildMoniker) : base(RuntimeMoniker.Wasm, msBuildMoniker, name)
        {
        }

        public override bool Equals(object obj) => obj is WasmRuntime other && Equals(other);

        public bool Equals(WasmRuntime other) => base.Equals(other);
    }
}
