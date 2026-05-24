using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Validators;

public abstract class ExecutionValidatorBase : IValidator
{
    protected ExecutionValidatorBase(bool failOnError)
    {
        TreatsWarningsAsErrors = failOnError;
    }

    public bool TreatsWarningsAsErrors { get; }

    public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
        => ValidateAsyncCore(validationParameters);

    protected abstract IAsyncEnumerable<ValidationError> ValidateAsyncCore(ValidationParameters validationParameters, CancellationToken cancellationToken = default);

    protected bool TryCreateBenchmarkTypeInstance(Type type, List<ValidationError> errors, CancellationToken cancellationToken, [NotNullWhen(true)] out object? instance)
    {
        try
        {
            instance = Activator.CreateInstance(type)!;

            return true;
        }
        catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
        {
            errors.Add(new ValidationError(
                TreatsWarningsAsErrors,
                $"Unable to create instance of {type.Name}, exception was: {GetDisplayExceptionMessage(ex)}"));

            instance = null;
            return false;
        }
    }

    protected async ValueTask<bool> TryToCallSetupOrCleanup<T>(object benchmarkTypeInstance, MethodInfo? method, List<ValidationError> errors, CancellationToken cancellationToken)
    {
        if (method is null)
        {
            return true;
        }

        try
        {
            var result = method.Invoke(benchmarkTypeInstance, null);
            if (method.ReturnType.IsAwaitable(out var awaitableInfo))
            {
                if (result is null)
                {
                    errors.Add(new ValidationError(TreatsWarningsAsErrors, $"[{GetAttributeName(typeof(T))}] for {benchmarkTypeInstance.GetType().Name} returned null"));
                    return false;
                }
                await DynamicAwaitHelper.AwaitResult(result, awaitableInfo).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
        {
            errors.Add(new ValidationError(
                TreatsWarningsAsErrors,
                $"Failed to execute [{GetAttributeName(typeof(T))}] for {benchmarkTypeInstance.GetType().Name}, exception was {GetDisplayExceptionMessage(ex)}"));

            return false;
        }

        return true;
    }

    private string GetAttributeName(Type type) => type.Name.Replace("Attribute", string.Empty);

    protected bool TryFillParamsAndGetArgs(BenchmarkCase benchmark, object benchmarkTypeInstance, List<ValidationError> errors, out object?[]? args, CancellationToken cancellationToken)
    {
        if (!benchmark.HasParameters)
        {
            args = null;
            return true;
        }

        List<object?> argsList = [];
        foreach (var param in benchmark.Parameters.Items)
        {
            if (!param.IsArgument)
                continue;
            if (param.Definition.ParameterType.IsByRefLike())
            {
                errors.Add(new ValidationError(
                    TreatsWarningsAsErrors,
                    $"{GetType().Name} cannot execute benchmark with ref struct parameter {benchmark.Descriptor.Type.Name}.{benchmark.Descriptor.WorkloadMethodDisplayInfo}"));
                args = null;
                return false;
            }
            argsList.Add(param.Value);
        }
        try
        {
            Toolchains.InProcess.NoEmit.InProcessNoEmitRunner.FillMembers(benchmarkTypeInstance, benchmark, cancellationToken);
        }
        catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
        {
            errors.Add(new ValidationError(
                TreatsWarningsAsErrors,
                $"Failed to set parameters for {benchmark.Descriptor.Type.Name}, exception was: {GetDisplayExceptionMessage(ex)}"));
            args = null;
            return false;
        }
        args = argsList.Count > 0 ? [.. argsList] : null;
        return true;
    }

    protected static string GetDisplayExceptionMessage(Exception ex)
    {
        if (ex is TargetInvocationException targetInvocationException)
            ex = targetInvocationException.InnerException!;

        return ex?.Message ?? "Unknown error";
    }
}