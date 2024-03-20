using System;
using System.Security.Cryptography;
using JsonSerializer = SimpleJson.SimpleJson;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.Snapshot.Stores
{
    internal static class HashUtility
    {
        public static string ToHash<T>([NotNull] this T source)
        {
            try
            {
                using var memory = new System.IO.MemoryStream(4000);
                using var streamWriter = new System.IO.StreamWriter(memory, System.Text.Encoding.UTF8);
                JsonSerializer.SerializeObject(streamWriter, source!);
                memory.Position = 0;
                using var hashCalulator = SHA256.Create();
                var bytes = hashCalulator.ComputeHash(memory);
                var result = Convert.ToBase64String(bytes);
                return result;
            }
            finally
            {

            }
        }



    }
}
