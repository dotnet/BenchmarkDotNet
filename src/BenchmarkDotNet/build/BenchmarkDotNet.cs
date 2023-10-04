using BenchmarkDotNet.Running;
using System.Reflection;

public class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(Assembly.GetEntryAssembly()).Run(args);
}
