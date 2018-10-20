using System.Reflection.Emit;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal class VoidConsumeEmitter : ConsumeEmitter
    {
        public VoidConsumeEmitter(ConsumableTypeInfo consumableTypeInfo) : base(consumableTypeInfo)
        {
        }

        protected override void EmitDisassemblyDiagnoserReturnDefaultOverride(ILGenerator ilBuilder)
        {
            /*
                IL_0001: ret
             */
            ilBuilder.Emit(OpCodes.Ret);
        }
    }
}