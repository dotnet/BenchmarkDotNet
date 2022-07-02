using Newtonsoft.Json;
using System;
using System.Security.Cryptography;

namespace BenchmarkDotNet.Toolchains.Snapshot.Stores
{
    internal static class HashUtility
    {
        private readonly static JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };
        public static string ToHash<T>(this T source)
        {

            try
            {

                using var memory = new System.IO.MemoryStream(4000);
                using var streamWriter = new System.IO.StreamWriter(memory, System.Text.Encoding.UTF8);
                var serializer = JsonSerializer.CreateDefault(settings);
                serializer.Serialize(streamWriter, source);
                memory.Position = 0;
                using var hashCalulator = SHA256.Create();
                var bytes = hashCalulator.ComputeHash(memory);
                return Convert.ToBase64String(bytes);

                throw new Exception("Can not serilizze");
            }
            finally
            {

            }
        }



    }
}
