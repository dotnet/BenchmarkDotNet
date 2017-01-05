using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Characteristics
{
    public class Resolver : IResolver
    {
        private readonly Dictionary<Characteristic, Func<object>> resolvers = new Dictionary<Characteristic, Func<object>>();

        protected void Register<T>(Characteristic<T> characteristic, Func<T> resolver) =>
            resolvers[characteristic] = () => resolver();

        public bool CanResolve(Characteristic characteristic) => resolvers.ContainsKey(characteristic);

        public object Resolve(CharacteristicObject obj, Characteristic characteristic)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            Func<object> resolver;
            if (resolvers.TryGetValue(characteristic, out resolver))
                return resolver();
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }

        public T Resolve<T>(CharacteristicObject obj, Characteristic<T> characteristic)
        {
            if (obj.HasValue(characteristic))
                return characteristic[obj];

            Func<object> resolver;
            if (resolvers.TryGetValue(characteristic, out resolver))
                return (T)resolver();
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }
    }
}