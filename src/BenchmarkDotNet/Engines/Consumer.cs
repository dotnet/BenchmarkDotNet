using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;

// ReSharper disable NotAccessedField.Local
namespace BenchmarkDotNet.Engines
{
    public class Consumer
    {
        private static readonly HashSet<Type> SupportedTypes
            = new HashSet<Type>(
                typeof(Consumer).GetTypeInfo()
                                .DeclaredFields
                                .Where(field => !field.IsStatic) // exclude this HashSet itself
                                .Select(field => field.FieldType));

        private volatile byte byteHolder;
        private volatile sbyte sbyteHolder;
        private volatile short shortHolder;
        private volatile ushort ushortHolder;
        private volatile int intHolder;
        private volatile uint uintHolder;
        private volatile bool boolHolder;
        private volatile char charHolder;
        private volatile float floatHolder;
        private double doubleHolder;
        private long longHolder;
        private ulong ulongHolder;
        private string stringHolder;
        private object objectHolder;
        private IntPtr ptrHolder;
        private UIntPtr uptrHolder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(byte byteValue) => byteHolder = byteValue;

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(sbyte sbyteValue) => sbyteHolder = sbyteValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(short shortValue) => shortHolder = shortValue;

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(ushort ushortValue) => ushortHolder = ushortValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(int intValue) => intHolder = intValue;

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(uint uintValue) => uintHolder = uintValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(bool boolValue) => boolHolder = boolValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(char charValue) => charHolder = charValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(float floatValue) => floatHolder = floatValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(double doubleValue) => Volatile.Write(ref doubleHolder, doubleValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(long longValue) => Volatile.Write(ref longHolder, longValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(IntPtr intPtrValue) => Volatile.Write(ref ptrHolder, intPtrValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(UIntPtr uintPtrValue) => Volatile.Write(ref uptrHolder, uintPtrValue);

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(ulong ulongValue) => Volatile.Write(ref ulongHolder, ulongValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(string stringValue) => Volatile.Write(ref stringHolder, stringValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume(object objectValue) => Volatile.Write(ref objectHolder, objectValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [PublicAPI]
        public void Consume<T>(T objectValue) where T : class // class constraint prevents from boxing structs
            => Volatile.Write(ref objectHolder, objectValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Consume<T>(T* ptrValue) where T: unmanaged => Volatile.Write(ref ptrHolder, (IntPtr)ptrValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Consume(void* ptrValue) => Volatile.Write(ref ptrHolder, (IntPtr)ptrValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume<T>(in T value)
        {
            if (typeof(T) == typeof(byte))
                byteHolder = (byte)(object)value;
            else if (typeof(T) == typeof(sbyte))
                sbyteHolder = (sbyte)(object)value;
            else if (typeof(T) == typeof(short))
                shortHolder = (short)(object)value;
            else if (typeof(T) == typeof(ushort))
                ushortHolder = (ushort)(object)value;
            else if (typeof(T) == typeof(int))
                intHolder = (int)(object)value;
            else if (typeof(T) == typeof(uint))
                uintHolder = (uint)(object)value;
            else if (typeof(T) == typeof(bool))
                boolHolder = (bool)(object)value;
            else if (typeof(T) == typeof(char))
                charHolder = (char)(object)value;
            else if (typeof(T) == typeof(float))
                floatHolder = (float)(object)value;
            else if (typeof(T) == typeof(double))
                Volatile.Write(ref doubleHolder, (double)(object)value);
            else if (typeof(T) == typeof(long))
                Volatile.Write(ref longHolder, (long)(object)value);
            else if (typeof(T) == typeof(ulong))
                Volatile.Write(ref ulongHolder, (ulong)(object)value);
            else if (default(T) == null)
                objectHolder = (object) value;
            else
                ValueTypesConsumer(value); // non-primitive value types
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ValueTypesConsumer<T>(in T _) { }

        internal static bool IsConsumable(Type type)
            => SupportedTypes.Contains(type) || type.GetTypeInfo().IsClass || type.GetTypeInfo().IsInterface;

        internal static bool HasConsumableField(Type type, out FieldInfo consumableField)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsEnum)
            {
                // Enums are tricky bastards which report "value__" field, which is public for reflection, but inaccessible via C#
                consumableField = null;
                return false;
            }

            var publicInstanceFields = typeInfo.DeclaredFields
                                               .Where(field => field.IsPublic && !field.IsStatic)
                                               .ToArray();

            consumableField = publicInstanceFields.FirstOrDefault(field => IsConsumable(field.FieldType));

            return consumableField != null;
        }
    }
}