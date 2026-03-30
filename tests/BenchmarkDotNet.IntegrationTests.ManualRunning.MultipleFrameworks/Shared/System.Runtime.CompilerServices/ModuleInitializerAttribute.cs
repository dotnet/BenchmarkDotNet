#if NETFRAMEWORK
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ModuleInitializerAttribute : Attribute
{
    public ModuleInitializerAttribute()
    {
    }
}
#endif
