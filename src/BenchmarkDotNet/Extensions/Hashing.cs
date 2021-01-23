// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// file copied from https://github.com/dotnet/machinelearning/blob/b31bdee6671bfe50460f9279609dd948f8ce081c/src/Microsoft.ML.Core/Utilities/Hashing.cs

using System;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Extensions
{
    internal static class Hashing
    {
        private const uint _defaultSeed = (5381 << 16) + 5381;

        internal static uint HashString(string str) => MurmurHash(_defaultSeed, str.AsSpan());

        /// <summary>
        /// Implements the murmur hash 3 algorithm, using a mock UTF-8 encoding.
        /// The UTF-8 conversion ignores the possibilities of unicode planes other than the 0th.
        /// That is, it simply converts char values to one, two, or three bytes according to
        /// the following rules:
        /// * 0x0000 to 0x007F : 0xxxxxxx
        /// * 0x0080 to 0x07FF : 110xxxxx 10xxxxxx
        /// * 0x0800 to 0xFFFF : 1110xxxx 10xxxxxx 10xxxxxx
        /// NOTE: This MUST match the StringBuilder version below.
        /// </summary>
        private static uint MurmurHash(uint hash, ReadOnlySpan<char> span, bool toUpper = false)
        {
            // Byte length (in pseudo UTF-8 form).
            int len = 0;

            // Current bits, value and count.
            ulong cur = 0;
            int bits = 0;
            for (int ich = 0; ich < span.Length; ich++)
            {
                uint ch = toUpper ? char.ToUpperInvariant(span[ich]) : span[ich];
                if (ch <= 0x007F)
                {
                    cur |= ch << bits;
                    bits += 8;
                }
                else if (ch <= 0x07FF)
                {
                    cur |= (ulong)((ch & 0x003F) | ((ch << 2) & 0x1F00) | 0xC080) << bits;
                    bits += 16;
                }
                else
                {
                    cur |= (ulong)((ch & 0x003F) | ((ch << 2) & 0x3F00) | ((ch << 4) & 0x0F0000) | 0xE08080) << bits;
                    bits += 24;
                }

                if (bits >= 32)
                {
                    hash = MurmurRound(hash, (uint)cur);
                    cur = cur >> 32;
                    bits -= 32;
                    len += 4;
                }
            }

            if (bits > 0)
            {
                hash = MurmurRound(hash, (uint)cur);
                len += bits / 8;
            }

            // Encode the length.
            hash = MurmurRound(hash, (uint)len);

            // Final mixing ritual for the hash.
            hash = MixHash(hash);

            return hash;
        }

        /// <summary>
        /// Combines the given hash value with a uint value, using the murmur hash 3 algorithm.
        /// Make certain to also use <see cref="MixHash"/> on the final hashed value, if you
        /// depend upon having distinct bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MurmurRound(uint hash, uint chunk)
        {
            chunk *= 0xCC9E2D51;
            chunk = Rotate(chunk, 15);
            chunk *= 0x1B873593;

            hash ^= chunk;
            hash = Rotate(hash, 13);
            hash *= 5;
            hash += 0xE6546B64;

            return hash;
        }

        /// <summary>
        /// The final mixing ritual for the Murmur3 hashing algorithm. Most users of
        /// <see cref="MurmurRound"/> will want to close their progressive building of
        /// a hash with a call to this method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MixHash(uint hash)
        {
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rotate(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}
