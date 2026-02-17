using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Validators;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit;

internal static class InProcessEmitRunner
{
    public static async ValueTask<int> Run(IHost host, ExecuteParameters parameters)
    {
        // the first thing to do is to let diagnosers hook in before anything happens
        // so all jit-related diagnosers can catch first jit compilation!
        host.BeforeAnythingElse();

        try
        {
            var runnableType = ((InProcessEmitArtifactsPath) parameters.BuildResult.ArtifactsPaths)
                .GeneratedAssembly
                .GetType(EmittedTypePrefix + parameters.BenchmarkId)!;

            await RunCore(runnableType, host, parameters);

            return 0;
        }
        catch (Exception oom) when (oom is OutOfMemoryException || oom is TargetInvocationException reflection && reflection.InnerException is OutOfMemoryException)
        {
            host.WriteLine();
            host.WriteLine("OutOfMemoryException!");
            host.WriteLine("BenchmarkDotNet continues to run additional iterations until desired accuracy level is achieved. It's possible only if the benchmark method doesn't have any side-effects.");
            host.WriteLine("If your benchmark allocates memory and keeps it alive, you are creating a memory leak.");
            host.WriteLine("You should redesign your benchmark and remove the side-effects. You can use `OperationsPerInvoke`, `IterationSetup` and `IterationCleanup` to do that.");
            host.WriteLine();
            host.WriteLine(oom.ToString());

            return -1;
        }
        catch (Exception ex)
        {
            host.WriteLine();
            host.WriteLine(ex.ToString());
            return -1;
        }
        finally
        {
            host.AfterAll();
        }
    }

    private static async ValueTask RunCore(Type runnableType, IHost host, ExecuteParameters parameters)
    {
        var benchmarkCase = parameters.BenchmarkCase;

        var instance = Activator.CreateInstance(runnableType)!;
        FillMembers(instance, benchmarkCase);

        host.WriteLine();
        foreach (string infoLine in BenchmarkEnvironmentInfo.GetCurrent().ToFormattedString())
        {
            host.WriteLine($"// {infoLine}");
        }
        var job = new Job().Apply(benchmarkCase.Job).Freeze();
        host.WriteLine($"// Job: {job.DisplayInfo}");
        host.WriteLine();

        var errors = BenchmarkProcessValidator.Validate(job, instance);
        if (await ValidationErrorReporter.ReportIfAnyAsync(errors, host))
            return;

        var compositeInProcessDiagnoserHandler = new Diagnosers.CompositeInProcessDiagnoserHandler(
            parameters.CompositeInProcessDiagnoser.InProcessDiagnosers
                .Select((d, i) => Diagnosers.InProcessDiagnoserRouter.Create(d, benchmarkCase, i))
                .Where(r => r.handler != null)
                .ToArray(),
            host,
            parameters.DiagnoserRunMode,
            new Diagnosers.InProcessDiagnoserActionArgs(instance)
        );
        if (parameters.DiagnoserRunMode == Diagnosers.RunMode.SeparateLogic)
        {
            await compositeInProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.SeparateLogic);
            return;
        }
        await compositeInProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.BeforeEngine);

        var engineParameters = new EngineParameters()
        {
            Host = host,
            WorkloadActionUnroll = LoopCallbackFromMethod(instance, WorkloadActionUnrollMethodName),
            WorkloadActionNoUnroll = LoopCallbackFromMethod(instance, WorkloadActionNoUnrollMethodName),
            OverheadActionNoUnroll = LoopCallbackFromMethod(instance, OverheadActionNoUnrollMethodName),
            OverheadActionUnroll = LoopCallbackFromMethod(instance, OverheadActionUnrollMethodName),
            GlobalSetupAction = SetupOrCleanupCallbackFromMethod(instance, GlobalSetupMethodName),
            GlobalCleanupAction = SetupOrCleanupCallbackFromMethod(instance, GlobalCleanupMethodName),
            IterationSetupAction = SetupOrCleanupCallbackFromMethod(instance, IterationSetupMethodName),
            IterationCleanupAction = SetupOrCleanupCallbackFromMethod(instance, IterationCleanupMethodName),
            TargetJob = benchmarkCase.Job,
            OperationsPerInvoke = benchmarkCase.Descriptor.OperationsPerInvoke,
            RunExtraIteration = benchmarkCase.Config.HasExtraIterationDiagnoser(benchmarkCase),
            BenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase),
            InProcessDiagnoserHandler = compositeInProcessDiagnoserHandler
        };

        var results = await job
            .ResolveValue(InfrastructureMode.EngineFactoryCharacteristic, InfrastructureResolver.Instance)!
            .Create(engineParameters)
            .RunAsync();
        host.ReportResults(results);

        runnableType.GetMethod(TrickTheJitCoreMethodName)!.Invoke(instance, []);

        await compositeInProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.AfterEngine);
    }

    private static void FillMembers(object instance, BenchmarkCase benchmarkCase)
    {
        var argIndex = 0;
        foreach (var argInfo in benchmarkCase.Descriptor.WorkloadMethod.GetParameters())
        {
            SetArgumentField(instance, benchmarkCase, argInfo, argIndex);
            argIndex++;
        }

        foreach (var paramInfo in benchmarkCase.Parameters.Items)
        {
            if (!paramInfo.IsArgument)
            {
                SetParameter(instance, paramInfo);
            }
        }
    }
}