using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers.Reflection.Emit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using Perfolizer.Horology;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    /// <summary>
    /// A helper type that emits code that matches BenchmarkType.txt template.
    /// IMPORTANT: this type IS NOT thread safe.
    /// </summary>
    internal abstract partial class RunnableEmitter
    {
        private RunnableEmitter() { }

        /// <summary>
        /// Maps action args to fields that store arg values.
        /// </summary>
        private record struct ArgFieldInfo(FieldInfo Field, Type ArgLocalsType, MethodInfo OpImplicitMethod);

        /// <summary>
        /// Emits assembly with runnables from current build partition.
        /// </summary>
        public static Assembly EmitPartitionAssembly(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var assemblyResultPath = generateResult.ArtifactsPaths.ExecutablePath;
            var assemblyFileName = Path.GetFileName(assemblyResultPath);
            var config = buildPartition.Benchmarks.First().Config;

            var saveToDisk = ShouldSaveToDisk(config);
            var assemblyBuilder = DefineAssemblyBuilder(assemblyResultPath, saveToDisk);
            var moduleBuilder = DefineModuleBuilder(assemblyBuilder, assemblyFileName, saveToDisk);
            foreach (var benchmark in buildPartition.Benchmarks)
            {
                var returnType = benchmark.BenchmarkCase.Descriptor.WorkloadMethod.ReturnType;
                RunnableEmitter runnableEmitter = returnType.IsAwaitable()
                    ? new AsyncCoreEmitter()
                    : new SyncCoreEmitter();
                runnableEmitter.buildPartition = buildPartition ?? throw new ArgumentNullException(nameof(buildPartition));
                runnableEmitter.moduleBuilder = moduleBuilder ?? throw new ArgumentNullException(nameof(moduleBuilder));
                runnableEmitter.benchmark = benchmark;
                runnableEmitter.jobUnrollFactor = benchmark.BenchmarkCase.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, buildPartition.Resolver);
                runnableEmitter.EmitRunnableCore();
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
            if (!BenchmarkDotNetInfo.Instance.IsRelease)
            {
                // we never want to do that in our official NuGet.org package, it's a hack
                return config.Options.IsSet(ConfigOptions.KeepBenchmarkFiles) && Portability.RuntimeInformation.IsFullFramework;
            }

            return false;
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

        private TypeBuilder DefineRunnableTypeBuilder()
        {
            // .class public auto ansi sealed beforefieldinit BenchmarkDotNet.Autogenerated.Runnable_0
            //    extends [BenchmarkDotNet]BenchmarkDotNet.Samples.SampleBenchmark
            var workloadType = Descriptor.Type.GetTypeInfo();
            var workloadTypeAttributes = workloadType.Attributes;
            if (workloadTypeAttributes.HasFlag(TypeAttributes.NestedPublic))
            {
                workloadTypeAttributes =
                    (workloadTypeAttributes & ~TypeAttributes.NestedPublic)
                    | TypeAttributes.Public;
            }
            var result = moduleBuilder.DefineType(
                GetRunnableTypeName(benchmark),
                workloadTypeAttributes | TypeAttributes.Sealed,
                workloadType);

            return result;
        }

        private static void EmitNoArgsMethodCallPopReturn(MethodBuilder methodBuilder, MethodInfo targetMethod, ILGenerator ilBuilder)
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
                ilBuilder.EmitStaticCall(targetMethod, []);
            }
            else if (methodBuilder.IsStatic)
            {
                throw new InvalidOperationException(
                    $"[BUG] Static method {methodBuilder.Name} tries to call instance member {targetMethod.Name}");
            }
            else
            {
                ilBuilder.Emit(OpCodes.Ldarg_0);
                ilBuilder.EmitInstanceCallThisValueOnStack(null, targetMethod, [], true);
            }

            if (targetMethod.ReturnType != typeof(void))
                ilBuilder.Emit(OpCodes.Pop);
        }

        private BuildPartition buildPartition;
        private ModuleBuilder moduleBuilder;
        private BenchmarkBuildInfo benchmark;
        private int jobUnrollFactor;

        private TypeBuilder runnableBuilder;
        private readonly List<TypeBuilder> nestedTypeBuilders = [];

        private FieldBuilder fieldsContainerField;
        private readonly List<ArgFieldInfo> argFields = [];
        private FieldBuilder notElevenField;

        private MethodBuilder overheadImplementationMethod;

        private Descriptor Descriptor => benchmark.BenchmarkCase.Descriptor;
        private Type BenchmarkReturnType => Descriptor.WorkloadMethod.ReturnType;

        private void EmitRunnableCore()
        {
            runnableBuilder = DefineRunnableTypeBuilder();

            EmitFields();
            EmitCtor();
            EmitSetupCleanupMethods();
            EmitTrickTheJit();
            overheadImplementationMethod = EmitOverheadImplementation(OverheadImplementationMethodName);
            EmitCoreImpl();

            foreach (var nestedTypeBuilder in nestedTypeBuilders)
            {
                nestedTypeBuilder.CreateTypeInfo();
            }
            runnableBuilder.CreateTypeInfo();
        }

        protected abstract void EmitCoreImpl();

        private void EmitFields()
        {
            /*
                private unsafe struct FieldsContainer
                {
                }
            */
            var fieldsContainerBuilder = runnableBuilder.DefineNestedType(
                "FieldsContainer",
                TypeAttributes.NestedPrivate | TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                typeof(ValueType));
            nestedTypeBuilders.Add(fieldsContainerBuilder);

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
                else if (parameterType.IsByRefLike() && argValue.Value != null)
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

                if (argFieldType.IsByRefLike())
                    throw new NotSupportedException(
                        $"Passing ref readonly structs by ref is not supported (cannot store {argFieldType} as a class field).");

                var argField = fieldsContainerBuilder.DefineField(
                    ArgFieldPrefix + parameter.Position,
                    argFieldType,
                    FieldAttributes.Public);

                argFields.Add(new(argField, argLocalsType, opConversion));
            }

            EmitExtraFields(fieldsContainerBuilder);

            // private FieldsContainer __fieldsContainer;
            fieldsContainerField = runnableBuilder.DefineField(
                FieldsContainerName,
                fieldsContainerBuilder,
                FieldAttributes.Private);

            notElevenField = runnableBuilder.DefineField(NotElevenFieldName, typeof(int), FieldAttributes.Public);
        }

        protected virtual void EmitExtraFields(TypeBuilder fieldsContainerBuilder) { }

        private void EmitCtor()
        {
            // .method public hidebysig specialname rtspecialname
            //    instance void.ctor() cil managed
            var ctorMethod = runnableBuilder.DefinePublicInstanceCtor();
            var ilBuilder = ctorMethod.GetILGenerator();
            ilBuilder.EmitCallBaseParameterlessCtor(ctorMethod);
            ilBuilder.EmitCtorReturn(ctorMethod);
        }

        protected void EmitLoadArgFieldsForCall(ILGenerator ilBuilder, LocalBuilder? runnableLocal)
        {
            /*
                // base.InvokeOnceVoid(__fieldsContainer.__argField0, __fieldsContainer.__argField1);
	            IL_000b: ldarg.0
	            IL_000c: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_1::__fieldsContainer
	            IL_0011: ldfld bool BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer::__argField0
	            IL_0016: ldarg.0
	            IL_0017: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_1::__fieldsContainer
	            IL_001c: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer::__argField1

                // -or-

                // base.InvokeOnceVoid(ref __fieldsContainer.__argField0, ref __fieldsContainer.__argField1);
	            IL_000b: ldarg.0
	            IL_000c: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_2/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_2::__fieldsContainer
	            IL_0011: ldflda bool BenchmarkDotNet.Autogenerated.Runnable_2/FieldsContainer::__argField0
	            IL_0016: ldarg.0
	            IL_0017: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_2/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_2::__fieldsContainer
	            IL_001c: ldflda int32 BenchmarkDotNet.Autogenerated.Runnable_2/FieldsContainer::__argField1

                // -or- (ref struct arg call)

                // base.InvokeOnceVoid((Span<bool>)__fieldsContainer.__argField0, __fieldsContainer.__argField1);
	            IL_000b: ldarg.0
	            IL_000c: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_0::__fieldsContainer
	            IL_0011: ldfld bool[] BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer::__argField0
	            IL_0016: call valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<bool>::op_Implicit(!0[])
	            IL_001b: ldarg.0
	            IL_001c: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_0::__fieldsContainer
	            IL_0021: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_0/FieldsContainer::__argField1
             */
            foreach (var argFieldInfo in argFields)
            {
                if (runnableLocal is not null)
                    ilBuilder.EmitLdloc(runnableLocal);
                else
                    ilBuilder.Emit(OpCodes.Ldarg_0);

                ilBuilder.Emit(OpCodes.Ldflda, fieldsContainerField);

                if (argFieldInfo.ArgLocalsType.IsByRef)
                    ilBuilder.Emit(OpCodes.Ldflda, argFieldInfo.Field);
                else
                    ilBuilder.Emit(OpCodes.Ldfld, argFieldInfo.Field);

                if (argFieldInfo.OpImplicitMethod != null)
                    ilBuilder.Emit(OpCodes.Call, argFieldInfo.OpImplicitMethod);
            }
        }

        private void EmitSetupCleanupMethods()
        {
            EmitSetupCleanup(GlobalSetupMethodName, Descriptor.GlobalSetupMethod, false);
            EmitSetupCleanup(GlobalCleanupMethodName, Descriptor.GlobalCleanupMethod, true);
            EmitSetupCleanup(IterationSetupMethodName, Descriptor.IterationSetupMethod, false);
            EmitSetupCleanup(IterationCleanupMethodName, Descriptor.IterationCleanupMethod, false);
        }

        private void EmitTrickTheJit()
        {
            var forDisassemblyDiagnoserMethod = EmitForDisassemblyDiagnoserMethod();

            // .method public hidebysig
            //    instance void __TrickTheJIT__() cil managed noinlining nooptimization
            var trickTheJitMethod = runnableBuilder
                .DefinePublicNonVirtualVoidInstanceMethod(TrickTheJitCoreMethodName)
                .SetNoInliningImplementationFlag()
                .SetNoOptimizationImplementationFlag();


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
            EmitNoArgsMethodCallPopReturn(trickTheJitMethod, forDisassemblyDiagnoserMethod, ilBuilder);

            // IL_001b: ret
            ilBuilder.EmitVoidReturn(trickTheJitMethod);
        }

        private MethodBuilder EmitForDisassemblyDiagnoserMethod()
        {
            // .method public hidebysig
            //    instance void __ForDisassemblyDiagnoser__() cil managed noinlining nooptimization
            var workloadMethod = Descriptor.WorkloadMethod;
            var workloadReturnParameter = EmitParameterInfo.CreateReturnParameter(typeof(void));
            var methodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    ForDisassemblyDiagnoserMethodName,
                    MethodAttributes.Public,
                    workloadReturnParameter
                )
                .SetNoInliningImplementationFlag()
                .SetNoOptimizationImplementationFlag();

            var ilBuilder = methodBuilder.GetILGenerator();

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
            ilBuilder.Emit(OpCodes.Ldc_I4_S, (byte) 11);
            ilBuilder.Emit(OpCodes.Bne_Un, notElevenLabel);
            {
                /*
                    // base.Simple(__fieldsContainer.__argField0, __fieldsContainer.__argField1);
	                IL_000a: ldarg.0
	                IL_000b: ldarg.0
	                IL_000c: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_1::__fieldsContainer
	                IL_0011: ldfld bool BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer::__argField0
	                IL_0016: ldarg.0
	                IL_0017: ldflda valuetype BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer BenchmarkDotNet.Autogenerated.Runnable_1::__fieldsContainer
	                IL_001c: ldfld int32 BenchmarkDotNet.Autogenerated.Runnable_1/FieldsContainer::__argField1
	                IL_0021: call instance void [BenchmarkDotNet.IntegrationTests]BenchmarkDotNet.IntegrationTests.ArgumentsTests/WithArguments::Simple(bool, int32)
                */
                if (!workloadMethod.IsStatic)
                {
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                }
                EmitLoadArgFieldsForCall(ilBuilder, null);
                ilBuilder.Emit(OpCodes.Call, workloadMethod);
                if (BenchmarkReturnType != typeof(void))
                {
                    ilBuilder.Emit(OpCodes.Pop);
                }
            }
            ilBuilder.MarkLabel(notElevenLabel);
            /*
                IL_0018: ret
            */
            ilBuilder.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private MethodBuilder EmitOverheadImplementation(string methodName)
        {
            /*
                .method private hidebysig 
	                instance void __Overhead (int64 arg0) cil managed noinlining flags(0200) 
             */
            // Replace arg names
            var parameters = Descriptor.WorkloadMethod.GetParameters()
                .Select(p =>
                    (ParameterInfo) new EmitParameterInfo(
                        p.Position,
                        ArgParamPrefix + p.Position,
                        p.ParameterType,
                        p.Attributes,
                        null))
                .ToArray();
            var methodBuilder = runnableBuilder
                .DefineNonVirtualInstanceMethod(
                    methodName,
                    MethodAttributes.Private,
                    EmitParameterInfo.CreateReturnVoidParameter(),
                    parameters
                )
                .SetNoInliningImplementationFlag()
                .SetAggressiveOptimizationImplementationFlag();

            var ilBuilder = methodBuilder.GetILGenerator();
            /*
                // return;
                IL_0001: ret
             */
            ilBuilder.EmitVoidReturn(methodBuilder);

            return methodBuilder;
        }

        private MethodInfo GetStartClockMethod()
            => typeof(ClockExtensions).GetMethod(
                nameof(ClockExtensions.Start),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(IClock)],
                null
            );
    }
}