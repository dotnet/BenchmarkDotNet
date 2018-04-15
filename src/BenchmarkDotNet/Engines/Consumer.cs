using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(byte byteValue) => byteHolder = byteValue;

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(sbyte sbyteValue) => sbyteHolder = sbyteValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(short shortValue) => shortHolder = shortValue;

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(ushort ushortValue) => ushortHolder = ushortValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(int intValue) => intHolder = intValue;

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(uint uintValue) => uintHolder = uintValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(bool boolValue) => boolHolder = boolValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(char charValue) => charHolder = charValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(float floatValue) => floatHolder = floatValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(double doubleValue) => Volatile.Write(ref doubleHolder, doubleValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(long longValue) => Volatile.Write(ref longHolder, longValue);

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(ulong ulongValue) => Volatile.Write(ref ulongHolder, ulongValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(string stringValue) => Volatile.Write(ref stringHolder, stringValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(object objectValue) => Volatile.Write(ref objectHolder, objectValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume<T>(T objectValue) where T : class // class constraint prevents from boxing structs
            => Volatile.Write(ref objectHolder, objectValue);

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