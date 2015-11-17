using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet
{
    // TODO handle command line args in a better way, see https://github.com/PerfDotNet/BenchmarkDotNet/issues/27
    internal class CommandLineArgs
    {
        static CommandLineArgs()
        {
            RawArgs = Environment.GetCommandLineArgs();
        }

        private static readonly string[] RawArgs;

        public static string[] GetCommandLineArgs()
        {
            return RawArgs;
        }

        public static bool PrintAssembly => RawArgs.Any(arg => IsMatch(arg, "-printAssembly", "-printAsm"));

        public static bool PrintIL => RawArgs.Any(arg => IsMatch(arg, "-printIL"));

        public static bool PrintDiagnostics => RawArgs.Any(arg => IsMatch(arg, "-printDiagnostic", "-printDiagnostics", "-printDiag"));

        private static bool IsMatch(string arg, params string [] possibleMatches)
        {
            return possibleMatches.Any(possible => possible.Equals(arg, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
