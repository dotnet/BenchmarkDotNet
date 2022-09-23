namespace BenchmarkDotNet.Diagnosers
{
    public enum DisassemblySyntax
    {
        /// <summary>
        /// Indicates a disassembler should use MASM syntax for generated assembly code
        /// </summary>
        Masm,
        /// <summary>
        /// Indicates a disassembler should use Intel syntax for generated assembly code.
        /// </summary>
        Intel,
        /// <summary>
        /// Indicates a disassembler should use AT&amp;T syntax for generated assembly code.
        /// </summary>
        Att
    }
}