using System;
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

        public bool CanResolve(ICharacteristic characteristic) => resolvers.Any(r => r.CanResolve(characteristic));

        public T Resolve<T>(ICharacteristic<T> characteristic)
        {
            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(characteristic);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.Id}");
        }

        public object Resolve(ICharacteristic characteristic)
        {
            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(characteristic);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.Id}");
        }
    }
}