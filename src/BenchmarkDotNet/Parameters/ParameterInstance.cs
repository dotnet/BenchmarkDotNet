﻿using System;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstance
    {
        public const string NullParameterTextRepresentation = "?";
        private const int MaxDisplayTextInnerLength = 15 + 5; // 5 is for postfix " [15]"

        [PublicAPI] public ParameterDefinition Definition { get; }

        private readonly object value;

        public ParameterInstance(ParameterDefinition definition, object value)
        {
            Definition = definition;
            this.value = value;
        }

        public string Name => Definition.Name;
        public bool IsStatic => Definition.IsStatic;
        public bool IsArgument => Definition.IsArgument;

        public object Value => value is IParam parameter ? parameter.Value : value;

        public string ToSourceCode()
            => value is IParam parameter
                ? parameter.ToSourceCode()
                : SourceCodeHelper.ToSourceCode(value);

        public string ToDisplayText()
        {
            switch (value) {
                case null:
                    return NullParameterTextRepresentation;
                case IParam parameter:
                    return Trim(parameter.DisplayText);
                // no trimming for types!
                case Type type:
                    return type.IsNullable() ? $"{Nullable.GetUnderlyingType(type).GetDisplayName()}?" : type.GetDisplayName();
            }

            return Trim(value.ToString());
        }

        public override string ToString() => ToDisplayText();

        private static string Trim(string value)
            => value.Length <= MaxDisplayTextInnerLength
                ? value
                : value.Substring(0, 5) + "(...)" + value.Substring(value.Length - 5, 5) + $" [{value.Length}]";
    }
}