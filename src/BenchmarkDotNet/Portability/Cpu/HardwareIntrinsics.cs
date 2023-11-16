using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BenchmarkDotNet.Environments;
using System.Text;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
#endif

namespace BenchmarkDotNet.Portability.Cpu
{
    internal static class HardwareIntrinsics
    {
        internal static string GetVectorSize() => Vector.IsHardwareAccelerated ? $"VectorSize={Vector<byte>.Count * 8}" : string.Empty;

        internal static string GetShortInfo()
        {
            if (IsX86Avx512FSupported)
                return GetShortAvx512Representation();
            if (IsX86Avx2Supported)
                return "AVX2";
            else if (IsX86AvxSupported)
                return "AVX";
            else if (IsX86Sse42Supported)
                return "SSE4.2";
            else if (IsX86Sse41Supported)
                return "SSE4.1";
            else if (IsX86Ssse3Supported)
                return "SSSE3";
            else if (IsX86Sse3Supported)
                return "SSE3";
            else if (IsX86Sse2Supported)
                return "SSE2";
            else if (IsX86SseSupported)
                return "SSE";
            else if (IsX86BaseSupported)
                return "X86Base";
            else if (IsArmAdvSimdSupported)
                return "AdvSIMD";
            else if (IsArmBaseSupported)
                return "ArmBase";
            else
                return GetVectorSize(); // Runtimes prior to .NET Core 3.0 (APIs did not exist so we print non-exact Vector info)
        }

        internal static string GetFullInfo(Platform platform)
        {
            return string.Join(",", GetCurrentProcessInstructionSets(platform));

            static IEnumerable<string> GetCurrentProcessInstructionSets(Platform platform)
            {
                switch (platform)
                {
                    case Platform.X86:
                    case Platform.X64:

                        if (IsX86Avx512FSupported) yield return GetShortAvx512Representation();
                        else if (IsX86Avx2Supported) yield return "AVX2";
                        else if (IsX86AvxSupported) yield return "AVX";
                        else if (IsX86Sse42Supported) yield return "SSE4.2";
                        else if (IsX86Sse41Supported) yield return "SSE4.1";
                        else if (IsX86Ssse3Supported) yield return "SSSE3";
                        else if (IsX86Sse3Supported) yield return "SSE3";
                        else if (IsX86Sse2Supported) yield return "SSE2";
                        else if (IsX86SseSupported) yield return "SSE";
                        else if (IsX86BaseSupported) yield return "X86Base";

                        if (IsX86AesSupported) yield return "AES";
                        if (IsX86Bmi1Supported) yield return "BMI1";
                        if (IsX86Bmi2Supported) yield return "BMI2";
                        if (IsX86FmaSupported) yield return "FMA";
                        if (IsX86LzcntSupported) yield return "LZCNT";
                        if (IsX86PclmulqdqSupported) yield return "PCLMUL";
                        if (IsX86PopcntSupported) yield return "POPCNT";
                        if (IsX86AvxVnniSupported) yield return "AvxVnni";
                        if (IsX86SerializeSupported) yield return "SERIALIZE";
                        // TODO: Add MOVBE when API is added.
                        break;
                    case Platform.Arm64:
                        if (IsArmAdvSimdSupported) yield return "AdvSIMD";
                        else if (IsArmBaseSupported) yield return "ArmBase";

                        if (IsArmAesSupported) yield return "AES";
                        if (IsArmCrc32Supported) yield return "CRC32";
                        if (IsArmDpSupported) yield return "DP";
                        if (IsArmRdmSupported) yield return "RDM";
                        if (IsArmSha1Supported) yield return "SHA1";
                        if (IsArmSha256Supported) yield return "SHA256";
                        break;
                    default:
                        yield break;
                }
            }
        }

        private static string GetShortAvx512Representation()
        {
            StringBuilder avx512 = new ("AVX-512F");
            if (IsX86Avx512CDSupported) avx512.Append("+CD");
            if (IsX86Avx512BWSupported) avx512.Append("+BW");
            if (IsX86Avx512DQSupported) avx512.Append("+DQ");
            if (IsX86Avx512FVLSupported) avx512.Append("+VL");
            if (IsX86Avx512VbmiSupported) avx512.Append("+VBMI");

            return avx512.ToString();
        }

        internal static bool IsX86BaseSupported =>
#if NET6_0_OR_GREATER
            X86Base.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.X86Base");
#endif

        internal static bool IsX86SseSupported =>
#if NET6_0_OR_GREATER
            Sse.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse");
#endif

