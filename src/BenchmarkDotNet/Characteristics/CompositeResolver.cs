using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BenchmarkDotNet.Characteristics
{
    public class CompositeResolver : IResolver
    {
        private readonly IResolver[] resolvers;

        public CompositeResolver(params IResolver[] resolvers)
        {
            this.resolvers = resolvers;
        }

        public bool CanResolve(Characteristic characteristic) => resolvers.Any(r => r.CanResolve(characteristic));

        public object Resolve(CharacteristicObject obj, Characteristic characteristic)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(obj, characteristic);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }

        public T Resolve<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(CharacteristicObject obj, Characteristic<T> characteristic)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(obj, characteristic);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }

        public object Resolve(CharacteristicObject obj, Characteristic characteristic, object defaultValue)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(obj, characteristic, defaultValue);
            return defaultValue;
        }

        public T Resolve<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(CharacteristicObject obj, Characteristic<T> characteristic, T defaultValue)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(obj, characteristic, defaultValue);
            return defaultValue;
        }
    }
}