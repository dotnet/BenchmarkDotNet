﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BenchmarkDotNet.Characteristics
{
    public static class CharacteristicHelper
    {
        #region Helpers
        internal static bool IsCharacteristicObjectSubclass(Type type) =>
            type.GetTypeInfo().IsSubclassOf(typeof(CharacteristicObject));

        internal static bool IsCharacteristicSubclass(Type type) =>
            type.GetTypeInfo().IsSubclassOf(typeof(Characteristic));

        private static Characteristic AssertHasValue(MemberInfo member, Characteristic value)
        {
            // ReSharper disable once PossibleNullReferenceException
            if (value == null)
                throw new ArgumentException(
                    $"The value of {member.DeclaringType.Name}.{member.Name} is null");

            return value;
        }

        internal static string GetMemberName<TOwner, T>(Expression<Func<TOwner, T>> propertyGetterExpression)
            where TOwner : CharacteristicObject =>
                ((MemberExpression)propertyGetterExpression.Body).Member.Name;

        internal static Type GetDeclaringType<TOwner, T>(Expression<Func<TOwner, T>> propertyGetterExpression)
            where TOwner : CharacteristicObject =>
                ((MemberExpression)propertyGetterExpression.Body).Member.DeclaringType;

        public static bool IsPresentableCharacteristic(this Characteristic c, bool includeIgnoreOnApply = false) =>
            !c.HasChildCharacteristics && (includeIgnoreOnApply || !c.IgnoreOnApply);
        #endregion

        #region Type characteristics
        private static readonly IReadOnlyList<Characteristic> EmptyCharacteristics = Array.Empty<Characteristic>();

        private static readonly ConcurrentDictionary<Type, IReadOnlyList<Characteristic>> thisTypeCharacteristics =
            new ConcurrentDictionary<Type, IReadOnlyList<Characteristic>>();

        public static IReadOnlyList<Characteristic> GetThisTypeCharacteristics(this CharacteristicObject obj) =>
            GetThisTypeCharacteristics(obj.GetType());

        public static IReadOnlyList<Characteristic> GetThisTypeCharacteristics(Type characteristicObjectType)
        {
            if (!IsCharacteristicObjectSubclass(characteristicObjectType))
                return EmptyCharacteristics;
            return thisTypeCharacteristics.GetOrAdd(characteristicObjectType, t => GetThisTypeCharacteristicsCore(t));
        }

        private static IReadOnlyList<Characteristic> GetThisTypeCharacteristicsCore(Type characteristicObjectType)
        {
            var fieldValues = characteristicObjectType.GetTypeInfo()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static)
                .Where(f => IsCharacteristicSubclass(f.FieldType))
                .Select(f => AssertHasValue(f, (Characteristic)f.GetValue(null)));

            var propertyValues = characteristicObjectType.GetTypeInfo()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static)
                .Where(p => p.GetMethod != null && IsCharacteristicSubclass(p.PropertyType))
                .Select(p => AssertHasValue(p, (Characteristic)p.GetValue(null)));

            // DONTOUCH: DO NOT change the order of characteristic as it may break logic of some operations.
            return fieldValues
                .Concat(propertyValues)
                .Distinct()
                .OrderBy(k => k.HasChildCharacteristics ? 1 : 0)
                .ThenBy(k => k.Id)
                .ToArray();
        }

        private static readonly ConcurrentDictionary<Type, IReadOnlyList<Characteristic>> allTypeCharacteristics =
            new ConcurrentDictionary<Type, IReadOnlyList<Characteristic>>();

        public static IReadOnlyList<Characteristic> GetAllCharacteristics(this CharacteristicObject obj) =>
            GetAllCharacteristics(obj.GetType());

        public static IReadOnlyList<Characteristic> GetAllCharacteristics(Type characteristicObjectType)
        {
            if (!IsCharacteristicObjectSubclass(characteristicObjectType))
                return EmptyCharacteristics;
            return allTypeCharacteristics.GetOrAdd(characteristicObjectType, t => GetAllCharacteristicsCore(t));
        }

        private static IReadOnlyList<Characteristic> GetAllCharacteristicsCore(Type characteristicObjectType)
        {
            var result = new List<Characteristic>();

            FillAllCharacteristicsCore(characteristicObjectType, result, new HashSet<Characteristic>());

            return result.ToArray();
        }

        private static void FillAllCharacteristicsCore(
            Type characteristicObjectType, List<Characteristic> result, HashSet<Characteristic> visited)
        {
            // DONTOUCH: DO NOT change the order of characteristic as it may break logic of some operations.

            var characteristics = GetThisTypeCharacteristics(characteristicObjectType);
            foreach (var characteristic in characteristics.Where(c => !c.HasChildCharacteristics))
            {
                if (!visited.Add(characteristic))
                    continue;

                result.Add(characteristic);
            }

            foreach (var characteristic in characteristics.Where(c => c.HasChildCharacteristics))
            {
                if (!visited.Add(characteristic))
                    continue;

                result.Add(characteristic);
                FillAllCharacteristicsCore(characteristic.CharacteristicType, result, visited);
            }
        }

        public static IReadOnlyList<Characteristic> GetAllPresentableCharacteristics(Type characteristicObjectType, bool includeIgnoreOnApply = false) =>
            GetAllCharacteristics(characteristicObjectType)
                .Where(c => c.IsPresentableCharacteristic(includeIgnoreOnApply))
                .ToArray();
        #endregion
    }
}