        internal static bool IsX86Sse2Supported =>
#if NET6_0_OR_GREATER
            Sse2.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse2");
#endif

        internal static bool IsX86Sse3Supported =>
#if NET6_0_OR_GREATER
            Sse3.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse3");
#endif

        internal static bool IsX86Ssse3Supported =>
#if NET6_0_OR_GREATER
            Ssse3.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Ssse3");
#endif

        internal static bool IsX86Sse41Supported =>
#if NET6_0_OR_GREATER
            Sse41.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse41");
#endif

        internal static bool IsX86Sse42Supported =>
#if NET6_0_OR_GREATER
            Sse42.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse42");
#endif

        internal static bool IsX86AvxSupported =>
#if NET6_0_OR_GREATER
            Avx.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx");
#endif

        internal static bool IsX86Avx2Supported =>
#if NET6_0_OR_GREATER
            Avx2.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx2");
#endif

        internal static bool IsX86Avx512FSupported =>
#if NET8_0_OR_GREATER
            Avx512F.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512F");
#endif

        internal static bool IsX86Avx512FVLSupported =>
#if NET8_0_OR_GREATER
            Avx512F.VL.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512F+VL");
#endif

        internal static bool IsX86Avx512BWSupported =>
#if NET8_0_OR_GREATER
            Avx512BW.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512BW");
#endif

        internal static bool IsX86Avx512CDSupported =>
#if NET8_0_OR_GREATER
            Avx512CD.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512CD");
#endif

        internal static bool IsX86Avx512DQSupported =>
#if NET8_0_OR_GREATER
            Avx512DQ.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512DQ");
#endif

        internal static bool IsX86Avx512VbmiSupported =>
#if NET8_0_OR_GREATER
            Avx512Vbmi.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512Vbmi");
#endif

        internal static bool IsX86AesSupported =>
#if NET6_0_OR_GREATER
            System.Runtime.Intrinsics.X86.Aes.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Aes");
#endif

        internal static bool IsX86Bmi1Supported =>
#if NET6_0_OR_GREATER
            Bmi1.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Bmi1");
#endif

        internal static bool IsX86Bmi2Supported =>
#if NET6_0_OR_GREATER
            Bmi2.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Bmi2");
#endif

        internal static bool IsX86FmaSupported =>
#if NET6_0_OR_GREATER
            Fma.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Fma");
#endif

        internal static bool IsX86LzcntSupported =>
#if NET6_0_OR_GREATER
            Lzcnt.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Lzcnt");
#endif

        internal static bool IsX86PclmulqdqSupported =>
#if NET6_0_OR_GREATER
            Pclmulqdq.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Pclmulqdq");
#endif

        internal static bool IsX86PopcntSupported =>
#if NET6_0_OR_GREATER
            Popcnt.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Popcnt");
#endif

        internal static bool IsX86AvxVnniSupported =>
#if NET6_0_OR_GREATER
#pragma warning disable CA2252 // This API requires opting into preview features
            AvxVnni.IsSupported;
#pragma warning restore CA2252 // This API requires opting into preview features
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.AvxVnni");
#endif

        internal static bool IsX86SerializeSupported =>
#if NET7_0_OR_GREATER
            X86Serialize.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.X86Serialize");
#endif

        internal static bool IsArmBaseSupported =>
#if NET6_0_OR_GREATER
            ArmBase.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.ArmBase");
#endif

        internal static bool IsArmAdvSimdSupported =>
#if NET6_0_OR_GREATER
            AdvSimd.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.AdvSimd");
#endif

        internal static bool IsArmAesSupported =>
#if NET6_0_OR_GREATER
            System.Runtime.Intrinsics.Arm.Aes.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.Aes");
#endif

        internal static bool IsArmCrc32Supported =>
#if NET6_0_OR_GREATER
            Crc32.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.Crc32");
#endif

        internal static bool IsArmDpSupported =>
#if NET6_0_OR_GREATER
            Dp.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.Dp");
#endif

        internal static bool IsArmRdmSupported =>
#if NET6_0_OR_GREATER
            Rdm.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.Rdm");
#endif

        internal static bool IsArmSha1Supported =>
#if NET6_0_OR_GREATER
            Sha1.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.Sha1");
#endif

        internal static bool IsArmSha256Supported =>
#if NET6_0_OR_GREATER
            Sha256.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.Sha256");
#endif

        private static bool GetIsSupported([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null) return false;

            return (bool)type.GetProperty("IsSupported", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null, null);
        }
    }
}
