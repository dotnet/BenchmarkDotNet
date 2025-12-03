using System;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Diagnosers;

/// <summary>
/// The data used to construct the <see cref="IInProcessDiagnoserHandler"/> in the benchmark process.
/// </summary>
/// <param name="handlerType">The type of the <see cref="IInProcessDiagnoserHandler"/>.</param>
/// <param name="serializedConfig">The serialized config that will be passed to <see cref="IInProcessDiagnoserHandler.Initialize(string?)"/>.</param>
public readonly struct InProcessDiagnoserHandlerData(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? handlerType,
    string? serializedConfig)
{
    /// <summary>
    /// The type of the <see cref="IInProcessDiagnoserHandler"/>. If <see langword="null"/>, the handler will not be created.
    /// </summary>
    /// <remarks>
    /// The type must implement <see cref="IInProcessDiagnoserHandler"/> and have a publicly accessible default constructor.
    /// <para/>
    /// </remarks>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? HandlerType { get; } = handlerType;

    /// <summary>
    /// The serialized config that will be passed to <see cref="IInProcessDiagnoserHandler.Initialize(string?)"/>.
    /// </summary>
    public string? SerializedConfig { get; } = serializedConfig;
}