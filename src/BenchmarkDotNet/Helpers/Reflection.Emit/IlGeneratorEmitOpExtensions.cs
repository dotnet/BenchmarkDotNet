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

        public static void EmitLdindStind(this ILGenerator ilBuilder, Type resultType)
        {
            if (!resultType.IsByRef)
                throw new NotSupportedException($"Cannot emit indirect op for non-reference {resultType}.");

            // The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single
            var valueType = resultType.GetElementType();
            if (valueType?.IsEnum ?? false)
                valueType = valueType.GetEnumUnderlyingType();

            switch (valueType)
            {
                case Type t when t == typeof(bool):
                    /*
                        IL_0018: ldind.u1
                        IL_0019: stind.i1
                     */
                    ilBuilder.Emit(OpCodes.Ldind_U1);
                    ilBuilder.Emit(OpCodes.Stind_I1);
                    break;
                case Type t when t == typeof(byte):
                    /*
                        IL_0018: ldind.u1
                        IL_0019: stind.i1
                     */
                    ilBuilder.Emit(OpCodes.Ldind_U1);
                    ilBuilder.Emit(OpCodes.Stind_I1);
                    break;
                case Type t when t == typeof(sbyte):
                    /*
                        IL_0018: ldind.i1
                        IL_0019: stind.i1
                     */
                    ilBuilder.Emit(OpCodes.Ldind_I1);
                    ilBuilder.Emit(OpCodes.Stind_I1);
                    break;
                case Type t when t == typeof(short):
                    /*
                        IL_0018: ldind.i2
                        IL_0019: stind.i2
                     */
                    ilBuilder.Emit(OpCodes.Ldind_I2);
                    ilBuilder.Emit(OpCodes.Stind_I2);
                    break;
                case Type t1 when t1 == typeof(ushort):
                case Type t2 when t2 == typeof(char):
                    /*
                        IL_0018: ldind.u2
                        IL_0019: stind.i2
                     */
                    ilBuilder.Emit(OpCodes.Ldind_U2);
                    ilBuilder.Emit(OpCodes.Stind_I2);
                    break;
                case Type t when t == typeof(int):
                    /*
                        IL_0018: ldind.i4
                        IL_0019: stind.i4
                     */
                    ilBuilder.Emit(OpCodes.Ldind_I4);
                    ilBuilder.Emit(OpCodes.Stind_I4);
                    break;
                case Type t when t == typeof(uint):
                    /*
                        IL_0018: ldind.i4
                        IL_0019: stind.i4
                     */
                    ilBuilder.Emit(OpCodes.Ldind_U4);
                    ilBuilder.Emit(OpCodes.Stind_I4);
                    break;
                case Type t1 when t1 == typeof(ulong):
                case Type t2 when t2 == typeof(long):
                    /*
                        IL_0018: ldind.i8
                        IL_0019: stind.i8
                     */
                    ilBuilder.Emit(OpCodes.Ldind_I8);
                    ilBuilder.Emit(OpCodes.Stind_I8);
                    break;
                case Type t1 when t1 == typeof(IntPtr):
                case Type t2 when t2 == typeof(UIntPtr):
                    /*
                        IL_0018: ldind.i
                        IL_0019: stind.i
                     */
                    ilBuilder.Emit(OpCodes.Ldind_I);
                    ilBuilder.Emit(OpCodes.Stind_I);
                    break;
                case Type t when t == typeof(double):
                    /*
                        IL_0018: ldind.r8
                        IL_0019: stind.i8
                     */
                    ilBuilder.Emit(OpCodes.Ldind_R8);
                    ilBuilder.Emit(OpCodes.Stind_R8);
                    break;
                case Type t when t == typeof(float):
                    /*
                        IL_0018: ldind.r4
                        IL_0019: stind.i4
                     */
                    ilBuilder.Emit(OpCodes.Ldind_R4);
                    ilBuilder.Emit(OpCodes.Stind_R4);
                    break;
                case Type t when t.IsClass || t.IsInterface:
                    /*
                        IL_0018: ldind.ref
                        IL_0019: stind.ref
                     */
                    ilBuilder.Emit(OpCodes.Ldind_Ref);
                    ilBuilder.Emit(OpCodes.Stind_Ref);
                    break;
                case Type t when t.IsEnum || t.IsValueType:
                    /*
                        IL_0018: ldobj valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.TimeSpan>
                        IL_0019: stobj valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.TimeSpan>
                     */
                    ilBuilder.Emit(OpCodes.Ldobj, valueType);
                    ilBuilder.Emit(OpCodes.Stobj, valueType);
                    break;
                default:
                    throw new NotSupportedException($"Cannot emit indirect store for {resultType}.");
            }
        }
    }
}