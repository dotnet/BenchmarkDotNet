namespace BenchmarkDotNet.Toolchains
{
    public class Toolchain : IToolchain
    {
        public static readonly IToolchain Current
#if CLASSIC
            = Classic.ClassicToolchain.Instance; 
#elif DNX
            = Dnx.DnxToolchain.Instance; 
#elif CORE  
            = Core.CoreToolchain.Instance; 
#endif

        public string Name { get; }
        public IGenerator Generator { get; }
        public IBuilder Builder { get; }
        public IExecutor Executor { get; }

        public Toolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor)
        {
            Name = name;
            Generator = generator;
            Builder = builder;
            Executor = executor;
        }

        public override string ToString() => Name;
    }
}