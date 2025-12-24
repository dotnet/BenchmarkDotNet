using System;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Helpers.Reflection.Emit;

internal static class IlGeneratorDefaultValueExtensions
{
    public static void EmitSetFieldToDefault(this ILGenerator ilBuilder, FieldInfo fieldInfo)
    {
        var resultType = fieldInfo.FieldType;
        switch (resultType)
        {
            case null:
            case Type t when t == typeof(void):
                throw new ArgumentException();
            case Type t when t.IsValueType:
                ilBuilder.Emit(OpCodes.Ldflda, fieldInfo);
                ilBuilder.Emit(OpCodes.Initobj, resultType);
                break;
            default:
                ilBuilder.Emit(OpCodes.Ldnull);
                ilBuilder.Emit(OpCodes.Stfld, fieldInfo);
                break;
        }
    }

    public static void MaybeEmitSetLocalToDefault(this ILGenerator ilBuilder, LocalBuilder? optionalLocal)
    {
        var resultType = optionalLocal?.LocalType;
        switch (resultType)
        {
            case null:
            case Type t when t == typeof(void):
                break;
            case Type t when t.IsClass || t.IsInterface:
                ilBuilder.Emit(OpCodes.Ldnull);
                ilBuilder.EmitStloc(optionalLocal);
                break;
            case Type t when t.UseInitObjForInitLocal():
                EmitInitObj(ilBuilder, resultType, optionalLocal);
                break;
            default:
                EmitLoadDefaultPrimitive(ilBuilder, resultType);
                ilBuilder.EmitStloc(optionalLocal);
                break;
        }
    }

    private static bool IsInitLocalPrimitive(this Type t)
    {
        // var x = default(T):
        // C# compiler uses special logic for enum defaults and primitive defaults
        // On init local case this logic does not apply for IntPtr & UIntPtr.

        if (t == typeof(void))
            return true;

        if (t.IsEnum)
            return true;

        return t.IsPrimitive
               && t != typeof(IntPtr)
               && t != typeof(UIntPtr);
    }

    private static bool UseInitObjForInitLocal(this Type resultType)
    {
        return resultType.IsValueType && !resultType.IsInitLocalPrimitive();
    }

    private static void EmitInitObj(ILGenerator ilBuilder, Type resultType, LocalBuilder optionalLocalForInitobj)
    {
        if (optionalLocalForInitobj == null)
            throw new ArgumentNullException(nameof(optionalLocalForInitobj));

        /*
            IL_0000: ldloca.s 0
            IL_0002: initobj [mscorlib]System.DateTime
        */
        ilBuilder.EmitLdloca(optionalLocalForInitobj);
        ilBuilder.Emit(OpCodes.Initobj, resultType);
    }

    private static void EmitLoadDefaultPrimitive(this ILGenerator ilBuilder, Type resultType)
    {
        var valueType = resultType;
        if (valueType.IsEnum)
            valueType = resultType.GetEnumUnderlyingType();

        // The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single
        // + custom logic for decimal
        switch (valueType)
        {
            case Type t0 when t0 == typeof(bool):
            case Type t1 when t1 == typeof(byte):
            case Type t2 when t2 == typeof(sbyte):
            case Type t3 when t3 == typeof(short):
            case Type t4 when t4 == typeof(ushort):
            case Type t5 when t5 == typeof(int):
            case Type t6 when t6 == typeof(uint):
            case Type t7 when t7 == typeof(char):
                ilBuilder.Emit(OpCodes.Ldc_I4_0);
                break;
            case Type t1 when t1 == typeof(ulong):
            case Type t2 when t2 == typeof(long):
                /*
                    // return 0L;
                    IL_0000: ldc.i4.0
                    IL_0001: conv.i8
                    // return 0uL;
                    IL_0000: ldc.i4.0
                    IL_0001: conv.i8
                 */
                ilBuilder.Emit(OpCodes.Ldc_I4_0);
                ilBuilder.Emit(OpCodes.Conv_I8);
                break;
            case Type t when t == typeof(IntPtr):
                /*
                    IL_0000: ldc.i4.0
                    IL_0001: conv.i
                 */
                ilBuilder.Emit(OpCodes.Ldc_I4_0);
                ilBuilder.Emit(OpCodes.Conv_I);
                break;
            case Type t when t == typeof(UIntPtr):
                /*
                    IL_0000: ldc.i4.0
                    IL_0001: conv.u
                 */
                ilBuilder.Emit(OpCodes.Ldc_I4_0);
                ilBuilder.Emit(OpCodes.Conv_U);
                break;
            case Type t when t == typeof(double):
                ilBuilder.Emit(OpCodes.Ldc_R8, 0.0d);
                break;
            case Type t when t == typeof(float):
                ilBuilder.Emit(OpCodes.Ldc_R4, 0.0f);
                break;
            case Type t when t == typeof(decimal):
                /*
                    // return decimal.Zero;
                    IL_0011: ldsfld valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::Zero
                 */
                var zeroField = typeof(decimal).GetField(nameof(decimal.Zero));
                ilBuilder.Emit(OpCodes.Ldsfld, zeroField);
                break;
            default:
                throw new NotSupportedException($"Cannot emit default for {resultType}.");
        }
    }
}