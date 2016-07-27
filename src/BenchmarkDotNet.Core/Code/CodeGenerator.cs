using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Code
{
    internal static class CodeGenerator
    {
        internal static string Generate(Benchmark benchmark)
        {
            var declarationsProvider = GetDeclarationsProvider(benchmark.Target);

            return new StringBuilder(ResourceHelper.LoadTemplate("BenchmarkProgram.txt"))
               .Replace("$OperationsPerInvoke$", declarationsProvider.OperationsPerInvoke)
               .Replace("$TargetTypeNamespace$", declarationsProvider.TargetTypeNamespace)
               .Replace("$TargetMethodReturnTypeNamespace$", declarationsProvider.TargetMethodReturnTypeNamespace)
               .Replace("$TargetTypeName$", declarationsProvider.TargetTypeName)
               .Replace("$TargetMethodDelegate$", declarationsProvider.TargetMethodDelegate)
               .Replace("$TargetMethodResultHolder$", declarationsProvider.TargetMethodResultHolder)
               .Replace("$TargetMethodDelegateType$", declarationsProvider.TargetMethodDelegateType)
               .Replace("$TargetMethodHoldValue$", declarationsProvider.TargetMethodHoldValue)
               .Replace("$TargetMethodReturnType$", declarationsProvider.TargetMethodReturnType)
               .Replace("$IdleMethodDelegateType$", declarationsProvider.IdleMethodDelegateType)
               .Replace("$IdleMethodReturnType$", declarationsProvider.IdleMethodReturnType)
               .Replace("$SetupMethodName$", declarationsProvider.SetupMethodName)
               .Replace("$CleanupMethodName$", declarationsProvider.CleanupMethodName)
               .Replace("$IdleImplementation$", declarationsProvider.IdleImplementation)
               .Replace("$AdditionalLogic$", benchmark.Target.AdditionalLogic)
               .Replace("$TargetBenchmarkTaskArguments$", benchmark.Job.GenerateWithDefinitions())
               .Replace("$ParamsContent$", GetParamsContent(benchmark))
               .ToString();
        }

        private static DeclarationsProvider GetDeclarationsProvider(Target target)
        {
            var method = target.Method;

            if (method.ReturnType == typeof(Task))
            {
                return new TaskDeclarationsProvider(target);
            }
            if (method.ReturnType.GetTypeInfo().IsGenericType 
                && method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>))
            {
                return new GenericTaskDeclarationsProvider(target, typeof(TaskMethodInvoker<>));
            }
            if (method.ReturnType.GetTypeInfo().IsGenericType 
                && method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return new GenericTaskDeclarationsProvider(target, typeof(ValueTaskMethodInvoker<>));
            }

            if (method.ReturnType == typeof(void))
            {
                var isUsingAsyncKeyword = method.HasAttribute<AsyncStateMachineAttribute>();
                if (isUsingAsyncKeyword)
                {
                    throw new NotSupportedException("async void is not supported by design");
                }

                return new VoidDeclarationsProvider(target);
            }
            return new NonVoidDeclarationsProvider(target);
        }

        private static string GetParamsContent(Benchmark benchmark)
        {
            return string.Join(
                string.Empty,
                benchmark.Parameters.Items.Select(
                    parameter =>
                        $"{(parameter.IsStatic ? "" : "instance.")}{parameter.Name} = {GetParameterValue(parameter.Value)};"));
        }

        private static string GetParameterValue(object value)
        {
            if (value is bool)
                return value.ToString().ToLower();
            if (value is string)
                return $"\"{value}\"";
            if (value is char)
                return $"'{value}'";
            if (value is float)
                return ((float)value).ToString("G", CultureInfo.InvariantCulture) + "f";
            if (value is double)
                return ((double)value).ToString("G", CultureInfo.InvariantCulture) + "d";
            if (value is decimal)
                return ((decimal)value).ToString("G", CultureInfo.InvariantCulture) + "m";
            if (value.GetType().GetTypeInfo().IsEnum)
                return value.GetType().GetCorrectTypeName() + "." + value;
            if (value is Type)
                return "typeof(" + ((Type)value).GetCorrectTypeName() + ")";
            return value.ToString();
        }
    }
}