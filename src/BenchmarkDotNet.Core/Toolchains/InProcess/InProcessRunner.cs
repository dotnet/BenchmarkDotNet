using System;
using System.Reflection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    internal class InProcessRunner
    {
        public static int Run(IHost host, Benchmark benchmark, BenchmarkActionCodegen codegenMode)
        {
            bool isDiagnoserAttached = host.IsDiagnoserAttached;

            // the first thing to do is to let diagnosers hook in before anything happens
            // so all jit-related diagnosers can catch first jit compilation!
            if (isDiagnoserAttached)
                host.BeforeAnythingElse();

            try
            {
                // we are not using Runnable here in any direct way in order to avoid strong dependency Main<=>Runnable
                // which could cause the jitting/assembly loading to happen before we do anything
                // we have some jitting diagnosers and we want them to catch all the informations!!

                var inProcessRunnableTypeName = $"{typeof(InProcessRunner).FullName}+{nameof(Runnable)}";
                var type = typeof(InProcessRunner).GetTypeInfo().Assembly.GetType(inProcessRunnableTypeName);
                if (type == null)
                    throw new InvalidOperationException($"Bug: type {inProcessRunnableTypeName} not found.");

                type.GetMethod(nameof(Runnable.RunCore), BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { host, benchmark, codegenMode });

                return 0;
            }
            catch (Exception ex)
            {
                host.WriteLine(ex.ToString());
                return -1;
            }
        }

        [UsedImplicitly]
        private static class Runnable
        {
            public static void RunCore(IHost host, Benchmark benchmark, BenchmarkActionCodegen codegenMode)
            {
                var target = benchmark.Target;
                var job = benchmark.Job; // TODO: filter job (same as SourceCodePresenter does)?
                var unrollFactor = benchmark.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvResolver.Instance);

                // DONTTOUCH: these should be allocated together
                var instance = Activator.CreateInstance(benchmark.Target.Type);
                var mainAction = BenchmarkActionFactory.CreateRun(target, instance, codegenMode, unrollFactor);
                var idleAction = BenchmarkActionFactory.CreateIdle(target, instance, codegenMode, unrollFactor);
                var globalSetupAction = BenchmarkActionFactory.CreateGlobalSetup(target, instance);
                var globalCleanupAction = BenchmarkActionFactory.CreateGlobalCleanup(target, instance);
                var iterationSetupAction = BenchmarkActionFactory.CreateIterationSetup(target, instance);
                var iterationCleanupAction = BenchmarkActionFactory.CreateIterationCleanup(target, instance);
                var dummy1 = BenchmarkActionFactory.CreateDummy();
                var dummy2 = BenchmarkActionFactory.CreateDummy();
                var dummy3 = BenchmarkActionFactory.CreateDummy();

                FillMembers(instance, benchmark);

                host.WriteLine();
                foreach (var infoLine in BenchmarkEnvironmentInfo.GetCurrent().ToFormattedString())
                    host.WriteLine("// {0}", infoLine);
                host.WriteLine("// Job: {0}", job.DisplayInfo);
                host.WriteLine();

                var engineParameters = new EngineParameters
                {
                    Host = host,
                    MainAction = mainAction.InvokeMultiple,
                    Dummy1Action = dummy1.InvokeSingle,
                    Dummy2Action = dummy2.InvokeSingle,
                    Dummy3Action = dummy3.InvokeSingle,
                    IdleAction = idleAction.InvokeMultiple,
                    GlobalSetupAction = globalSetupAction.InvokeSingle,
                    GlobalCleanupAction = globalCleanupAction.InvokeSingle,
                    IterationSetupAction = iterationSetupAction.InvokeSingle,
                    IterationCleanupAction = iterationCleanupAction.InvokeSingle,
                    TargetJob = job,
                    OperationsPerInvoke = target.OperationsPerInvoke
                };

                var engine = job
                    .ResolveValue(InfrastructureMode.EngineFactoryCharacteristic, InfrastructureResolver.Instance)
                    .Create(engineParameters);

                engine.PreAllocate();

                globalSetupAction.InvokeSingle();
                iterationSetupAction.InvokeSingle();

                if (job.ResolveValue(RunMode.RunStrategyCharacteristic, EngineResolver.Instance).NeedsJitting())
                    engine.Jitting(); // does first call to main action, must be executed after setup()!

                iterationCleanupAction.InvokeSingle();

                if (host.IsDiagnoserAttached)
                    host.AfterGlobalSetup();

                var results = engine.Run();

                if (host.IsDiagnoserAttached)
                    host.BeforeGlobalCleanup();
                globalCleanupAction.InvokeSingle();

                host.ReportResults(results); // printing costs memory, do this after runs
            }

            /// <summary>Fills the properties of the instance of the object used to run the benchmark.</summary>
            /// <param name="instance">The instance.</param>
            /// <param name="benchmark">The benchmark.</param>
            private static void FillMembers(object instance, Benchmark benchmark)
            {
                foreach (var parameter in benchmark.Parameters.Items)
                {
                    var flags = BindingFlags.Public;
                    flags |= parameter.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

                    var targetType = benchmark.Target.Type;
                    var paramProperty = targetType.GetProperty(parameter.Name, flags);

                    if (paramProperty == null)
                    {
                        var paramField = targetType.GetField(parameter.Name, flags);
                        if (paramField == null)
                            throw new InvalidOperationException(
                                $"Type {targetType.FullName}: no property or field {parameter.Name} found.");

                        var callInstance = paramField.IsStatic ? null : instance;
                        paramField.SetValue(callInstance, parameter.Value);
                    }
                    else
                    {
                        var setter = paramProperty.GetSetMethod();
                        if (setter == null)
                            throw new InvalidOperationException(
                                $"Type {targetType.FullName}: no settable property {parameter.Name} found.");

                        var callInstance = setter.IsStatic ? null : instance;
                        setter.Invoke(callInstance, new[] { parameter.Value });
                    }
                }
            }
        }
    }
}