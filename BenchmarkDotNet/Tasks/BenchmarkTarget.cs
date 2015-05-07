using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkTarget
    {
        public Type Type { get; }
        public MethodInfo Method { get; }
        public long OperationsPerMethod { get; }
        public string Description { get; }

        public string Caption => Type.Name.WithoutSuffix("Competition") + "_" + Method.Name;

        public BenchmarkTarget(Type type, MethodInfo method, string description = null)
        {
            Type = type;
            Method = method;
            OperationsPerMethod = method.ResolveAttribute<OperationCountAttribute>()?.Count ?? 1;
            Description = description ?? Caption;
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(Type), Type.Name);
                yield return new BenchmarkProperty(nameof(Method), Method.Name);
            }
        }
    }
}