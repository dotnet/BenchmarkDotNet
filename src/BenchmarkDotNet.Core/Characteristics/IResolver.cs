namespace BenchmarkDotNet.Characteristics
{
    /// <summary>
    /// An entity which can resolve default values of <see cref="Characteristic{T}"/>.
    /// </summary>
    public interface IResolver
    {
        bool CanResolve(Characteristic characteristic);

        object Resolve(JobMode jobMode, Characteristic characteristic);

        T Resolve<T>(JobMode jobMode, Characteristic<T> characteristic);
    }
}