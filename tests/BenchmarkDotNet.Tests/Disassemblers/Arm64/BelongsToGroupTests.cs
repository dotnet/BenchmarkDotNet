using AsmArm64;
using AwesomeAssertions;
using BenchmarkDotNet.Disassemblers;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;
using static AsmArm64.Arm64InstructionFactory;
using static AsmArm64.Arm64ConditionalKind;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class BelongsToGroupTests
{
    private readonly ITestOutputHelper Output;

    public BelongsToGroupTests(ITestOutputHelper output)
    {
        Output = output;
    }

    [Theory]
    [MemberData(nameof(TestData.JumpInstructions), MemberType = typeof(TestData))]
    public void BelongToGroup_Jump(uint rawInstruction)
    {
        // Arrange
        var groupId = Arm64InstructionGroupId.ARM64_GRP_JUMP;
        var instruction = Arm64Instruction.Decode(rawInstruction);

        // Act
        var result = instruction.BelongsToGroup(groupId);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(TestData.CallInstructions), MemberType = typeof(TestData))]
    public void BelongToGroup_Call(uint rawInstruction)
    {
        // Arrange
        var groupId = Arm64InstructionGroupId.ARM64_GRP_CALL;
        var instruction = Arm64Instruction.Decode(rawInstruction);

        // Act
        var result = instruction.BelongsToGroup(groupId);

        // Assert
        result.Should().BeTrue();
    }


    [Theory]
    [MemberData(nameof(TestData.BranchRelativeInstructions), MemberType = typeof(TestData))]
    public void BelongToGroup_BranchRelative(uint rawInstruction)
    {
        // Arrange
        var groupId = Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE;
        var instruction = Arm64Instruction.Decode(rawInstruction);

        // Act
        var result = instruction.BelongsToGroup(groupId);

        // Assert
        result.Should().BeTrue();
    }

    public static class TestData
    {
        // Following instructions is based on
        // Capstone's group mapping at https://github.com/capstone-engine/capstone/blob/6.0.0-Alpha10/arch/AArch64/AArch64GenCSMappingInsn.inc
        //
        // Gee.External.Capstone internally use v4.0.2 mappings.
        // https://github.com/capstone-engine/capstone/blob/4.0.2/arch/AArch64/AArch64MappingInsn.inc
        public static readonly TheoryData<uint> JumpInstructions =
        [
            B(0x100),                   // B_only_branch_imm:
            B(EQ, 0x100),               // B_only_condbranch:
            BC(EQ, 0x100),              // BC_only_condbranch: // It's introduced at Armv9.2-A
            BR(X0),                     // BR_64_branch_reg:
            BRAA(X0, X1),               // BRAA_64p_branch_reg:
            BRAAZ(X0),                  // BRAAZ_64_branch_reg:
            BRAB(X0, X1),               // BRAB_64p_branch_reg:
            BRABZ(X0),                  // BRABZ_64_branch_reg:
            CBNZ(W0, 0x100),            // CBNZ_32_compbranch:
            CBNZ(X0, 0x100),            // CBNZ_64_compbranch:
            CBZ(W0, 0x100),             // CBZ_32_compbranch:
            CBZ(X0, 0x100),             // CBZ_64_compbranch:
            DRPS(),                     // DRPS_64e_branch_reg:
            ERET(),                     // ERET_64e_branch_reg:
            ERETAA(),                   // ERETAA_64e_branch_reg:
            ERETAB(),                   // ERETAB_64e_branch_reg:
            RET(X0),                    // RET_64r_branch_reg:
            RETAA(),                    // RETAA_64e_branch_reg:
            RETAASPPCR(X0),             // RETAASPPCR_64m_branch_reg:
            RETAASPPC(-0x100),          // RETAASPPC_only_miscbranch:
            RETAB(),                    // RETAB_64e_branch_reg:
            RETABSPPCR(X0),             // RETABSPPCR_64m_branch_reg:
            RETABSPPC(-0x100),          // RETABSPPC_only_miscbranch:
            TBNZ(X0, imm: 0x10, 0x100), // TBNZ_only_testbranch:
            TBZ(X0, imm: 0x10, 0x100),  // TBZ_only_testbranch:
        ];

        public static readonly TheoryData<uint> CallInstructions =
        [
            BL(0x100),     // BL_only_branch_imm
            BLR(X0),       // BLR_64_branch_reg
            BLRAA(X0, X1), // BLRAA_64p_branch_reg
            BLRAB(X0, X1), // BLRAB_64p_branch_reg
            BLRAAZ(X0),    // BLRAAZ_64_branch_reg
            BLRABZ(X0),    // BLRABZ_64_branch_reg
            HVC(0x100),    // HVC_ex_exception
            SMC(0x100),    // HVC_ex_exception
            SVC(0x100),    // SVC_ex_exception
        ];

        public static readonly TheoryData<uint> BranchRelativeInstructions =
        [
            B(0x100),                       // B_only_branch_imm
            B(EQ, 0x100),                   // B_only_condbranch
            BC(NE, 0x100),                  // BC_only_condbranch
            BL(0x100),                      // BL_only_branch_imm
            CBNZ(W0,0x100),                 // CBNZ_32_compbranch
            CBNZ(X0,0x100),                 // CBNZ_64_compbranch
            CBZ(W0,0x100),                  // CBZ_32_compbranch
            CBZ(X0,0x100),                  // CBZ_64_compbranch
            RETAASPPC(-0x100),              // RETAASPPC_only_miscbranch
            RETABSPPC(-0x100),              // RETABSPPC_only_miscbranch
            TBNZ(X0,imm:0x10, label: 0x20), // TBNZ_only_testbranch
            TBZ(X0,imm:0x10, label: 0x20),  // TBZ_only_testbranch
        ];
    }
}
