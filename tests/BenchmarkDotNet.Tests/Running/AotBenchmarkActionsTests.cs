using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Perfolizer.Horology;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Running
{
    // The closed actions are what a Native AOT host uses instead of MakeGenericType; this checks they bind to and invoke
    // each supported return type (a method returning a class bound to Func<object>, Task<T> awaited as a Task, ...).
    public class AotBenchmarkActionsTests
    {
        private const int UnrollFactor = 4;

        public static TheoryData<string> SupportedMethods => new(
            nameof(Target.Int), nameof(Target.Double), nameof(Target.String), nameof(Target.Array), nameof(Target.StaticString),
            nameof(Target.TaskOfInt), nameof(Target.TaskOfString), nameof(Target.TaskOfStruct),
            nameof(Target.ValueTaskOfLong), nameof(Target.AsyncEnumerableOfInt), nameof(Target.AsyncEnumerableOfString),
            nameof(Target.RefInt), nameof(Target.RefReadonlyInt),
            nameof(Target.RegisteredStruct), nameof(Target.ValueTaskOfRegisteredClass), nameof(Target.RefRegisteredClass),
            nameof(Target.RefReadonlyRegisteredStruct), nameof(Target.AsyncEnumerableOfRegisteredStruct));

        static AotBenchmarkActionsTests()
        {
            // What the source generator emits for a project published with Native AOT.
            NativeAotRegistration.RegisterResultType<RegisteredStruct>();
            NativeAotRegistration.RegisterResultType<RegisteredClass>();
        }

        [Theory]
        [MemberData(nameof(SupportedMethods))]
        public async Task SupportedReturnType_InvokesTheMethod(string methodName)
        {
            foreach (bool consumeTasksSynchronously in new[] { false, true })
            {
                var target = new Target();
                var method = typeof(Target).GetMethod(methodName)!;
                Assert.True(AotBenchmarkActions.IsSupported(method.ReturnType));
                Assert.True(AotBenchmarkActions.TryCreate(method.IsStatic ? null : target, method, method.ReturnType, UnrollFactor, consumeTasksSynchronously, out var action));

                // The async enumerable actions don't support InvokeSingle (by design), the others are invoked once first.
                bool invokeSingle = !methodName.StartsWith("AsyncEnumerable", StringComparison.Ordinal);

                Target.Counter = 0;
                action.Setup();
                try
                {
                    if (invokeSingle)
                    {
                        await action.InvokeSingle();
                        Assert.Equal(1, Target.Counter);
                    }
                    await action.InvokeUnroll(11, new MockClock(Frequency.MHz));
                    Assert.Equal((invokeSingle ? 1 : 0) + UnrollFactor * 11, Target.Counter);
                }
                finally
                {
                    action.Cleanup();
                }
            }
        }

        [Theory]
        [InlineData(nameof(Target.UnregisteredStruct))]
        [InlineData(nameof(Target.ValueTaskOfUnregisteredClass))]
        [InlineData(nameof(Target.RefUnregisteredClass))]
        [InlineData(nameof(Target.Void))]
        public void UnsupportedReturnType_NotCreated(string methodName)
        {
            var method = typeof(Target).GetMethod(methodName)!;
            Assert.False(AotBenchmarkActions.IsSupported(method.ReturnType));
            Assert.False(AotBenchmarkActions.TryCreate(new Target(), method, method.ReturnType, UnrollFactor, false, out _));
        }

        public struct CustomStruct
        {
            public long Value;
        }

        public struct RegisteredStruct
        {
            public long Value;
        }

        public struct UnregisteredStruct
        {
            public long Value;
        }

        public class RegisteredClass
        {
        }

        public class UnregisteredClass
        {
        }

        public class Target
        {
            public static int Counter;
            private int field;
            private RegisteredClass registeredClass = new();
            private RegisteredStruct registeredStruct;
            private UnregisteredClass unregisteredClass = new();

            public int Int() => ++Counter;
            public double Double() => ++Counter;
            public string String() => (++Counter).ToString();
            public int[] Array() => [++Counter];
            public static string StaticString() => (++Counter).ToString();
            public Task<int> TaskOfInt() => Task.FromResult(++Counter);
            public async Task<string> TaskOfString() { await Task.Yield(); return (++Counter).ToString(); }
            public Task<CustomStruct> TaskOfStruct() => Task.FromResult(new CustomStruct { Value = ++Counter });
            public ValueTask<long> ValueTaskOfLong() => new(++Counter);
            public async IAsyncEnumerable<int> AsyncEnumerableOfInt() { await Task.Yield(); yield return ++Counter; }
            public async IAsyncEnumerable<string> AsyncEnumerableOfString() { await Task.Yield(); yield return (++Counter).ToString(); }

            public ref int RefInt() { ++Counter; return ref field; }
            public ref readonly int RefReadonlyInt() { ++Counter; return ref field; }
            public RegisteredStruct RegisteredStruct() => new() { Value = ++Counter };
            public ValueTask<RegisteredClass> ValueTaskOfRegisteredClass() { ++Counter; return new(registeredClass); }
            public ref RegisteredClass RefRegisteredClass() { ++Counter; return ref registeredClass; }
            public ref readonly RegisteredStruct RefReadonlyRegisteredStruct() { ++Counter; return ref registeredStruct; }
            public async IAsyncEnumerable<RegisteredStruct> AsyncEnumerableOfRegisteredStruct() { await Task.Yield(); yield return new() { Value = ++Counter }; }

            public UnregisteredStruct UnregisteredStruct() => default;
            public ValueTask<UnregisteredClass> ValueTaskOfUnregisteredClass() => new(new UnregisteredClass());
            public ref UnregisteredClass RefUnregisteredClass() => ref unregisteredClass;
            public void Void() { }
        }
    }
}
