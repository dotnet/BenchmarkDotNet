using System;
using System.Reflection;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkTarget
    {
        public Type Type { get; }
        public MethodInfo Method { get; }
        public string Description { get; }

        public string Caption => Type.Name.WithoutSuffix("Competition") + "_" + Method.Name;

        public BenchmarkTarget(Type type, MethodInfo method, string description = null)
        {
            Type = type;
            Method = method;
            Description = description ?? Caption;
        }
    }
}