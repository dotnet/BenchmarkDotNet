using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>Helper class that creates <see cref="BenchmarkAction"/> instances. </summary>
    public static partial class BenchmarkActionFactory
    {
        private class DummyInstance
        {
            [UsedImplicitly]
            private int dummyField;

            public void Dummy()
            {
                // copy-pasted as emit is not supported across all platforms
                dummyField++; // 0
                dummyField++; // 1
                dummyField++; // 2
                dummyField++; // 3
                dummyField++; // 4
                dummyField++; // 5
                dummyField++; // 6
                dummyField++; // 7
                dummyField++; // 8
                dummyField++; // 9
                dummyField++; // 10
                dummyField++; // 11
                dummyField++; // 12
                dummyField++; // 13
                dummyField++; // 14
                dummyField++; // 15
                dummyField++; // 16
                dummyField++; // 17
                dummyField++; // 18
                dummyField++; // 19
                dummyField++; // 20
                dummyField++; // 21
                dummyField++; // 22
                dummyField++; // 23
                dummyField++; // 24
                dummyField++; // 25
                dummyField++; // 26
                dummyField++; // 27
                dummyField++; // 28
                dummyField++; // 29
                dummyField++; // 30
                dummyField++; // 31
                dummyField++; // 32
                dummyField++; // 33
                dummyField++; // 34
                dummyField++; // 35
                dummyField++; // 36
                dummyField++; // 37
                dummyField++; // 38
                dummyField++; // 39
                dummyField++; // 40
                dummyField++; // 41
                dummyField++; // 42
                dummyField++; // 43
                dummyField++; // 44
                dummyField++; // 45
                dummyField++; // 46
                dummyField++; // 47
                dummyField++; // 48
                dummyField++; // 49
                dummyField++; // 50
                dummyField++; // 51
                dummyField++; // 52
                dummyField++; // 53
                dummyField++; // 54
                dummyField++; // 55
                dummyField++; // 56
                dummyField++; // 57
                dummyField++; // 58
                dummyField++; // 59
                dummyField++; // 60
                dummyField++; // 61
                dummyField++; // 62
                dummyField++; // 63
            }
        }
    }
}