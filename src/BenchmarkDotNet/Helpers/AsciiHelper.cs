namespace BenchmarkDotNet.Helpers
{
    internal static class AsciiHelper
    {
        /// <summary>
        /// The 'μ' symbol
        /// </summary>
        internal const string Mu = "\u03BC";

        public static string ToAscii(this string s)
        {
            // We should replace all non-ASCII symbols that used in BenchmarkDotNet by ASCII-compatible analogues
            return s.Replace(Mu, "u");
        }
    }
}