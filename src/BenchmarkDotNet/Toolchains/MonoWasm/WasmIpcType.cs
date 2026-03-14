namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    /// <summary>
    /// Specifies the IPC mechanism to use for WASM benchmarks.
    /// </summary>
    public enum WasmIpcType
    {
        /// <summary>
        /// Automatically detect based on JavaScript engine (default).
        /// WebSocket for Node.js/Bun/Browsers, file+stdout for shell engines (d8, js, jsc, etc.).
        /// </summary>
        Auto,

        /// <summary>
        /// Use WebSocket IPC. Requires JavaScript engine with WebSocket support.
        /// </summary>
        WebSocket,

        /// <summary>
        /// Use file+stdout IPC. Works for shell engines.
        /// </summary>
        FileStdOut
    }
}
