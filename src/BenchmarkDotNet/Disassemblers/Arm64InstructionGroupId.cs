namespace BenchmarkDotNet.Disassemblers;

// Based on Capstone's instruction grouping.
public enum Arm64InstructionGroupId
{
    /// <summary>
    ///     Indicates an invalid, or an uninitialized, instruction group.
    /// </summary>
    Invalid = 0,

    ARM64_GRP_JUMP,
    ARM64_GRP_CALL,
    ARM64_GRP_RET,
    ARM64_GRP_INT,
    ARM64_GRP_PRIVILEGE = 6,
    ARM64_GRP_BRANCH_RELATIVE,
    ARM64_GRP_CRYPTO = 128,
    ARM64_GRP_FPARMV8,
    ARM64_GRP_NEON,
    ARM64_GRP_CRC
}
