#if UAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.Reflection
{
    // Do not rename this to IntrospectionExtensions. 
    // See https://github.com/scottksmith95/LINQKit/issues/47#issuecomment-249734435
    public static class IntrospectionExtensionsEx
    {
        public static FieldInfo[] GetFields(this TypeInfo typeInfo, BindingFlags bindingFlags)
        {
            return typeInfo.DeclaredFields.ToArray();
        }

        public static PropertyInfo[] GetProperties(this TypeInfo typeInfo, BindingFlags bindingFlags)
        {
            return typeInfo.DeclaredProperties.ToArray();
        }

        public static bool IsInstanceOfType(this TypeInfo typeInfo, object value)
        {
            return typeInfo.AsType().IsInstanceOfType(value);
        }

        public static Type[] GetGenericArguments(this TypeInfo typeInfo)
        {
            return typeInfo.GenericTypeArguments;
        }

        public static MethodInfo GetMethod(this TypeInfo typeInfo, string methodName, BindingFlags bindingFlags)
        {
            return typeInfo.GetDeclaredMethod(methodName);
        }

        public static FieldInfo GetField(this TypeInfo typeInfo, string fieldName, BindingFlags bindingFlags)
        {
            return typeInfo.GetDeclaredField(fieldName);
        }

        public static MethodInfo GetMethod(this TypeInfo typeInfo, string methodName)
        {
            return typeInfo.GetDeclaredMethod(methodName);
        }
    }
}
#endif