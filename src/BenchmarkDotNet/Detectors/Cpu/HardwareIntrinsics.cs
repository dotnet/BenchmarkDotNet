using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using BenchmarkDotNet.Environments;
#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
#endif

#nullable enable

namespace BenchmarkDotNet.Detectors.Cpu
{
    // based on https://github.com/dotnet/runtime/tree/v10.0.0-rc.1.25451.107/src/coreclr/tools/Common/JitInterface/ThunkGenerator/InstructionSetDesc.txt
    internal static class HardwareIntrinsics
    {
        internal static string GetVectorSize() => Vector.IsHardwareAccelerated ? $"VectorSize={Vector<byte>.Count * 8}" : string.Empty;

        internal static string GetShortInfo()
        {
            if (IsX86BaseSupported)
            {
                if (IsX86Avx512Supported)
                {
                    return "x86-64-v4";
                }
                else if (IsX86Avx2Supported)
                {
                    return "x86-64-v3";
                }
                else if (IsX86Sse42Supported)
                {
                    return "x86-64-v2";
                }
                else
                {
                    return "x86-64-v1";
                }
            }
            else if (IsArmBaseSupported)
            {
                return "armv8.0-a";
            }
            else
            {
                return GetVectorSize(); // Runtimes prior to .NET Core 3.0 (APIs did not exist so we print non-exact Vector info)
            }
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
                    {
                        if (IsX86Avx10v2Supported) yield return "AVX10v2";
                        if (IsX86Avx10v1Supported)
                        {
                            yield return "AVX10v1";
                            yield return "AVX512 BF16+FP16";
                        }
                        if (IsX86Avx512v3Supported) yield return "AVX512 BITALG+VBMI2+VNNI+VPOPCNTDQ";
                        if (IsX86Avx512v2Supported) yield return "AVX512 IFMA+VBMI";
                        if (IsX86Avx512Supported) yield return "AVX512 F+BW+CD+DQ+VL";
                        if (IsX86Avx2Supported) yield return "AVX2+BMI1+BMI2+F16C+FMA+LZCNT+MOVBE";
                        if (IsX86AvxSupported) yield return "AVX";
                        if (IsX86Sse42Supported) yield return "SSE3+SSSE3+SSE4.1+SSE4.2+POPCNT";
                        if (IsX86BaseSupported) yield return "X86Base+SSE+SSE2";
                        if (IsX86AesSupported) yield return "AES+PCLMUL";
                        if (IsX86AvxVnniSupported) yield return "AvxVnni";
                        if (IsX86SerializeSupported) yield return "SERIALIZE";
                        break;
                    }
                    case Platform.Arm64:
                    {
                        if (IsArmBaseSupported)
                        {
                            yield return "ArmBase+AdvSimd";
                        }

                        if (IsArmAesSupported) yield return "AES";
                        if (IsArmCrc32Supported) yield return "CRC32";
                        if (IsArmDpSupported) yield return "DP";
                        if (IsArmRdmSupported) yield return "RDM";
                        if (IsArmSha1Supported) yield return "SHA1";
                        if (IsArmSha256Supported) yield return "SHA256";
                        break;
                    }

                    default:
                        yield break;
                }
            }
        }

#pragma warning disable CA2252 // Some APIs require opting into preview features
        internal static bool IsX86BaseSupported =>
#if NET6_0_OR_GREATER
            X86Base.IsSupported &&
            Sse.IsSupported &&
            Sse2.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.X86Base") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse2");
#endif

        internal static bool IsX86Sse42Supported =>
#if NET6_0_OR_GREATER
            Sse3.IsSupported &&
            Ssse3.IsSupported &&
            Sse41.IsSupported &&
            Sse42.IsSupported &&
            Popcnt.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse3") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Ssse3") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse41") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Sse42") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Popcnt");
#endif

        internal static bool IsX86AvxSupported =>
#if NET6_0_OR_GREATER
            Avx.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx");
#endif

        internal static bool IsX86Avx2Supported =>
#if NET6_0_OR_GREATER
            Avx2.IsSupported &&
            Bmi1.IsSupported &&
            Bmi2.IsSupported &&
            Fma.IsSupported &&
            Lzcnt.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx2") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Bmi1") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Bmi2") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Fma") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Lzcnt");
#endif

        internal static bool IsX86Avx512Supported =>
#if NET8_0_OR_GREATER
            Avx512F.IsSupported &&
            Avx512F.VL.IsSupported &&
            Avx512BW.IsSupported &&
            Avx512BW.VL.IsSupported &&
            Avx512CD.IsSupported &&
            Avx512CD.VL.IsSupported &&
            Avx512DQ.IsSupported &&
            Avx512DQ.VL.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512F") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512F+VL") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512BW") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512BW+VL") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512CD") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512CD+VL") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512DQ") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512DQ+VL");
#endif

        internal static bool IsX86Avx512v2Supported =>
#if NET8_0_OR_GREATER
            Avx512Vbmi.IsSupported &&
            Avx512Vbmi.VL.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512Vbmi") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL");
#endif

        internal static bool IsX86Avx512v3Supported =>
#if NET10_0_OR_GREATER
            Avx512Vbmi2.IsSupported &&
            Avx512Vbmi2.VL.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512Vbmi2") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL");
#endif

        internal static bool IsX86Avx10v1Supported =>
#if NET9_0_OR_GREATER
            Avx10v1.IsSupported &&
            Avx10v1.V512.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx10v1") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx10v1+V512");
#endif

        internal static bool IsX86Avx10v2Supported =>
#if NET10_0_OR_GREATER
            Avx10v2.IsSupported &&
            Avx10v2.V512.IsSupported;
#else
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx10v2") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Avx10v2+V512");
#endif

        internal static bool IsX86AesSupported =>
#if NET6_0_OR_GREATER
            System.Runtime.Intrinsics.X86.Aes.IsSupported &&
            Pclmulqdq.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.X86.Aes") &&
            GetIsSupported("System.Runtime.Intrinsics.X86.Pclmulqdq");
#endif

        internal static bool IsX86AvxVnniSupported =>
#if NET6_0_OR_GREATER
            AvxVnni.IsSupported;
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
            ArmBase.IsSupported &&
            AdvSimd.IsSupported;
#elif NETSTANDARD
            GetIsSupported("System.Runtime.Intrinsics.Arm.ArmBase") &&
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
#pragma warning restore CA2252 // Some APIs require opting into preview features

        private static bool GetIsSupported([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] string typeName)
        {
            Type type = Type.GetType(typeName)!;
            if (type == null) return false;

            return (bool)type.GetProperty("IsSupported", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!.GetValue(null, null)!;
        }
    }
}
