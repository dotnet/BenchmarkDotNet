using System;
using System.Reflection;

namespace BenchmarkDotNet.Helpers.Reflection.Emit
{
    internal class EmitParameterInfo : ParameterInfo
    {
        public static ParameterInfo CreateReturnVoidParameter()
        {
            return CreateReturnParameter(typeof(void), ParameterAttributes.None);
        }

        public static ParameterInfo CreateReturnParameter(Type parameterType)
        {
            return CreateReturnParameter(parameterType, ParameterAttributes.None);
        }

        public static ParameterInfo CreateReturnParameter(Type parameterType, ParameterAttributes parameterAttributes)
        {
            return new EmitParameterInfo(-1, null, parameterType, parameterAttributes, null);
        }

        public EmitParameterInfo(int position, string name, Type parameterType)
            : this(position, name, parameterType, ParameterAttributes.None, null)
        {
        }

        public EmitParameterInfo(
            int position,
            string name,
            Type parameterType,
            ParameterAttributes parameterAttributes,
            MemberInfo member)
        {
            PositionImpl = position;
            NameImpl = name;
            ClassImpl = parameterType;
            AttrsImpl = parameterAttributes;
            MemberImpl = member;
        }

        public ParameterInfo SetMember(MemberInfo member)
        {
            MemberImpl = member;
            return this;
        }
    }
}