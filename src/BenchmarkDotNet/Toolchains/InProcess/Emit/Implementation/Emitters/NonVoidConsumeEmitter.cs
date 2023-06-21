using System.Reflection.Emit;
using BenchmarkDotNet.Helpers.Reflection.Emit;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal class NonVoidConsumeEmitter : ConsumeEmitter
    {
        private LocalBuilder disassemblyDiagnoserLocal;

        public NonVoidConsumeEmitter(ConsumableTypeInfo consumableTypeInfo) : base(consumableTypeInfo)
        {
        }

        protected override void DeclareDisassemblyDiagnoserLocalsOverride(ILGenerator ilBuilder)
        {
            // optional local if default(T) uses .initobj
            disassemblyDiagnoserLocal = ilBuilder.DeclareOptionalLocalForReturnDefault(ConsumableInfo.WorkloadMethodReturnType);
        }

        protected override void EmitDisassemblyDiagnoserReturnDefaultOverride(ILGenerator ilBuilder)
        {
            ilBuilder.EmitReturnDefault(ConsumableInfo.WorkloadMethodReturnType, disassemblyDiagnoserLocal);
        }

        protected override void EmitActionAfterCallOverride(ILGenerator ilBuilder)
        {
            // IL_000a: pop
            ilBuilder.Emit(OpCodes.Pop);
        }
    }
}