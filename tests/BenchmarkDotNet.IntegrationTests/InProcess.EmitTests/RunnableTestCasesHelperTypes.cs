using System;
using System.Runtime.CompilerServices;
// ReSharper disable UnusedMember.Global

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests
{
    public enum CustomEnumNonConsumable
    {
    }

    public enum CustomEnumConsumable
    {
        Default = 0
    }


    public struct CustomStructNonConsumable
    {
    }

    public struct CustomStructConsumable
    {
        public int Value;
    }

    public class CustomStructConsumable<T>
    {
        public T Value;
    }

    public class CustomClassConsumable
    {
    }

    public class CustomClassConsumable<T>
    {
        public T Value;
    }

    public struct CustomAwaitableStruct
    {
        public CustomAwaiterStruct GetAwaiter() => new CustomAwaiterStruct();
    }

    public struct CustomAwaitableStruct2
    {
        public CustomAwaiterClass GetAwaiter() => new CustomAwaiterClass();
    }

    public struct CustomAwaitableClass
    {
        public CustomAwaiterStruct GetAwaiter() => new CustomAwaiterStruct();
    }

    public struct CustomAwaitableClass2
    {
        public CustomAwaiterClass GetAwaiter() => new CustomAwaiterClass();
    }

    public struct CustomAwaiterStruct : INotifyCompletion
    {
        public bool IsCompleted => true;
        public double GetResult() => 0;

        public void OnCompleted(Action continuation)
        {
        }
    }

    public class CustomAwaiterClass : INotifyCompletion
    {
        public bool IsCompleted => true;
        public double GetResult() => 0;

        public void OnCompleted(Action continuation)
        {
        }
    }
}