using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Characteristics
{
    public class Resolver : IResolver
    {
        private readonly Dictionary<Characteristic, Func<CharacteristicObject, object>> resolvers = new Dictionary<Characteristic, Func<CharacteristicObject, object>>();

        protected void Register<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(Characteristic<T> characteristic, Func<T> resolver) =>
            resolvers[characteristic] = obj => resolver();

        protected void Register<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(Characteristic<T> characteristic, Func<CharacteristicObject, T> resolver) =>
            resolvers[characteristic] = obj => resolver(obj);

        public bool CanResolve(Characteristic characteristic) => resolvers.ContainsKey(characteristic);

        public object Resolve(CharacteristicObject obj, Characteristic characteristic)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            if (resolvers.TryGetValue(characteristic, out var resolver))
                return resolver(obj);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }

        public T Resolve<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(CharacteristicObject obj, Characteristic<T> characteristic)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            if (resolvers.TryGetValue(characteristic, out var resolver))
                return (T)resolver(obj);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }

        public object Resolve(CharacteristicObject obj, Characteristic characteristic, object defaultValue)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            if (resolvers.TryGetValue(characteristic, out var resolver))
                return resolver(obj);

            return defaultValue;
        }

        public T Resolve<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(CharacteristicObject obj, Characteristic<T> characteristic, T defaultValue)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            if (resolvers.TryGetValue(characteristic, out var resolver))
                return (T)resolver(obj);

            return defaultValue;
        }
    }
}