namespace BenchmarkDotNet.Characteristics
{
    /// <summary>
    /// An entity which can resolve default values of <see cref="Characteristic{T}"/>.
    /// </summary>
    public interface IResolver
    {
        T Resolve<T>(ICharacteristic<T> characteristic);

        bool CanResolve(ICharacteristic characteristic);
        object Resolve(ICharacteristic characteristic);
    }
}