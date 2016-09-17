using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Characteristics
{
    public class Resolver : IResolver
    {
        private readonly Dictionary<string, Func<object>> resolvers = new Dictionary<string, Func<object>>();

        protected void Register<T>(string id, Func<T> resolver)
        {
            resolvers[id] = () => resolver();
        }

        protected void Register<T>(ICharacteristic<T> characteristic, Func<T> resolver) => Register(characteristic.Id, resolver);

        public bool CanResolve(ICharacteristic characteristic) => resolvers.ContainsKey(characteristic.Id);

        public T Resolve<T>(ICharacteristic<T> characteristic)
        {
            if (!characteristic.IsDefault)
                return characteristic.SpecifiedValue;

            Func<object> resolver;
            if (resolvers.TryGetValue(characteristic.Id, out resolver))
                return (T) resolver();
            throw new InvalidOperationException($"There is no default resolver for {characteristic.Id}");
        }

        public object Resolve(ICharacteristic characteristic)
        {
            if (!characteristic.IsDefault)
                return characteristic.ObjectValue;

            Func<object> resolver;
            if (resolvers.TryGetValue(characteristic.Id, out resolver))
                return resolver();
            throw new InvalidOperationException($"There is no default resolver for {characteristic.Id}");
        }
    }
}