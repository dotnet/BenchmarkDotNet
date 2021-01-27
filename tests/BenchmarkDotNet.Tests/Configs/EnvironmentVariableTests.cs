using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs
{
    public class EnvironmentVariableTests
    {
        [Fact]
        public void AddVariableArray()
        {
            var job = new Job()
                .WithEnvironmentVariables(new EnvironmentVariable("a", "b"))
                .WithEnvironmentVariables(new EnvironmentVariable("c", "d"));
            Assert.Equal(1, job.Environment.EnvironmentVariables.Count);
        }

        [Fact]
        public void AddVariableArrayWithDuplications()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new Job().WithEnvironmentVariables(
                    new EnvironmentVariable("a", "b"),
                    new EnvironmentVariable("a", "c"));
            });
        }

        [Fact]
        public void AddOneVariable()
        {
            var job = new Job()
                .WithEnvironmentVariables(new EnvironmentVariable("a", "b"), new EnvironmentVariable("c", "d"))
                .WithEnvironmentVariable(new EnvironmentVariable("e", "f"));
            Assert.Equal(3, job.Environment.EnvironmentVariables.Count);
            Assert.Equal(new EnvironmentVariable("a", "b"), job.Environment.EnvironmentVariables[0]);
            Assert.Equal(new EnvironmentVariable("c", "d"), job.Environment.EnvironmentVariables[1]);
            Assert.Equal(new EnvironmentVariable("e", "f"), job.Environment.EnvironmentVariables[2]);
        }

        [Fact]
        public void OverrideOneVariable()
        {
            var job = new Job()
                .WithEnvironmentVariables(new EnvironmentVariable("a", "b"), new EnvironmentVariable("c", "d"))
                .WithEnvironmentVariable("c", "e");
            Assert.Equal(2, job.Environment.EnvironmentVariables.Count);
            Assert.Equal(new EnvironmentVariable("a", "b"), job.Environment.EnvironmentVariables[0]);
            Assert.Equal(new EnvironmentVariable("c", "e"), job.Environment.EnvironmentVariables[1]);
        }

        [Fact]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public void InvalidVariables()
        {
            Assert.Throws<ArgumentNullException>(() => new EnvironmentVariable(null, "x"));
            Assert.Throws<ArgumentNullException>(() => new EnvironmentVariable("x", null));
            Assert.Throws<ArgumentNullException>(() => new EnvironmentVariable(null, null));
        }

        [Fact]
        public void ClearVariables()
        {
            var job = new Job()
                .WithEnvironmentVariable("a", "b")
                .WithoutEnvironmentVariables();
            Assert.Empty(job.Environment.EnvironmentVariables);
        }
    }
}