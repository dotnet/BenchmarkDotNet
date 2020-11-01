using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    /// <summary>
    /// A helper type that emits code that matches BenchmarkType.txt template.
    /// IMPORTANT: this type IS NOT thread safe.
    /// </summary>
    internal class RunnableEmitter
    {
        /// <summary>
        /// Maps action args to fields that store arg values.
        /// </summary>
        private class ArgFieldInfo
        {
            public ArgFieldInfo(FieldInfo field, Type argLocalsType, MethodInfo opImplicitMethod)
            {
                Field = field;
                ArgLocalsType = argLocalsType;
                OpImplicitMethod = opImplicitMethod;
            }

            public FieldInfo Field { get; }

            public Type ArgLocalsType { get; }

            public MethodInfo OpImplicitMethod { get; }
        }

        /// <summary>
        /// Emits assembly with runnables from current build partition..
        /// </summary>
        public static Assembly EmitPartitionAssembly(
            GenerateResult generateResult,
            BuildPartition buildPartition,
            ILogger logger)
        {
            var assemblyResultPath = generateResult.ArtifactsPaths.ExecutablePath;
            var assemblyFileName = Path.GetFileName(assemblyResultPath);
            var config = buildPartition.Benchmarks.First().Config;

            var saveToDisk = ShouldSaveToDisk(config);
            var assemblyBuilder = DefineAssemblyBuilder(assemblyResultPath, saveToDisk);
            var moduleBuilder = DefineModuleBuilder(assemblyBuilder, assemblyFileName, saveToDisk);
            foreach (var benchmark in buildPartition.Benchmarks)
            {
                var runnableEmitter = new RunnableEmitter(buildPartition, moduleBuilder);
                runnableEmitter.EmitRunnableCore(benchmark);
            }

            if (saveToDisk)
            {
                assemblyBuilder.Save(assemblyFileName);
                logger.WriteLineInfo($"{assemblyFileName} assembly saved to {assemblyResultPath}");
            }

            return assemblyBuilder;
        }


        private static bool ShouldSaveToDisk(IConfig config)
        {
#if PRERELEASE_DEVELOP || PRERELEASE_NIGHTLY // we never want to do that in our official NuGet.org package, it's a hack
            return config.Options.IsSet(ConfigOptions.KeepBenchmarkFiles) && Portability.RuntimeInformation.IsFullFramework;
#else
            return false;
#endif
        }

        private static string GetRunnableTypeName(BenchmarkBuildInfo benchmark)
        {
            return EmittedTypePrefix + benchmark.Id;
        }

        private static AssemblyBuilder DefineAssemblyBuilder(string assemblyResultPath, bool saveToDisk)
        {
            var assemblyName = new AssemblyName { Name = Path.GetFileNameWithoutExtension(assemblyResultPath) };

            var assemblyMode = saveToDisk
                ? (AssemblyBuilderAccess)3 // https://apisof.net/catalog/System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave
                : AssemblyBuilderAccess.RunAndCollect;

            var assemblyBuilder = saveToDisk
                ? AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, assemblyMode, Path.GetDirectoryName(assemblyResultPath))
                : AssemblyBuilder.DefineDynamicAssembly(assemblyName, assemblyMode);

            DefineAssemblyAttributes(assemblyBuilder);

            return assemblyBuilder;
        }

        private static void DefineAssemblyAttributes(AssemblyBuilder assemblyBuilder)
        {
            // [assembly: CompilationRelaxations(8)]
            var attributeCtor = typeof(CompilationRelaxationsAttribute)
                .GetConstructor(new[] { typeof(int) })
                ?? throw new MissingMemberException(nameof(CompilationRelaxationsAttribute));

            var attBuilder = new CustomAttributeBuilder(
                attributeCtor,
                new object[] { (int)CompilationRelaxations.NoStringInterning });
            assemblyBuilder.SetCustomAttribute(attBuilder);

            // [assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
            attributeCtor = typeof(RuntimeCompatibilityAttribute)
                .GetConstructor(Array.Empty<Type>())
                ?? throw new MissingMemberException(nameof(RuntimeCompatibilityAttribute));

            var attributeProp = typeof(RuntimeCompatibilityAttribute)
                .GetProperty(nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows));
            attBuilder = new CustomAttributeBuilder(
                attributeCtor,
                Array.Empty<object>(),
                new[] { attributeProp },
                new object[] { true });
            assemblyBuilder.SetCustomAttribute(attBuilder);

            // [assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
            attributeCtor = typeof(DebuggableAttribute)
                .GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) })
                ?? throw new MissingMemberException(nameof(DebuggableAttribute));
            attBuilder = new CustomAttributeBuilder(
                attributeCtor,
                new object[] { DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints });
            assemblyBuilder.SetCustomAttribute(attBuilder);
        }

        private static ModuleBuilder DefineModuleBuilder(AssemblyBuilder assemblyBuilder, string moduleFileName, bool saveToDisk)
        {
            var moduleName = Path.GetFileNameWithoutExtension(moduleFileName)
               ?? throw new ArgumentNullException(nameof(moduleFileName));

            var moduleBuilder = saveToDisk
                ? assemblyBuilder.DefineDynamicModule(moduleName, moduleFileName)
                : assemblyBuilder.DefineDynamicModule(moduleName);

            // [module:UnverifiableCodeAttribute()]
            var attributeCtor = typeof(UnverifiableCodeAttribute)
                .GetConstructor(Array.Empty<Type>())
                ?? throw new MissingMemberException(nameof(UnverifiableCodeAttribute));
            var attBuilder = new CustomAttributeBuilder(
                attributeCtor,
                Array.Empty<object>());
            moduleBuilder.SetCustomAttribute(attBuilder);

            return moduleBuilder;
        }

        private static TypeBuilder DefineRunnableTypeBuilder(
            BenchmarkBuildInfo benchmark,
            ModuleBuilder moduleBuilder)
        {
            // .class public auto ansi beforefieldinit BenchmarkDotNet.Autogenerated.Runnable_0
            //    extends [BenchmarkDotNet]BenchmarkDotNet.Samples.SampleBenchmark
            var benchmarkDescriptor = benchmark.BenchmarkCase.Descriptor;

            var workloadType = benchmarkDescriptor.Type.GetTypeInfo();
            var workloadTypeAttributes = workloadType.Attributes;
            if (workloadTypeAttributes.HasFlag(TypeAttributes.NestedPublic))
            {
                workloadTypeAttributes =
                    (workloadTypeAttributes & ~TypeAttributes.NestedPublic)
                    | TypeAttributes.Public;
            }
            var result = moduleBuilder.DefineType(
                GetRunnableTypeName(benchmark),
                workloadTypeAttributes,
                workloadType);

            return result;
        }

        private static void EmitNoArgsMethodCallPopReturn(
            MethodBuilder methodBuilder,
            MethodInfo targetMethod,
            ILGenerator ilBuilder,
            bool forceDirectCall)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            /*
                // call for instance void
                // GlobalSetup();
                IL_0000: ldarg.0
                IL_0001: call instance void [BenchmarkDotNet]BenchmarkDotNet.Samples.SampleBenchmark::GlobalSetup()
            */
            /*
                // call for static with return value
                // GlobalSetup();
                IL_0000: call string [BenchmarkDotNet]BenchmarkDotNet.Samples.SampleBenchmark::GlobalCleanup()
                IL_0005: pop
            */
            if (targetMethod.IsStatic)
            {
                ilBuilder.EmitStaticCall(targetMethod, Array.Empty<LocalBuilder>());
            }
            else if (methodBuilder.IsStatic)
            {
                throw new InvalidOperationException(
                    $"[BUG] Static method {methodBuilder.Name} tries to call instance member {targetMethod.Name}");
            }
            else
            {
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.EmitInstanceCallThisValueOnStack(
                    null,
                    targetMethod,
                    Array.Empty<LocalBuilder>(),
                    forceDirectCall);
            }

            if (targetMethod.ReturnType != typeof(void))
                ilBuilder.Emit(OpCodes.Pop);
        }

        private readonly BuildPartition buildPartition;
        private readonly ModuleBuilder moduleBuilder;

        private BenchmarkBuildInfo benchmark;
        private List<ArgFieldInfo> argFields;
        private int jobUnrollFactor;
        private int dummyUnrollFactor;

        private Type overheadDelegateType;
        private Type workloadDelegateType;
        private TypeBuilder runnableBuilder;
        private ConsumableTypeInfo consumableInfo;
        private ConsumeEmitter consumeEmitter;

        private FieldBuilder globalSetupActionField;
        private FieldBuilder globalCleanupActionField;
        private FieldBuilder iterationSetupActionField;
        private FieldBuilder iterationCleanupActionField;
        private FieldBuilder overheadDelegateField;
        private FieldBuilder workloadDelegateField;
        private FieldBuilder notElevenField;
        private FieldBuilder dummyVarField;

        // ReSharper disable NotAccessedField.Local
        private ConstructorBuilder ctorMethod;
        private MethodBuilder trickTheJitMethod;
        private MethodBuilder dummy1Method;
        private MethodBuilder dummy2Method;
        private MethodBuilder dummy3Method;
        private MethodInfo workloadImplementationMethod;
        private MethodBuilder overheadImplementationMethod;
        private MethodBuilder overheadActionUnrollMethod;
        private MethodBuilder overheadActionNoUnrollMethod;
        private MethodBuilder workloadActionUnrollMethod;
        private MethodBuilder workloadActionNoUnrollMethod;
        private MethodBuilder forDisassemblyDiagnoserMethod;

        private MethodBuilder globalSetupMethod;
        private MethodBuilder globalCleanupMethod;
        private MethodBuilder iterationSetupMethod;
        private MethodBuilder iterationCleanupMethod;

        private MethodBuilder runMethod;
        // ReSharper restore NotAccessedField.Local

        private RunnableEmitter([NotNull] BuildPartition buildPartition, [NotNull] ModuleBuilder moduleBuilder)
        {
            if (buildPartition == null)
                throw new ArgumentNullException(nameof(buildPartition));
            if (moduleBuilder == null)
                throw new ArgumentNullException(nameof(moduleBuilder));

            this.buildPartition = buildPartition;
            this.moduleBuilder = moduleBuilder;
        }

        [NotNull]
        private Descriptor Descriptor => benchmark.BenchmarkCase.Descriptor;

        // ReSharper disable once UnusedMethodReturnValue.Local
        private Type EmitRunnableCore(BenchmarkBuildInfo newBenchmark)
        {
            if (newBenchmark == null)
                throw new ArgumentNullException(nameof(newBenchmark));

            InitForEmitRunnable(newBenchmark);

            // 1. Emit fields
            DefineFields();

            // 2. Define members
            ctorMethod = DefineCtor();
            trickTheJitMethod = DefineTrickTheJitMethod();

            // Dummy
            dummy1Method = EmitDummyMethod(Dummy1MethodName, dummyUnrollFactor);
            dummy2Method = EmitDummyMethod(Dummy2MethodName, dummyUnrollFactor);
            dummy3Method = EmitDummyMethod(Dummy3MethodName, dummyUnrollFactor);

            // 3. Emit impl
            consumeEmitter.OnEmitMembers(runnableBuilder);

            // Overhead impl
            overheadImplementationMethod = EmitOverheadImplementation(OverheadImplementationMethodName);
            overheadActionUnrollMethod = EmitOverheadAction(OverheadActionUnrollMethodName, jobUnrollFactor);
            overheadActionNoUnrollMethod = EmitOverheadAction(OverheadActionNoUnrollMethodName, 1);

            // Workload impl
            workloadImplementationMethod = EmitWorkloadImplementation(WorkloadImplementationMethodName);
            workloadActionUnrollMethod = EmitWorkloadAction(WorkloadActionUnrollMethodName, jobUnrollFactor);
            workloadActionNoUnrollMethod = EmitWorkloadAction(WorkloadActionNoUnrollMethodName, 1);

            // __ForDisassemblyDiagnoser__ impl
            forDisassemblyDiagnoserMethod = EmitForDisassemblyDiagnoser(ForDisassemblyDiagnoserMethodName);

            // 4. Instance completion
            // Emit wrappers for setup/cleanup callbacks
            EmitSetupCleanupMethods();

            // Emit methods that depend on others
            EmitTrickTheJitBody();
            EmitCtorBody();

            // 5. Emit Run() logic
            runMethod = EmitRunMethod();

#if NETFRAMEWORK
            return runnableBuilder.CreateType();
#else
            return runnableBuilder.CreateTypeInfo();
#endif
        }

        private void InitForEmitRunnable(BenchmarkBuildInfo newBenchmark)
        {
            // Init current state
            argFields = new List<ArgFieldInfo>();
            benchmark = newBenchmark;
            jobUnrollFactor = benchmark.BenchmarkCase.Job.ResolveValue(
                RunMode.UnrollFactorCharacteristic,
                buildPartition.Resolver);
            dummyUnrollFactor = DummyUnrollFactor;

            consumableInfo = new ConsumableTypeInfo(benchmark.BenchmarkCase.Descriptor.WorkloadMethod.ReturnType);
            consumeEmitter = ConsumeEmitter.GetConsumeEmitter(consumableInfo);

            // Init types
            runnableBuilder = DefineRunnableTypeBuilder(benchmark, moduleBuilder);
            overheadDelegateType = EmitOverheadDelegateType();
            workloadDelegateType = EmitWorkloadDelegateType();
        }

        private Type EmitOverheadDelegateType()
        {
            // .class public auto ansi sealed BenchmarkDotNet.Autogenerated.Runnable_0OverheadDelegate
            //    extends[mscorlib]System.MulticastDelegate;
            var overheadReturnType = EmitParameterInfo.CreateReturnParameter(consumableInfo.OverheadMethodReturnType);

            // replace arg names
            var overheadParameters = Descriptor.WorkloadMethod.GetParameters()
                .Select(p =>
                    (ParameterInfo)new EmitParameterInfo(
                        p.Position,
                        ArgParamPrefix + p.Position,
                        p.ParameterType,
                        p.Attributes,
                        null))
                .ToArray();

            return moduleBuilder.EmitCustomDelegate(
                GetRunnableTypeName(benchmark) + OverheadDelegateTypeSuffix,
                overheadReturnType,
                overheadParameters);
        }

        private Type EmitWorkloadDelegateType()
        {
            // .class public auto ansi sealed BenchmarkDotNet.Autogenerated.Runnable_0WorkloadDelegate
            //    extends [mscorlib]System.MulticastDelegate
            var workloadReturnType = EmitParameterInfo.CreateReturnParameter(consumableInfo.WorkloadMethodReturnType);

            // Replace arg names
            var workloadParameters = Descriptor.WorkloadMethod.GetParameters()
                .Select(p =>
                    (ParameterInfo)new EmitParameterInfo(
                        p.Position,
                        ArgParamPrefix + p.Position,
                        p.ParameterType,
                        p.Attributes,
                        null))
                .ToArray();

            return moduleBuilder.EmitCustomDelegate(
                GetRunnableTypeName(benchmark) + WorkloadDelegateTypeSuffix,
                workloadReturnType,
                workloadParameters);
        }

        private void DefineFields()
        {
            globalSetupActionField =
                runnableBuilder.DefineField(GlobalSetupActionFieldName, typeof(Action), FieldAttributes.Private);
            globalCleanupActionField =
                runnableBuilder.DefineField(GlobalCleanupActionFieldName, typeof(Action), FieldAttributes.Private);
            iterationSetupActionField =
                runnableBuilder.DefineField(IterationSetupActionFieldName, typeof(Action), FieldAttributes.Private);
            iterationCleanupActionField =
                runnableBuilder.DefineField(IterationCleanupActionFieldName, typeof(Action), FieldAttributes.Private);
            overheadDelegateField =
                runnableBuilder.DefineField(OverheadDelegateFieldName, overheadDelegateType, FieldAttributes.Private);
            workloadDelegateField =
                runnableBuilder.DefineField(WorkloadDelegateFieldName, workloadDelegateType, FieldAttributes.Private);

            // Define arg fields
            foreach (var parameter in Descriptor.WorkloadMethod.GetParameters())
            {
                var argValue = benchmark.BenchmarkCase.Parameters.GetArgument(parameter.Name);
                var parameterType = parameter.ParameterType;

                Type argLocalsType;
                Type argFieldType;
                MethodInfo opConversion = null;
                if (parameterType.IsByRef)
                {
                    argLocalsType = parameterType;
                    argFieldType = argLocalsType.GetElementType()
                        ?? throw new InvalidOperationException($"Bug: cannot get field type from {argLocalsType}");
                }
                else if (IsRefLikeType(parameterType) && argValue.Value != null)
                {
                    argLocalsType = parameterType;

                    // Use conversion on load; store passed value
                    var passedArgType = argValue.Value.GetType();
                    opConversion = GetImplicitConversionOpFromTo(passedArgType, argLocalsType) ??
                        throw new InvalidOperationException($"Bug: No conversion from {passedArgType} to {argLocalsType}.");
                    argFieldType = passedArgType;
                }
                else
                {
                    // No conversion; load ref to arg field;
                    argLocalsType = parameterType;
                    argFieldType = parameterType;
                }

                if (IsRefLikeType(argFieldType))
                    throw new NotSupportedException(
                        $"Passing ref readonly structs by ref is not supported (cannot store {argFieldType} as a class field).");

                var argField = runnableBuilder.DefineField(
                    ArgFieldPrefix + parameter.Position,
                    argFieldType,
                    FieldAttributes.Private);

                argFields.Add(new ArgFieldInfo(argField, argLocalsType, opConversion));
            }

            notElevenField = runnableBuilder.DefineField(NotElevenFieldName, typeof(int), FieldAttributes.Public);
            dummyVarField = runnableBuilder.DefineField(DummyVarFieldName, typeof(int), FieldAttributes.Private);
            consumeEmitter.OnDefineFields(runnableBuilder);
        }

        private ConstructorBuilder DefineCtor()
        {
            // .method public hidebysig specialname rtspecialname
            //    instance void.ctor() cil managed
            return runnableBuilder.DefinePublicInstanceCtor();
        }

        private MethodBuilder DefineTrickTheJitMethod()
        {
            // .method public hidebysig
            //    instance void __TrickTheJIT__() cil managed noinlining nooptimization
            var result = runnableBuilder
                .DefinePublicNonVirtualVoidInstanceMethod(TrickTheJitCoreMethodName)
                .SetNoInliningImplementationFlag()
                .SetNoOptimizationImplementationFlag();

            return result;
        }

        private MethodBuilder EmitDummyMethod(string methodName, int unrollFactor)
        {
            // .method private hidebysig
            //    instance void Dummy1() cil managed noinlining
            var methodBuilder = runnableBuilder
                .DefinePrivateVoidInstanceMethod(methodName)
                .SetNoInliningImplementationFlag();

            /*
                // dummyVar++;
                IL_0000: ldarg.0
                IL_0001: ldarg.0
                IL_0002: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::dummyVar
                IL_0007: ldc.i4.1
                IL_0008: add
                IL_0009: stfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::dummyVar
                // dummyVar++;
                IL_000e: ldarg.0
                IL_000f: ldarg.0
                IL_0010: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::dummyVar
                IL_0015: ldc.i4.1
                IL_0016: add
                IL_0017: stfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::dummyVar
                ...
                IL_0380: ret
             */

            var ilBuilder = methodBuilder.GetILGenerator();

            for (int i = 0; i < unrollFactor; i++)
            {
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.Emit(OpCodes.Ldfld, dummyVarField);
                ilBuilder.Emit(OpCodes.Ldc_I4_1);
                ilBuilder.Emit(OpCodes.Add);
                ilBuilder.Emit(OpCodes.Stfld, dummyVarField);
            }

            ilBuilder.EmitVoidReturn(methodBuilder);

            return methodBuilder;
        }

        private MethodBuilder EmitOverheadImplementation(string methodName)
        {
            var overheadInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(overheadDelegateType);

            //.method private hidebysig
            //    instance int32 __Overhead(int64 arg0) cil managed
            var methodBuilder = runnableBuilder.DefineNonVirtualInstanceMethod(
                methodName,
                MethodAttributes.Private,
                overheadInvokeMethod.ReturnParameter,
                overheadInvokeMethod.GetParameters());

            var ilBuilder = methodBuilder.GetILGenerator();
            var returnType = methodBuilder.ReturnType;

            /*
                // return default;
                IL_0000: ldc.i4.0
                IL_0001: ret
             */
            // optional local if default(T) uses .initobj
            var optionalLocalForInitobj = ilBuilder.DeclareOptionalLocalForReturnDefault(returnType);
            ilBuilder.EmitReturnDefault(returnType, optionalLocalForInitobj);

            return methodBuilder;
        }

        private MethodInfo EmitWorkloadImplementation(string methodName)
        {
            // Shortcut: DO NOT emit method if the result type is not awaitable
            if (!consumableInfo.IsAwaitable)
                return Descriptor.WorkloadMethod;

            var workloadInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(workloadDelegateType);

            //.method private hidebysig
            //   instance int32 __Workload(int64 arg0) cil managed
            var args = workloadInvokeMethod.GetParameters();
            var methodBuilder = runnableBuilder.DefineNonVirtualInstanceMethod(
                methodName,
                MethodAttributes.Private,
                workloadInvokeMethod.ReturnParameter,
                args);
            args = methodBuilder.GetEmitParameters(args);
            var callResultType = consumableInfo.OriginMethodReturnType;
            var awaiterType = consumableInfo.GetAwaiterMethod?.ReturnType
                ?? throw new InvalidOperationException($"Bug: {nameof(consumableInfo.GetAwaiterMethod)} is null");

            var ilBuilder = methodBuilder.GetILGenerator();

            /*
                .locals init (
                    [0] valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<int32>
                )
             */
            var callResultLocal =
                ilBuilder.DeclareOptionalLocalForInstanceCall(callResultType, consumableInfo.GetAwaiterMethod);
            var awaiterLocal =
                ilBuilder.DeclareOptionalLocalForInstanceCall(awaiterType, consumableInfo.GetResultMethod);

            /*
                // return TaskSample(arg0). ... ;
                IL_0000: ldarg.0
                IL_0001: ldarg.1
                IL_0002: call instance class [mscorlib]System.Threading.Tasks.Task`1<int32> [BenchmarkDotNet]BenchmarkDotNet.Samples.SampleBenchmark::TaskSample(int64)
             */
            if (!Descriptor.WorkloadMethod.IsStatic)
                ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.EmitLdargs(args);
            ilBuilder.Emit(OpCodes.Call, Descriptor.WorkloadMethod);

            /*
                // ... .GetAwaiter().GetResult();
                IL_0007: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [mscorlib]System.Threading.Tasks.Task`1<int32>::GetAwaiter()
                IL_000c: stloc.0
                IL_000d: ldloca.s 0
                IL_000f: call instance !0 valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<int32>::GetResult()
             */
            ilBuilder.EmitInstanceCallThisValueOnStack(callResultLocal, consumableInfo.GetAwaiterMethod);
            ilBuilder.EmitInstanceCallThisValueOnStack(awaiterLocal, consumableInfo.GetResultMethod);

            /*
                IL_0014: ret
             */
            ilBuilder.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private MethodBuilder EmitOverheadAction(string methodName, int unrollFactor)
        {
            return EmitActionImpl(methodName, RunnableActionKind.Overhead, unrollFactor);
        }

        private MethodBuilder EmitWorkloadAction(string methodName, int unrollFactor)
        {
            return EmitActionImpl(methodName, RunnableActionKind.Workload, unrollFactor);
        }

        private MethodBuilder EmitActionImpl(string methodName, RunnableActionKind actionKind, int unrollFactor)
        {
            FieldInfo actionDelegateField;
            MethodInfo actionInvokeMethod;
            switch (actionKind)
            {
                case RunnableActionKind.Overhead:
                    actionDelegateField = overheadDelegateField;
                    actionInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(overheadDelegateType);
                    break;
                case RunnableActionKind.Workload:
                    actionDelegateField = workloadDelegateField;
                    actionInvokeMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(workloadDelegateType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actionKind), actionKind, null);
            }

            // .method private hidebysig
            //    instance void OverheadActionUnroll(int64 invokeCount) cil managed
            var toArg = new EmitParameterInfo(0, InvokeCountParamName, typeof(long));
            var actionMethodBuilder = runnableBuilder.DefineNonVirtualInstanceMethod(
                methodName,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnVoidParameter(),
                toArg);
            toArg.SetMember(actionMethodBuilder);

            // Emit impl
            var ilBuilder = actionMethodBuilder.GetILGenerator();
            consumeEmitter.BeginEmitAction(actionMethodBuilder, ilBuilder, actionInvokeMethod, actionKind);

            // init locals
            var argLocals = EmitDeclareArgLocals(ilBuilder);
            consumeEmitter.DeclareActionLocals(ilBuilder);
            var indexLocal = ilBuilder.DeclareLocal(typeof(long));

            // load fields
            EmitLoadArgFieldsToLocals(ilBuilder, argLocals);
            consumeEmitter.EmitActionBeforeLoop(ilBuilder);

            // loop
            var loopStartLabel = ilBuilder.DefineLabel();
            var loopHeadLabel = ilBuilder.DefineLabel();
            ilBuilder.EmitLoopBeginFromLocToArg(loopStartLabel, loopHeadLabel, indexLocal, toArg);
            {
                /*
                    // overheadDelegate();
                    IL_0005: ldarg.0
                    IL_0006: ldfld class BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate BenchmarkDotNet.Autogenerated.Runnable_0::overheadDelegate
                    IL_000b: callvirt instance void BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate::Invoke()
                    // -or-
                    // consumer.Consume(overheadDelegate(_argField));
                    IL_000c: ldarg.0
                    IL_000d: ldfld class [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer BenchmarkDotNet.Autogenerated.Runnable_0::consumer
                    IL_0012: ldarg.0
                    IL_0013: ldfld class BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate BenchmarkDotNet.Autogenerated.Runnable_0::overheadDelegate
                    IL_0018: ldloc.0
                    IL_0019: callvirt instance int32 BenchmarkDotNet.Autogenerated.Runnable_0/OverheadDelegate::Invoke(int64)
                    IL_001e: callvirt instance void [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer::Consume(int32)
                 */
                for (int u = 0; u < unrollFactor; u++)
                {
                    consumeEmitter.EmitActionBeforeCall(ilBuilder);

                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    ilBuilder.Emit(OpCodes.Ldfld, actionDelegateField);
                    ilBuilder.EmitInstanceCallThisValueOnStack(null, actionInvokeMethod, argLocals);

                    consumeEmitter.EmitActionAfterCall(ilBuilder);
                }
            }
            ilBuilder.EmitLoopEndFromLocToArg(loopStartLabel, loopHeadLabel, indexLocal, toArg);

            consumeEmitter.EmitActionAfterLoop(ilBuilder);
            consumeEmitter.CompleteEmitAction(ilBuilder);

            // IL_003a: ret
            ilBuilder.EmitVoidReturn(actionMethodBuilder);

            return actionMethodBuilder;
        }

        private IReadOnlyList<LocalBuilder> EmitDeclareArgLocals(ILGenerator ilBuilder, bool skipFirst = false)
        {
            // NB: c# compiler does not store first arg in locals for static calls
            /*
                .locals init (
                    [0] int64, // argFields[0]
                    [1] int32, // argFields[1]
                )
                // -or- (static calls)
                .locals init (
                    [0] int32, // argFields[1]
                )
             */
            bool first = true;
            var argLocals = new List<LocalBuilder>(argFields.Count);
            foreach (var argField in argFields)
            {
                if (!first || !skipFirst)
                {
                    argLocals.Add(ilBuilder.DeclareLocal(argField.ArgLocalsType));
                }

                first = false;
            }

            return argLocals;
        }

        private void EmitLoadArgFieldsToLocals(ILGenerator ilBuilder, IReadOnlyList<LocalBuilder> argLocals, bool skipFirstArg = false)
        {
            // NB: c# compiler does not store first arg in locals for static calls
            int localsOffset = argFields.Count > 0 && skipFirstArg ? -1 : 0;

            if (argLocals.Count != argFields.Count + localsOffset)
                throw new InvalidOperationException("Bug: argLocals.Count != _argFields.Count + localsOffset");

            /*
                // long _argField = __argField0;
                IL_0000: ldarg.0
                IL_0001: ldfld int64 BenchmarkDotNet.Autogenerated.Runnable_0::__argField0
                IL_0006: stloc.0
                IL_0007: ldarg.1
                IL_0008: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::__argField1
                IL_000c: stloc.1
                // -or-
                // ref int _argField = ref __argField0;
                IL_0000: ldarg.0
                IL_0001: ldflda int64 BenchmarkDotNet.Autogenerated.Runnable_0::__argField0
                IL_0006: stloc.0
                IL_0007: ldarg.1
                IL_000b: ldflda int32 BenchmarkDotNet.Autogenerated.Runnable_0::__argField1
                IL_000c: stloc.1
                // -or- (static call)
                // long _argField = __argField0;
                IL_0000: ldarg.0
                IL_0001: ldfld int64 BenchmarkDotNet.Autogenerated.Runnable_0::__argField0
                IL_0006: ldarg.1
                IL_0007: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::__argField1
                IL_000b: stloc.0 // offset by -1
                // -or- (ref struct arg call)
                IL_0000: ldarg.0
                IL_0001: ldfld int32[] BenchmarkDotNet.Autogenerated.Runnable_0::__argField0
                IL_0006: call valuetype [System.Memory]System.Span`1<!0> valuetype [System.Memory]System.Span`1<int32>::op_Implicit(!0[])
                IL_000b: stloc.0
                IL_000c: ldarg.1
                IL_000d: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::__argField1
                IL_0012: stloc.1
             */
            for (int i = 0; i < argFields.Count; i++)
            {
                ilBuilder.Emit(OpCodes.Ldarg_0);
                var argFieldInfo = argFields[i];

                if (argFieldInfo.ArgLocalsType.IsByRef)
                    ilBuilder.Emit(OpCodes.Ldflda, argFieldInfo.Field);
                else
                    ilBuilder.Emit(OpCodes.Ldfld, argFieldInfo.Field);

                if (argFieldInfo.OpImplicitMethod != null)
                    ilBuilder.Emit(OpCodes.Call, argFieldInfo.OpImplicitMethod);

                var localsIndex = i + localsOffset;
                if (localsIndex >= 0)
                    ilBuilder.EmitStloc(argLocals[localsIndex]);
            }
        }

        private MethodBuilder EmitForDisassemblyDiagnoser(string methodName)
        {
            // .method public hidebysig
            //    instance int32 __ForDisassemblyDiagnoser__() cil managed noinlining nooptimization
            var workloadMethod = Descriptor.WorkloadMethod;
            var workloadReturnParameter = EmitParameterInfo.CreateReturnParameter(consumableInfo.WorkloadMethodReturnType);
            var methodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    methodName,
                    MethodAttributes.Public,
                    workloadReturnParameter)
                .SetNoInliningImplementationFlag()
                .SetNoOptimizationImplementationFlag();

            var ilBuilder = methodBuilder.GetILGenerator();

            /*
                .locals init (
                    [0] int64,
                )
             */
            // NB: c# compiler does not store first arg in locals for static calls
            var skipFirstArg = workloadMethod.IsStatic;
            var argLocals = EmitDeclareArgLocals(ilBuilder, skipFirstArg);

            LocalBuilder callResultLocal = null;
            LocalBuilder awaiterLocal = null;
            if (consumableInfo.IsAwaitable)
            {
                var callResultType = consumableInfo.OriginMethodReturnType;
                var awaiterType = consumableInfo.GetAwaiterMethod?.ReturnType
                    ?? throw new InvalidOperationException($"Bug: {nameof(consumableInfo.GetAwaiterMethod)} is null");
                callResultLocal =
                    ilBuilder.DeclareOptionalLocalForInstanceCall(callResultType, consumableInfo.GetAwaiterMethod);
                awaiterLocal =
                    ilBuilder.DeclareOptionalLocalForInstanceCall(awaiterType, consumableInfo.GetResultMethod);
            }

            consumeEmitter.DeclareDisassemblyDiagnoserLocals(ilBuilder);

            var notElevenLabel = ilBuilder.DefineLabel();
            /*
                // if (NotEleven == 11)
                IL_0000: ldarg.0
                IL_0001: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::NotEleven
                IL_0006: ldc.i4.s 11
                IL_0008: bne.un.s IL_0019  // we use long jump
             */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldfld, notElevenField);
            ilBuilder.Emit(OpCodes.Ldc_I4_S, (byte)11);
            ilBuilder.Emit(OpCodes.Bne_Un, notElevenLabel);
            {
                /*
                    // long _argField = __argField0;
                    IL_000a: ldarg.0
                    IL_000b: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::__argField0
                    IL_0010: stloc.0
                 */
                EmitLoadArgFieldsToLocals(ilBuilder, argLocals, skipFirstArg);

                /*
                    // return TaskSample(_argField) ... ;
                    IL_0011: ldarg.0
                    IL_0012: ldloc.0
                    IL_0013: call instance class [mscorlib]System.Threading.Tasks.Task`1<int32> [BenchmarkDotNet]BenchmarkDotNet.Samples.SampleBenchmark::TaskSample(int64)
                    IL_0018: ret
                */

                if (!workloadMethod.IsStatic)
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.EmitLdLocals(argLocals);
                ilBuilder.Emit(OpCodes.Call, workloadMethod);

                if (consumableInfo.IsAwaitable)
                {
                    /*
                        // ... .GetAwaiter().GetResult();
                        IL_0007: callvirt instance valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [mscorlib]System.Threading.Tasks.Task`1<int32>::GetAwaiter()
                        IL_000c: stloc.0
                        IL_000d: ldloca.s 0
                        IL_000f: call instance !0 valuetype [mscorlib]System.Runtime.CompilerServices.TaskAwaiter`1<int32>::GetResult()
                     */
                    ilBuilder.EmitInstanceCallThisValueOnStack(callResultLocal, consumableInfo.GetAwaiterMethod);
                    ilBuilder.EmitInstanceCallThisValueOnStack(awaiterLocal, consumableInfo.GetResultMethod);
                }

                /*
                    IL_0018: ret
                */
                if (consumableInfo.WorkloadMethodReturnType != typeof(void))
                {
                    ilBuilder.Emit(OpCodes.Ret);
                }

                // IL_0019: ret
                // -or-
                // return default;
                // IL_0019: ldc.i4.0
                // IL_001a: ret
            }
            ilBuilder.MarkLabel(notElevenLabel);
            consumeEmitter.EmitDisassemblyDiagnoserReturnDefault(ilBuilder);

            return methodBuilder;
        }

        private void EmitSetupCleanupMethods()
        {
            // Emit Setup/Cleanup methods
            // We emit empty method instead of EmptyAction = "() => { }"
            globalSetupMethod = EmitWrapperMethod(
                GlobalSetupMethodName,
                Descriptor.GlobalSetupMethod);
            globalCleanupMethod = EmitWrapperMethod(
                GlobalCleanupMethodName,
                Descriptor.GlobalCleanupMethod);
            iterationSetupMethod = EmitWrapperMethod(
                IterationSetupMethodName,
                Descriptor.IterationSetupMethod);
            iterationCleanupMethod = EmitWrapperMethod(
                IterationCleanupMethodName,
                Descriptor.IterationCleanupMethod);
        }

        private MethodBuilder EmitWrapperMethod(string methodName, MethodInfo optionalTargetMethod)
        {
            var methodBuilder = runnableBuilder.DefinePrivateVoidInstanceMethod(methodName);

            var ilBuilder = methodBuilder.GetILGenerator();

            if (optionalTargetMethod != null)
                EmitNoArgsMethodCallPopReturn(methodBuilder, optionalTargetMethod, ilBuilder, forceDirectCall: true);

            ilBuilder.EmitVoidReturn(methodBuilder);

            return methodBuilder;
        }

        private void EmitCtorBody()
        {
            var ilBuilder = ctorMethod.GetILGenerator();

            ilBuilder.EmitCallBaseParameterlessCtor(ctorMethod);

            consumeEmitter.OnEmitCtorBody(ctorMethod, ilBuilder);

            ilBuilder.EmitSetDelegateToThisField(globalSetupActionField, globalSetupMethod);
            ilBuilder.EmitSetDelegateToThisField(globalCleanupActionField, globalCleanupMethod);
            ilBuilder.EmitSetDelegateToThisField(iterationSetupActionField, iterationSetupMethod);
            ilBuilder.EmitSetDelegateToThisField(iterationCleanupActionField, iterationCleanupMethod);
            ilBuilder.EmitSetDelegateToThisField(overheadDelegateField, overheadImplementationMethod);

            if (workloadImplementationMethod == null)
                ilBuilder.EmitSetDelegateToThisField(workloadDelegateField, Descriptor.WorkloadMethod);
            else
                ilBuilder.EmitSetDelegateToThisField(workloadDelegateField, workloadImplementationMethod);

            ilBuilder.EmitCtorReturn(ctorMethod);
        }

        private void EmitTrickTheJitBody()
        {
            var ilBuilder = trickTheJitMethod.GetILGenerator();

            /*
                // NotEleven = new Random(123).Next(0, 10);
                IL_0000: ldarg.0
                IL_0001: ldc.i4.s 123
                IL_0003: newobj instance void [mscorlib]System.Random::.ctor(int32)
                IL_0008: ldc.i4.0
                IL_0009: ldc.i4.s 10
                IL_000b: callvirt instance int32 [mscorlib]System.Random::Next(int32, int32)
                IL_0010: stfld int32 BenchmarkDotNet.Autogenerated.Runnable_0::NotEleven
             */
            var randomCtor = typeof(Random).GetConstructor(new[] { typeof(int) })
                ?? throw new MissingMemberException(nameof(Random));
            var randomNextMethod = typeof(Random).GetMethod(nameof(Random.Next), new[] { typeof(int), typeof(int) })
                ?? throw new MissingMemberException(nameof(Random.Next));

            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldc_I4_S, (byte)123);
            ilBuilder.Emit(OpCodes.Newobj, randomCtor);
            ilBuilder.Emit(OpCodes.Ldc_I4_0);
            ilBuilder.Emit(OpCodes.Ldc_I4_S, (byte)10);
            ilBuilder.Emit(OpCodes.Callvirt, randomNextMethod);
            ilBuilder.Emit(OpCodes.Stfld, notElevenField);

            /*
                // __ForDisassemblyDiagnoser__();
                IL_0015: ldarg.0
                IL_0016: call instance int32 BenchmarkDotNet.Autogenerated.Runnable_0::__ForDisassemblyDiagnoser__()
                IL_001b: pop
            */
            EmitNoArgsMethodCallPopReturn(trickTheJitMethod, forDisassemblyDiagnoserMethod, ilBuilder, forceDirectCall: true);

            // IL_001b: ret
            ilBuilder.EmitVoidReturn(trickTheJitMethod);
        }

        private MethodBuilder EmitRunMethod()
        {
            var prepareForRunMethodTemplate = typeof(RunnableReuse).GetMethod(nameof(RunnableReuse.PrepareForRun))
                ?? throw new MissingMemberException(nameof(RunnableReuse.PrepareForRun));
            var resultTuple = new ValueTuple<Job, EngineParameters, IEngineFactory>();

            /*
                .method public hidebysig static
                    void Run (
                        class [BenchmarkDotNet]BenchmarkDotNet.Running.BenchmarkCase benchmarkCase,
                        class [BenchmarkDotNet]BenchmarkDotNet.Engines.IHost host
                    ) cil managed
             */
            var argsExceptInstance = prepareForRunMethodTemplate
                .GetParameters()
                .Skip(1)
                .Select(p => (ParameterInfo)new EmitParameterInfo(p.Position - 1, p.Name, p.ParameterType, p.Attributes, null))
                .ToArray();
            var methodBuilder = runnableBuilder.DefineStaticMethod(
                RunMethodName,
                MethodAttributes.Public,
                EmitParameterInfo.CreateReturnVoidParameter(),
                argsExceptInstance);
            argsExceptInstance = methodBuilder.GetEmitParameters(argsExceptInstance);
            var benchmarkCaseArg = argsExceptInstance[0];
            var hostArg = argsExceptInstance[1];

            var ilBuilder = methodBuilder.GetILGenerator();

            /*
                .locals init (
                    [0] class BenchmarkDotNet.Autogenerated.Runnable_0,
                    [1] class [BenchmarkDotNet]BenchmarkDotNet.Jobs.Job,
                    [2] class [BenchmarkDotNet]BenchmarkDotNet.Engines.EngineParameters,
                    [3] class [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngineFactory,
                    [4] class [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngine,
                    [5] valuetype [BenchmarkDotNet]BenchmarkDotNet.Engines.RunResults
                )
             */
            var instanceLocal = ilBuilder.DeclareLocal(runnableBuilder);
            var jobLocal = ilBuilder.DeclareLocal(typeof(Job));
            var engineParametersLocal = ilBuilder.DeclareLocal(typeof(EngineParameters));
            var engineFactoryLocal = ilBuilder.DeclareLocal(typeof(IEngineFactory));
            var engineLocal = ilBuilder.DeclareLocal(typeof(IEngine));
            var runResultsLocal = ilBuilder.DeclareLocal(typeof(RunResults));

            /*
                // Runnable_0 instance = new Runnable_0();
                IL_0000: newobj instance void BenchmarkDotNet.Autogenerated.Runnable_0::.ctor()
                IL_0005: stloc.0
             */
            ilBuilder.Emit(OpCodes.Newobj, ctorMethod);
            ilBuilder.EmitStloc(instanceLocal);

            /*
                // (Job, EngineParameters, IEngineFactory) valueTuple = RunnableReuse.PrepareForRun(instance, benchmarkCase, host);
                IL_0006: ldloc.0
                IL_0007: ldarg.0
                IL_0008: ldarg.1
                IL_0009: call valuetype [mscorlib]System.ValueTuple`3<class [BenchmarkDotNet]BenchmarkDotNet.Jobs.Job, class [BenchmarkDotNet]BenchmarkDotNet.Engines.EngineParameters, class [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngineFactory> [BenchmarkDotNet]BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReuse::PrepareForRun<class BenchmarkDotNet.Autogenerated.Runnable_0>(!!0, class [BenchmarkDotNet]BenchmarkDotNet.Running.BenchmarkCase, class [BenchmarkDotNet]BenchmarkDotNet.Engines.IHost)
             */
            ilBuilder.EmitLdloc(instanceLocal);
            ilBuilder.EmitLdarg(benchmarkCaseArg);
            ilBuilder.EmitLdarg(hostArg);
            ilBuilder.Emit(OpCodes.Call, prepareForRunMethodTemplate.MakeGenericMethod(runnableBuilder));

            /*
                // Job job = valueTuple.Item1;
                IL_000e: dup
                IL_000f: ldfld !0 valuetype [mscorlib]System.ValueTuple`3<class [BenchmarkDotNet]BenchmarkDotNet.Jobs.Job, class [BenchmarkDotNet]BenchmarkDotNet.Engines.EngineParameters, class [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngineFactory>::Item1
                IL_0014: stloc.1
             */
            ilBuilder.Emit(OpCodes.Dup);
            ilBuilder.Emit(OpCodes.Ldfld, resultTuple.GetType().GetField(nameof(resultTuple.Item1)));
            ilBuilder.EmitStloc(jobLocal);
            /*
                // EngineParameters engineParameters = valueTuple.Item2;
                IL_0015: dup
                IL_0016: ldfld !1 valuetype [mscorlib]System.ValueTuple`3<class [BenchmarkDotNet]BenchmarkDotNet.Jobs.Job, class [BenchmarkDotNet]BenchmarkDotNet.Engines.EngineParameters, class [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngineFactory>::Item2
                IL_001b: stloc.2
             */
            ilBuilder.Emit(OpCodes.Dup);
            ilBuilder.Emit(OpCodes.Ldfld, resultTuple.GetType().GetField(nameof(resultTuple.Item2)));
            ilBuilder.EmitStloc(engineParametersLocal);
            /*
                // IEngineFactory engineFactory = valueTuple.Item3;
                IL_001c: ldfld !2 valuetype [mscorlib]System.ValueTuple`3<class [BenchmarkDotNet]BenchmarkDotNet.Jobs.Job, class [BenchmarkDotNet]BenchmarkDotNet.Engines.EngineParameters, class [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngineFactory>::Item3
                IL_0021: stloc.3
             */
            ilBuilder.Emit(OpCodes.Ldfld, resultTuple.GetType().GetField(nameof(resultTuple.Item3)));
            ilBuilder.EmitStloc(engineFactoryLocal);

            var notNullLabel = ilBuilder.DefineLabel();
            /*
                // if (job != null) { ... } // translates to "if null: return; else: ..."
                IL_0022: ldloc.1
                IL_0023: brtrue.s IL_0026
                IL_0025: ret
             */
            ilBuilder.EmitLdloc(jobLocal);
            ilBuilder.Emit(OpCodes.Brtrue_S, notNullLabel);
            ilBuilder.EmitVoidReturn(methodBuilder);

            /*
                // using (IEngine engine = engineFactory.CreateReadyToRun(engineParameters))
                IL_0026: ldloc.3
                IL_0027: ldloc.2
                IL_0028: callvirt instance class [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngine [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngineFactory::CreateReadyToRun(class [BenchmarkDotNet]BenchmarkDotNet.Engines.EngineParameters)
                IL_002d: stloc.s 4
             */
            var createReadyToRunMethod = typeof(IEngineFactory).GetMethod(nameof(IEngineFactory.CreateReadyToRun))
                ?? throw new MissingMemberException(nameof(IEngineFactory.CreateReadyToRun));
            ilBuilder.MarkLabel(notNullLabel);
            ilBuilder.EmitLdloc(engineFactoryLocal);
            ilBuilder.EmitLdloc(engineParametersLocal);
            ilBuilder.Emit(OpCodes.Callvirt, createReadyToRunMethod);
            ilBuilder.EmitStloc(engineLocal);

            // .try
            // {
            ilBuilder.BeginExceptionBlock();
            {
                /*
                    // RunResults results = engine.Run();
                    IL_002f: ldloc.s 4
                    IL_0031: callvirt instance valuetype [BenchmarkDotNet]BenchmarkDotNet.Engines.RunResults [BenchmarkDotNet]BenchmarkDotNet.Engines.IEngine::Run()
                    IL_0036: stloc.s 5
                 */
                var runMethodImpl = typeof(IEngine).GetMethod(nameof(IEngine.Run))
                    ?? throw new MissingMemberException(nameof(IEngine.Run));
                ilBuilder.EmitLdloc(engineLocal);
                ilBuilder.Emit(OpCodes.Callvirt, runMethodImpl);
                ilBuilder.EmitStloc(runResultsLocal);
                /*
                    // host.ReportResults(results);
                    IL_0038: ldarg.1
                    IL_0039: ldloc.s 5
                    IL_003b: callvirt instance void [BenchmarkDotNet]BenchmarkDotNet.Engines.IHost::ReportResults(valuetype [BenchmarkDotNet]BenchmarkDotNet.Engines.RunResults)
                 */
                var reportResultsMethod = typeof(IHost).GetMethod(nameof(IHost.ReportResults))
                    ?? throw new MissingMemberException(nameof(IHost.ReportResults));
                ilBuilder.EmitLdarg(hostArg);
                ilBuilder.EmitLdloc(runResultsLocal);
                ilBuilder.Emit(OpCodes.Callvirt, reportResultsMethod);
                /*
                    // instance.__TrickTheJIT__();
                    IL_0040: ldloc.0
                    IL_0041: callvirt instance void BenchmarkDotNet.Autogenerated.ReplaceMe.Runnable0::__TrickTheJIT__()
                 */
                ilBuilder.Emit(OpCodes.Ldloc_0);
                ilBuilder.Emit(OpCodes.Callvirt, trickTheJitMethod);
            }
            // finally
            // {
            ilBuilder.BeginFinallyBlock();
            {
                /*
                    IL_0048: ldloc.s 4
                    IL_004a: brfalse.s IL_0053
                    IL_004c: ldloc.s 4
                    IL_004e: callvirt instance void [mscorlib]System.IDisposable::Dispose()
                 */
                var disposeMethod = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))
                    ?? throw new MissingMemberException(nameof(IDisposable.Dispose));
                var disposeNullLabel = ilBuilder.DefineLabel();
                ilBuilder.EmitLdloc(engineLocal);
                ilBuilder.Emit(OpCodes.Brfalse_S, disposeNullLabel);
                ilBuilder.EmitLdloc(engineLocal);
                ilBuilder.Emit(OpCodes.Callvirt, disposeMethod);

                ilBuilder.MarkLabel(disposeNullLabel);
                ilBuilder.EndExceptionBlock();
            }

            ilBuilder.EmitVoidReturn(methodBuilder);

            return methodBuilder;
        }
    }
}