﻿namespace BenchmarkDotNet.Characteristics
{
    /// <summary>
    /// An entity which can resolve default values of <see cref="Characteristic{T}"/>.
    /// </summary>
    public interface IResolver
    {
        bool CanResolve(Characteristic characteristic);

        object Resolve(CharacteristicObject obj, Characteristic characteristic);

        T Resolve<T>(CharacteristicObject obj, Characteristic<T> characteristic);
    }
}