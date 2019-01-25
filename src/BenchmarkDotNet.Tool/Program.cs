using BenchmarkDotNet.Running;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Tool
{

    public sealed class Program
    {
        public static int Main(string[] args)
        {
            RemainingArguments = args;
            var parser = new CommandLineBuilder(Root())
                // parser
                .AddVersionOption()

                // middleware
                .UseHelp()
                .UseParseDirective()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .CancelOnProcessTermination()

                .Build();

            return parser.InvokeAsync(args).Result;
        }

        internal static RootCommand Root()
        {
            var root = new RootCommand(argument: new Argument<FileInfo>()
                { Arity = ArgumentArity.ExactlyOne, Description = "The assembly with the benchmarks (required).", Name = "assemblyFile" });
            root.Description = "A dotnet tool to execute benchmarks built with BenchmarkDotNet.";

            ConsoleArguments.CommandLineParser.AddAllOptions(root);

            root.Handler = new MethodBinder(typeof(Program).GetType().GetMethod(nameof(RootHandler)));

            return root;
        }

        public static string[] RemainingArguments { get; set; }

        public static int RootHandler(FileInfo assemblyFile)
        {
            Console.WriteLine(assemblyFile.FullName);

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(assemblyFile.FullName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Couldn't load the assembly {assemblyFile}.");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }

            BenchmarkSwitcher benchmarkSwitcher = BenchmarkSwitcher.FromAssembly(assembly);
            benchmarkSwitcher.Run(RemainingArguments);
            return 0;
        }

//        private static string GetVersion()
//        {
//            return typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
//        }
//
//        private static string GenerateExtendedHelpText()
//        {
//            StringBuilder sb = new StringBuilder()
//                .AppendLine()
//                .AppendLine("The first argument in [arguments] is the benchmark assembly and every following argument is passed to the BenchmarkSwitcher.")
//                .AppendLine("BenchmarkSwitcher arguments:")
//                .AppendLine();
//            using (StringWriter sw = new StringWriter(sb))
//            {
//                using (Parser p = new Parser((ps) => { ps.HelpWriter = sw; }))
//                {
//                    p.ParseArguments<CommandLineOptions>(new string[] { "--help" });
//                }
//            }
//            return sb.ToString();
//        }
    }
}
