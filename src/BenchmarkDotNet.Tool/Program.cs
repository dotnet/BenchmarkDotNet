using BenchmarkDotNet.Running;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
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
            
            var nonNullConfig = DefaultConfig.Instance;

            // if user did not provide any loggers, we use the ConsoleLogger to somehow show the errors to the user
            var nonNullLogger = nonNullConfig.GetLoggers().Any() ? nonNullConfig.GetCompositeLogger() : ConsoleLogger.Default;

            OptionHandler optionHandler = new OptionHandler();
            var result = CommandLineParser.Parser(
                optionHandler,
                args,
                nonNullLogger,
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

            return Run(optionHandler.AssemblyFile, optionHandler.Options, nonNullConfig, nonNullLogger);
        }

        public static int Run(FileInfo assemblyFile, CommandLineOptions optionHandlerOptions, IConfig config, ILogger logger)
        {
            if (assemblyFile == null)
            {
                return -1;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => CurrentDomainOnAssemblyResolve(assemblyFile, args);
         
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
            benchmarkSwitcher.Run(optionHandlerOptions, config, logger);
            return 0;
        }

        private static Assembly CurrentDomainOnAssemblyResolve(FileInfo assemblyFile, ResolveEventArgs args)
        {
            var fullName = new AssemblyName(args.Name);
            string simpleName = fullName.Name;

            var directory = Path.GetDirectoryName(assemblyFile.FullName);

            string guessedPath = Path.Combine(directory, $"{simpleName}.dll");
            Console.WriteLine(guessedPath);
            if (!File.Exists(guessedPath))
                return null; // we can't help, and we also don't call Assembly.Load which if fails comes back here, creates endless loop and causes StackOverflow

            // we warn the user about that, in case some Super User want to be aware of that
            Console.WriteLine($".NET Framework was unable to load {args.Name}, but we are going to load it from {guessedPath}");

            return Assembly.LoadFrom(guessedPath);
        }
    }
}
