using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Characteristics
{
    /// <summary>
    /// An entity which can resolve default values of <see cref="Characteristic{T}"/>.
    /// </summary>
    public interface IResolver
    {
        bool CanResolve(Characteristic characteristic);

        object Resolve(CharacteristicObject obj, Characteristic characteristic);

        T Resolve<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(CharacteristicObject obj, Characteristic<T> characteristic);

        object Resolve(CharacteristicObject obj, Characteristic characteristic, object defaultValue);

        T Resolve<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(CharacteristicObject obj, Characteristic<T> characteristic, T defaultValue);
    }
}