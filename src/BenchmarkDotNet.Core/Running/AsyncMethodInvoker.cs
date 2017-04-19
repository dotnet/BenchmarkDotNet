using System.Threading.Tasks;
using HideFromIntelliSense = System.ComponentModel.EditorBrowsableAttribute; // we don't want people to use it

namespace BenchmarkDotNet.Running
{
    // if you want to rename any of these methods you need to update DeclarationsProvider's code as well
    // ReSharper disable MemberCanBePrivate.Global
    public static class TaskMethodInvoker
    {
        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Idle() => Task.CompletedTask.GetAwaiter().GetResult();
    }

    public static class TaskMethodInvoker<T>
    {
        private static readonly Task<T> Completed = Task.FromResult(default(T));

        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static T Idle() => Completed.GetAwaiter().GetResult();
    }

    public static class ValueTaskMethodInvoker<T>
    {
        private static readonly ValueTask<T> Completed = new ValueTask<T>(default(T));

        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static T Idle() => Completed.GetAwaiter().GetResult();
    }
    // ReSharper restore MemberCanBePrivate.Global
}