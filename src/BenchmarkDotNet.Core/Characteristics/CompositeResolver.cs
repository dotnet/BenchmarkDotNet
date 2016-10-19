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

        public bool CanResolve(Characteristic characteristic) => resolvers.Any(r => r.CanResolve(characteristic));

        public object Resolve(JobMode jobMode, Characteristic characteristic)
        {
            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(jobMode, characteristic);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }

        public T Resolve<T>(JobMode jobMode, Characteristic<T> characteristic)
        {
            var resolver = resolvers.FirstOrDefault(r => r.CanResolve(characteristic));
            if (resolver != null)
                return resolver.Resolve(jobMode, characteristic);
            throw new InvalidOperationException($"There is no default resolver for {characteristic.FullId}");
        }
    }
}