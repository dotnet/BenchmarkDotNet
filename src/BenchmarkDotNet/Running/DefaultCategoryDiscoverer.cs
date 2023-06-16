using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Running
{
    public class DefaultCategoryDiscoverer : ICategoryDiscoverer
    {
        public static readonly ICategoryDiscoverer Instance = new DefaultCategoryDiscoverer();

        private readonly bool inherit;

        public DefaultCategoryDiscoverer(bool inherit = true)
        {
            this.inherit = inherit;
        }

        public virtual string[] GetCategories(MethodInfo method)
        {
            var attributes = new List<BenchmarkCategoryAttribute>();
            attributes.AddRange(method.GetCustomAttributes(typeof(BenchmarkCategoryAttribute), inherit).OfType<BenchmarkCategoryAttribute>());
            var type = method.ReflectedType;
            if (type != null)
            {
                attributes.AddRange(type.GetTypeInfo().GetCustomAttributes(typeof(BenchmarkCategoryAttribute), inherit).OfType<BenchmarkCategoryAttribute>());
                attributes.AddRange(type.GetTypeInfo().Assembly.GetCustomAttributes().OfType<BenchmarkCategoryAttribute>());
            }
            if (attributes.Count == 0)
                return Array.Empty<string>();
            return attributes.SelectMany(attr => attr.Categories).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}