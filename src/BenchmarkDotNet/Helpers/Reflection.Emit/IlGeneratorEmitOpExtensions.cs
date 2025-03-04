using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Helpers.Reflection.Emit
{
    internal static class IlGeneratorEmitOpExtensions
    {
        public static void EmitStloc(this ILGenerator ilBuilder, LocalBuilder local)
        {
            switch (local.LocalIndex)
            {
                case 0:
                    ilBuilder.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    ilBuilder.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    ilBuilder.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    ilBuilder.Emit(OpCodes.Stloc_3);
                    break;
                case var i when i < 255:
                    ilBuilder.Emit(OpCodes.Stloc_S, (byte)local.LocalIndex);
                    break;
                default:
                    ilBuilder.Emit(OpCodes.Stloc, checked((short)local.LocalIndex));
                    break;
            }
        }

        public static void EmitLdloc(this ILGenerator ilBuilder, LocalBuilder local)
        {
            switch (local.LocalIndex)
            {
                case 0:
                    ilBuilder.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    ilBuilder.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    ilBuilder.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    ilBuilder.Emit(OpCodes.Ldloc_3);
                    break;
                case var i when i < 255:
                    ilBuilder.Emit(OpCodes.Ldloc_S, (byte)local.LocalIndex);
                    break;
                default:
                    ilBuilder.Emit(OpCodes.Ldloc, checked((short)local.LocalIndex));
                    break;
            }
        }

        public static void EmitLdloca(this ILGenerator ilBuilder, LocalBuilder local)
        {
            switch (local.LocalIndex)
            {
                case var i when i < 255:
                    ilBuilder.Emit(OpCodes.Ldloca_S, (byte)local.LocalIndex);
                    break;
                default:
                    ilBuilder.Emit(OpCodes.Ldloca, checked((short)local.LocalIndex));
                    break;
            }
        }

        public static void EmitLdargs(this ILGenerator ilBuilder, IEnumerable<ParameterInfo> arguments)
        {
            foreach (var argument in arguments)
            {
                ilBuilder.EmitLdarg(argument);
            }
        }

        public static void EmitLdLocals(this ILGenerator ilBuilder, IEnumerable<LocalBuilder> locals)
        {
            foreach (var local in locals)
            {
                ilBuilder.EmitLdloc(local);
            }
        }

        public static void EmitLdarg(this ILGenerator ilBuilder, ParameterInfo argument)
        {
            // this is passed as Ldarg_0
            var position = argument.Position;
            if (!((MethodBase)argument.Member).IsStatic)
                position++;

            switch (position)
            {
                case 0:
                    ilBuilder.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    ilBuilder.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    ilBuilder.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    ilBuilder.Emit(OpCodes.Ldarg_3);
                    break;
                case var i when i < 255:
                    ilBuilder.Emit(OpCodes.Ldarg_S, (byte)position);
                    break;
                default:
                    ilBuilder.Emit(OpCodes.Ldarg, checked((short)position));
                    break;
            }
        }

        public static void EmitStarg(this ILGenerator ilBuilder, ParameterInfo argument)
        {
            var position = argument.Position;
            if (!((MethodBase) argument.Member).IsStatic)
                position++;

            if (position < 255)
            {
                ilBuilder.Emit(OpCodes.Starg_S, (byte) position);
            }
            else
            {
                ilBuilder.Emit(OpCodes.Starg, checked((short) position));
            }
        }
    }
}