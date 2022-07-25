#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
#elif NETSTANDARD2_0_OR_GREATER
using System;
#endif

namespace BenchmarkDotNet.Portability.Cpu
{
    internal static class HardwareIntrinsics
    {
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

#if NETSTANDARD
        private static bool GetIsSupported(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null) return false;

            return (bool)type.GetProperty("IsSupported", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null, null);
        }
#endif
    }
}
