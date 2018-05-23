using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class GenericBuilderTests
    {
        [Fact]
        public void TestBuildGenericWithOneArgument()
        {
            // Arrange & Act
            var types = GenericBuilderHelper.GetRunnableBenchmarks(new [] {typeof(OneArgGenericBenchmark<>)});
            
            // Assert
            Assert.Equal(types.Length, 2);
        }
        
        [Fact]
        public void TestBuildGenericWithTwoArguments()
        {
            // Arrange & Act
            var types = GenericBuilderHelper.GetRunnableBenchmarks(new [] {typeof(TwoArgGenericBenchmark<,>)});
            
            // Assert
            Assert.Equal(types.Length, 2);
        }
        
        [Fact]
        public void TestBuildGenericWithThreeArguments()
        {
            // Arrange & Act
            var types = GenericBuilderHelper.GetRunnableBenchmarks(new [] {typeof(ThreeArgGenericBenchmark<,,>)});
            
            // Assert
            Assert.Equal(types.Length, 2);
        }
        
        [Fact]
        public void TestBuildGenericWithWrongAttributes()
        {
            // Arrange & Act
            var types = GenericBuilderHelper.GetRunnableBenchmarks(new [] {typeof(GenericBenchmarkWithWrongAttribute<,>)});
            
            // Assert
            Assert.Equal(types.Length, 2);
        }
        
        [Fact]
        public void TestBuildGenericWithConstraints()
        {
            // Arrange & Act
            var types = GenericBuilderHelper.GetRunnableBenchmarks(new [] {typeof(GenericBenchmarkWithConstraints<,>)});
            
            // Assert
            Assert.Equal(types.Length, 2);
        }
        
        [Fact]
        public void TestBuildGenericWithConstraintsWrongArgs()
        {
            // Arrange & Act
            var types = GenericBuilderHelper.GetRunnableBenchmarks(new [] {typeof(GenericBenchmarkWithConstraintsWrongArgs<,>)});
            
            // Assert
            Assert.Equal(types.Length, 1);
        }
    }
    
    [GenericBenchmark(typeof(int))]
    [GenericBenchmark(typeof(char))]
    public class OneArgGenericBenchmark<T>
    {
        [Benchmark] public T CreateT() => Activator.CreateInstance<T>();
    }
    
    [GenericBenchmark(typeof(int), typeof(char))]
    [GenericBenchmark(typeof(char), typeof(string))]
    public class TwoArgGenericBenchmark<T1, T2>
    {
        [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

        [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
    }
    
    [GenericBenchmark(typeof(int), typeof(char),  typeof(string))]
    [GenericBenchmark(typeof(char), typeof(string), typeof(byte))]
    public class ThreeArgGenericBenchmark<T1, T2, T3>
    {
        [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

        [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
        
        [Benchmark] public T3 CreateT3() => Activator.CreateInstance<T3>();
    }
    
    [GenericBenchmark(typeof(int), typeof(char))]
    [GenericBenchmark(typeof(char), typeof(string))]
    [GenericBenchmark(typeof(char))]
    public class GenericBenchmarkWithWrongAttribute<T1, T2>
    {
        [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

        [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
    }
    
    [GenericBenchmark(typeof(int), typeof(char))]
    [GenericBenchmark(typeof(char), typeof(byte))]
    public class GenericBenchmarkWithConstraints<T1, T2> where T1 : struct
                                                         where T2 : struct 
    {
        [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

        [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
    }
    
    [GenericBenchmark(typeof(int), typeof(char))]
    [GenericBenchmark(typeof(char), typeof(string))]
    public class GenericBenchmarkWithConstraintsWrongArgs<T1, T2> where T1 : struct
                                                                  where T2 : struct 
    {
        [Benchmark] public T1 CreateT1() => Activator.CreateInstance<T1>();

        [Benchmark] public T2 CreateT2() => Activator.CreateInstance<T2>();
    }
}