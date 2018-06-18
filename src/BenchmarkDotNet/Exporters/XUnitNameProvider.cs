using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Exporters
{
    internal class XUnitNameProvider 
    {
        internal static string GetBenchmarkName(BenchmarkCase benchmarkCase)
        {
            var type = benchmarkCase.Target.Type;
            var method = benchmarkCase.Target.Method;

            // we can't just use type.FullName because we need sth different for generics (it reports SimpleGeneric`1[[System.Int32, mscorlib, Version=4.0.0.0)
            var name = new StringBuilder();

            if (!string.IsNullOrEmpty(type.Namespace))
                name.Append(type.Namespace).Append('.');

            name.Append(GetNestedTypes(type));

            name.Append(GetTypeName(type)).Append('.');

            name.Append(method.Name);

            if (benchmarkCase.HasParameters)
                name.Append(GetBenchmarkParameters(method, benchmarkCase.Parameters));

            return name.ToString();
        }

        private static string GetNestedTypes(Type type)
        {
            var nestedTypes = "";
            Type child = type, parent = type.DeclaringType;
            while (child.IsNested && parent != null)
            {
                nestedTypes = parent.Name + "+" + nestedTypes;

                child = parent;
                parent = parent.DeclaringType;
            }

            return nestedTypes;
        }

        private static string GetTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            var mainName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var args = string.Join(", ", type.GetGenericArguments().Select(GetTypeName).ToArray());

            return $"{mainName}<{args}>";
        }

        private static string GetBenchmarkParameters(MethodInfo method, ParameterInstances benchmarkParameters)
        {
            var methodParameters = method.GetParameters();
            var parametersBuilder = new StringBuilder(methodParameters.Length * 20).Append('(');

            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i > 0)
                    parametersBuilder.Append(", ");

                parametersBuilder.Append(methodParameters[i].Name).Append(':').Append(' ');
                parametersBuilder.Append(GetArgument(benchmarkParameters.GetArgument(methodParameters[i].Name).Value, methodParameters[i].ParameterType));
            }

            foreach (var parameter in benchmarkParameters.Items.Where(parameter => !parameter.IsArgument))
            {
                if (methodParameters.Length > 0)
                    parametersBuilder.Append(", ");
                
                parametersBuilder.Append(parameter.Name).Append(':').Append(' ');
                parametersBuilder.Append(GetArgument(parameter.Value, parameter.Value?.GetType()));
            }

            return parametersBuilder.Append(')').ToString();
        }

        private static string GetArgument(object argumentValue, Type argumentType)
        {
            if (argumentValue == null)
                return "null";

            if (argumentValue is IParam iparam)
                return GetArgument(iparam.Value, argumentType);
            
            if (argumentValue is object[] array && array.Length == 1)
                return GetArgument(array[0], argumentType);

            if (argumentValue is string text)
                return $"\"{EscapeWhitespaces(text)}\"";
            if (argumentValue is char character)
                return $"'{character}'";
            if (argumentValue is DateTime time)
                return time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK");

            if (argumentType != null && argumentType.IsArray)
                return GetArray((IEnumerable)argumentValue);

            return argumentValue.ToString();
        }

        // it's not generic so I can't simply use .Skip and all other LINQ goodness
        private static string GetArray(IEnumerable collection)
        {
            var buffer = new StringBuilder().Append('[');

            int index = 0;
            foreach (var item in collection)
            {
                if (index > 0)
                    buffer.Append(", ");

                if (index > 4)
                {
                    buffer.Append("..."); // [0, 1, 2, 3, 4, ...]
                    break;
                }

                buffer.Append(GetArgument(item, item?.GetType()));

                ++index;
            }

            buffer.Append(']');

            return buffer.ToString();
        }

        private static string EscapeWhitespaces(string text)
            => text.Replace("\t", "\\t")
                   .Replace("\r\n", "\\r\\n");
    }
}
