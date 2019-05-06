using BenchmarkDotNet.Running;
using System;
using System.CommandLine;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;

namespace BenchmarkDotNet.Tool
{
    public sealed class Program
    {
        public static int Main(string[] args)
        {
            var config = DefaultConfig.Instance;

            var logger = config.GetNonNullCompositeLogger();

            OptionHandler optionHandler = new OptionHandler();
            var result = CommandLineParser.Parser(
                optionHandler,
                args,
                logger,
                $"A dotnet tool to execute benchmarks built with {BenchmarkDotNetInfo.FullTitle}.",
                new Argument<FileInfo>()
                {
                    Arity = ArgumentArity.ExactlyOne,
                    Description = "The assembly with the benchmarks (required).",
                    Name = "assemblyFile"
                });

            if (result != 0)
            {
                return result;
            }

            return Run(optionHandler.AssemblyFile, optionHandler.Options, config, logger);
        }

        public static int Run(FileInfo assemblyFile, CommandLineOptions optionHandlerOptions, IConfig config, ILogger logger)
        {
            if (assemblyFile == null)
            {
                return -1;
            }

            try
            {
                using (var dynamicContext = new AssemblyResolver(assemblyFile.FullName))
                {
                    BenchmarkSwitcher benchmarkSwitcher = BenchmarkSwitcher.FromAssembly(dynamicContext.Assembly);
                    benchmarkSwitcher.Run(optionHandlerOptions, config, logger);
                }
            }
            catch (FileLoadException ex)
            {
                Console.Error.WriteLine($"Couldn't load the assembly {assemblyFile}.");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            catch (BadImageFormatException ex)
            {
                Console.Error.WriteLine($"The assembly {assemblyFile} is not a valid assembly.");
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
           
            return 0;
        }
    }
}
