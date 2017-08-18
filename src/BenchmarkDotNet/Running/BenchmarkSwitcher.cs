using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Properties;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkSwitcher
    {
        private readonly ConsoleLogger logger = new ConsoleLogger();
        private readonly TypeParser typeParser;

        public BenchmarkSwitcher(Type[] types)
        {
            foreach (var type in types.Where(type => !type.ContainsRunnableBenchmarks()).ToArray())
            {
                logger.WriteLineError($"Type {type} is invalid. Only public, non-generic, non-abstract, non-sealed types with public instance [Benchmark] method(s) are supported");
            }

            typeParser = new TypeParser(types.Where(type => type.ContainsRunnableBenchmarks()).ToArray(), logger);
        }

        public BenchmarkSwitcher(Assembly assembly)
        {
            typeParser = new TypeParser(assembly.GetRunnableBenchmarks(), logger);
        }

        public static BenchmarkSwitcher FromTypes(Type[] types) => new BenchmarkSwitcher(types);

        public static BenchmarkSwitcher FromAssembly(Assembly assembly) => new BenchmarkSwitcher(assembly);

        /// <summary>
        /// Run all available benchmarks.
        /// </summary>
        public IEnumerable<Summary> RunAll() => Run(new[] { "*" });
        
        /// <summary>
        /// Run all available benchmarks and join them to a single summary
        /// </summary>
        public Summary RunAllJoined() => Run(new[] { "*", "--join" }).Single();

        public IEnumerable<Summary> Run(string[] args = null, IConfig config = null)
        {
            args = typeParser.ReadArgumentList(args ?? Array.Empty<string>());
            return RunBenchmarks(args, config);
        }

        private IEnumerable<Summary> RunBenchmarks(string[] args, IConfig config)
        {
            var globalChronometer = Chronometer.Start();
            var summaries = new List<Summary>();

            if (ShouldDisplayOptions(args))
            {
                DisplayOptions();
                return Enumerable.Empty<Summary>();
            }
            
            var effectiveConfig = ManualConfig.Union(config ?? DefaultConfig.Instance, ManualConfig.Parse(args));
            bool join = args.Any(arg => arg.EqualsWithIgnoreCase("--join"));

            if (join)
            {
                var typesWithMethods = typeParser.MatchingTypesWithMethods(args);
                var benchmarks = typesWithMethods.SelectMany(typeWithMethods => 
                    typeWithMethods.AllMethodsInType 
                        ? BenchmarkConverter.TypeToBenchmarks(typeWithMethods.Type, effectiveConfig) 
                        : BenchmarkConverter.MethodsToBenchmarks(typeWithMethods.Type, typeWithMethods.Methods, effectiveConfig)).ToArray();
                summaries.Add(BenchmarkRunner.Run(benchmarks, effectiveConfig));
            }
            else
            {
                foreach (var typeWithMethods in typeParser.MatchingTypesWithMethods(args))
                {
                    logger.WriteLineHeader("Target type: " + typeWithMethods.Type.Name);
                    if (typeWithMethods.AllMethodsInType)
                        summaries.Add(BenchmarkRunner.Run(typeWithMethods.Type, effectiveConfig));
                    else
                        summaries.Add(BenchmarkRunner.Run(typeWithMethods.Type, typeWithMethods.Methods, effectiveConfig));
                    logger.WriteLine();
                }
            }

            // TODO: move this logic to the RunUrl method
#if CLASSIC
            if (args.Length > 0 && (args[0].StartsWith("http://") || args[0].StartsWith("https://")))
            {
                var url = args[0];
                Uri uri = new Uri(url);
                var name = uri.IsFile ? Path.GetFileName(uri.LocalPath) : "URL";
                summaries.Add(BenchmarkRunner.RunUrl(url, effectiveConfig));
            }
#endif

            var clockSpan = globalChronometer.Stop();
            BenchmarkRunnerCore.LogTotalTime(logger, clockSpan.GetTimeSpan(), "Global total time");
            return summaries;
        }

        public bool ShouldDisplayOptions(string[] args) 
            => args.Select(a => a.ToLowerInvariant()).Any(a => a == "--help" || a == "-h");

        private void DisplayOptions()
        {
            logger.WriteLineHeader($"{BenchmarkDotNetInfo.FullTitle}");
            logger.WriteLine();
            logger.WriteLineHeader("Options:");

            var consoleWidth = 80;
            try
            {
                consoleWidth = Console.WindowWidth;
            }
            // IOException causes build error
            // The type 'IOException' exists in both 
            //    'System.IO, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' and 
            //    'System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
            // TODO see if this error goes away after RC2, in the meantime just use "catch (Exception)"
            catch (Exception)
            {
                logger.WriteLine($"Unable to get the Console width, defaulting to {consoleWidth}");
            }

            ManualConfig.PrintOptions(logger, prefixWidth: 30, outputWidth: consoleWidth);
            logger.WriteLine();
            typeParser.PrintOptions(prefixWidth: 30, outputWidth: consoleWidth);
        }
    }
}