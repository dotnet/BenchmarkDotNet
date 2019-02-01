using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Running;
using CommandLine;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Text;

namespace BenchmarkDotNet.Tool
{
    [Command(
        Name = "BenchmarkDotNet",
        Description = "A dotnet tool to execute benchmarks built with BenchmarkDotNet.")]
    [HelpOption()]
    [VersionOptionFromMember(MemberName = nameof(GetVersion))]
    public sealed class Program
    {
        public static int Main(string[] args)
        {
            using (CommandLineApplication<Program> app = new CommandLineApplication<Program>())
            {
                app.Conventions.UseDefaultConventions();
                app.ThrowOnUnexpectedArgument = false;
                app.ExtendedHelpText = GenerateExtendedHelpText();
                return app.Execute(args);
            }
        }

        private IConsole Console => PhysicalConsole.Singleton;

        [Argument(0, Description = "The assembly with the benchmarks (required).")]
        [Required]
        [FileExists]
        public string AssemblyFile { get; set; }

        public string[] RemainingArguments { get; set; }

        public int OnExecute()
        {
            try
            {
                using (var dynamicContext = new AssemblyResolver(Path.GetFullPath(AssemblyFile)))
                {

                    BenchmarkSwitcher benchmarkSwitcher = BenchmarkSwitcher.FromAssembly(dynamicContext.Assembly);
                    benchmarkSwitcher.Run(RemainingArguments);
                }
            }
            catch (FileLoadException ex)
            {
                Console.Error.WriteLine($"Couldn't load the assembly {AssemblyFile}.");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            catch (BadImageFormatException ex)
            {
                Console.Error.WriteLine($"The assembly {AssemblyFile} is not a valid assembly.");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }

            return 0;
        }

        private static string GetVersion()
        {
            return typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        private static string GenerateExtendedHelpText()
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine()
                .AppendLine("The first argument in [arguments] is the benchmark assembly and every following argument is passed to the BenchmarkSwitcher.")
                .AppendLine("BenchmarkSwitcher arguments:")
                .AppendLine();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (Parser p = new Parser((ps) => { ps.HelpWriter = sw; }))
                {
                    p.ParseArguments<CommandLineOptions>(new string[] { "--help" });
                }
            }
            return sb.ToString();
        }
    }
}