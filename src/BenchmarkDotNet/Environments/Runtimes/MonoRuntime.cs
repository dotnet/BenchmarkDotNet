﻿using System;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class MonoRuntime : Runtime, IEquatable<MonoRuntime>
    {
        public static readonly MonoRuntime Default = new MonoRuntime("Mono");
        public static readonly MonoRuntime Mono60 = new MonoRuntime("Mono with .NET 6.0", RuntimeMoniker.Mono60, "net6.0", isDotNetBuiltIn: true);
        public static readonly MonoRuntime Mono70 = new MonoRuntime("Mono with .NET 7.0", RuntimeMoniker.Mono70, "net7.0", isDotNetBuiltIn: true);
        public static readonly MonoRuntime Mono80 = new MonoRuntime("Mono with .NET 8.0", RuntimeMoniker.Mono80, "net8.0", isDotNetBuiltIn: true);

        public string CustomPath { get; }

        public string AotArgs { get; }

        public override bool IsAOT => !string.IsNullOrEmpty(AotArgs);

        public string MonoBclPath { get; }

        internal bool IsDotNetBuiltIn { get; }

        private MonoRuntime(string name) : base(RuntimeMoniker.Mono, "mono", name) { }

        private MonoRuntime(string name, RuntimeMoniker runtimeMoniker, string msBuildMoniker, bool isDotNetBuiltIn) : base(runtimeMoniker, msBuildMoniker, name)
        {
            IsDotNetBuiltIn = isDotNetBuiltIn;
        }

        public MonoRuntime(string name, string customPath) : this(name) => CustomPath = customPath;

        public MonoRuntime(string name, string customPath, string aotArgs, string monoBclPath) : this(name)
        {
            CustomPath = customPath;
            AotArgs = aotArgs;
            MonoBclPath = monoBclPath;
        }

        public override bool Equals(object obj) => obj is MonoRuntime other && Equals(other);

        public bool Equals(MonoRuntime other)
            => base.Equals(other) && Name == other?.Name && CustomPath == other?.CustomPath && AotArgs == other?.AotArgs && MonoBclPath == other?.MonoBclPath;

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Name, CustomPath, AotArgs, MonoBclPath);
    }
}