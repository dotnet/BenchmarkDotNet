using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class WasmRuntime : Runtime, IEquatable<WasmRuntime>
    {
        public static readonly WasmRuntime Default = new WasmRuntime("Wasm");

        public WasmRuntime(string name) : base(RuntimeMoniker.Wasm, "wasm", name)
        {
        }

        public override bool Equals(object obj) => obj is WasmRuntime other && Equals(other);

        public bool Equals(WasmRuntime other) => base.Equals(other) && Name == other?.Name;

        public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode();
    }
}
