using System;
using System.IO;

namespace BenchmarkDotNet.Achievements
{
    public class LocalStateStorage
    {
        static readonly string rootDirectory =
            #if NETCOREAPP2_0 || CLASSIC
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            #else
                Path.GetFullPath(".");
            #endif
        static readonly string benchmarkDotNetDirectory = Path.Combine(rootDirectory, ".benchmarkDotNet");

        static string GetFileName(string key)
        {
            Directory.CreateDirectory(benchmarkDotNetDirectory);
            return Path.Combine(benchmarkDotNetDirectory, key + ".json");
        }

        public static void SaveSomething<T>(string key, T value)
        {
            var serializedValue = SimpleJson.SimpleJson.SerializeObject(value);

            File.WriteAllText(GetFileName(key), serializedValue);
        }

        public static T ReadSomething<T>(string key)
        {
            var fileName = GetFileName(key);
            if (File.Exists(fileName))
            {
                var serializedValue = File.ReadAllText(fileName);
                return SimpleJson.SimpleJson.DeserializeObject<T>(serializedValue);
            }
            else return default;
        }

        public static void ModifySomething<T>(string key, Func<T, T> modify)
        {
            SaveSomething(key, modify(ReadSomething<T>(key)));
        }
    }
}