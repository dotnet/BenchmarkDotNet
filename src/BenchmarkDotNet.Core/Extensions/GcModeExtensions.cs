using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Extensions
{
    public static class GcModeExtensions
    {
        /// <summary>
        /// Returns original gcMode for gcMode != null. Returns GcMode.Host for gcMode == null.
        /// </summary>
        public static GcMode Resolve(this GcMode gcMode)
        {
            return gcMode == null ? GcMode.Host : gcMode;
        }

        /// <summary>
        /// Returns gcMode.ToString() for gcMode != null. Returns "Host" for gcMode == null.
        /// </summary>
        /// <param name="gcMode"></param>
        /// <returns></returns>
        public static string ToStr(this GcMode gcMode)
        {
            return gcMode == null ? "Host" : gcMode.ToString();
        }
    }
}