using System.Diagnostics;

namespace BenchmarkDotNet.Diagnosers
{
    /// <summary>
    /// Energy counter which reads from /sys/class/powercap/intel-rapl/**
    /// </summary>
    internal class LinuxEnergyCounter : EnergyCounter
    {
        private readonly string _energyPath;
        private long _start;
        private long _finish;

        public LinuxEnergyCounter(string energyPath, string name, string id) : base(name, id)
        {
            _energyPath = !string.IsNullOrEmpty(energyPath) ? energyPath : throw new ArgumentException(nameof(energyPath));
        }

        public override (bool, string) TestRead()
        {
            try
            {
                string fileContents = File.ReadAllText(_energyPath);
                bool good = long.Parse(fileContents) > 0;
                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        public override void FixStart()
        {
            try
            {
                _start = long.Parse(File.ReadAllText(_energyPath));
            }
            catch { }
        }

        public override void FixFinish()
        {
            try
            {
                _finish = long.Parse(File.ReadAllText(_energyPath));
            }
            catch { }
        }

        public override long GetValue()
        {
            return _start > 0 && _finish > 0 ? (_finish - _start) : 0;
        }

        public static bool IsValid(string path)
        {
            return File.Exists(Path.Combine(path, "name")) && File.Exists(Path.Combine(path, "energy_uj"));
        }

        public static LinuxEnergyCounter FromPath(string path)
        {
            string name = File.ReadAllText(Path.Combine(path, "name")).Trim();
            Debug.Assert(!string.IsNullOrEmpty(name));
            return new LinuxEnergyCounter($"{path}/energy_uj", name, path);
        }
    }
}
