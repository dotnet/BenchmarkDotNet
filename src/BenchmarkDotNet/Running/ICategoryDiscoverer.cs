using System.Reflection;

namespace BenchmarkDotNet.Running
{
    public interface ICategoryDiscoverer
    {
        string[] GetCategories(MethodInfo method);
    }
}