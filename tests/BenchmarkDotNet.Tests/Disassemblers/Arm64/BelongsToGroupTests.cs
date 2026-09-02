// TODO: Remove #if directive when migrated to xunit.v3 or migrated to AsmArm64 based implementation.
#if NET
using AsmArm64;
using AwesomeAssertions;
using Gee.External.Capstone.Arm64;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class BelongsToGroupTests
{
    private readonly ITestOutputHelper Output;

    public BelongsToGroupTests(ITestOutputHelper output)
    {
        Output = output;
    }

    [Theory]
    [MemberData(nameof(TestData.Jump), MemberType = typeof(TestData))]
    public void BelongToGroup_Jump(InstructionTestData testData)
    {
        // Arrange
        var groupId = Arm64InstructionGroupId.ARM64_GRP_JUMP;
        var instruction = testData.GetCapstoneInstruction();

        // Act
        var result = instruction.Details.BelongsToGroup(groupId);

        // Assert
        result.Should().BeTrue();
    }

    // It looks like underlying capstone version (v4.0.2) don't contains following PR changes.
    // So It returns always false.
    // https://github.com/capstone-engine/capstone/pull/1610
    [Theory(Skip = "Incomaptible with capstone v4.0.2")]
    [MemberData(nameof(TestData.Call), MemberType = typeof(TestData))]
    public void BelongToGroup_Call(InstructionTestData testData)
    {
        // Arrange
        var groupId = Arm64InstructionGroupId.ARM64_GRP_CALL;
        var instruction = testData.GetCapstoneInstruction();

        // Act
        var result = instruction.Details.BelongsToGroup(groupId);

        // Assert
        result.Should().BeTrue();
    }


    [Theory]
    [MemberData(nameof(TestData.BranchRelative), MemberType = typeof(TestData))]
    public void BelongToGroup_BranchRelative(InstructionTestData testData)
    {
        // Arrange
        var groupId = Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE;
        var instruction = testData.GetCapstoneInstruction();

        // Act
        var result = instruction.Details.BelongsToGroup(groupId);

        // Assert
        result.Should().BeTrue();
    }


    public record struct InstructionTestData(
        Arm64Mnemonic Nemonic,
        AsmArm64.Arm64InstructionId InstructionId,
        string Text,
        uint RawInstruction
    )
    {
        public AsmArm64.Arm64Instruction GetInstruction() => AsmArm64.Arm64Instruction.Decode(RawInstruction);

        public Gee.External.Capstone.Arm64.Arm64Instruction GetCapstoneInstruction() => RawInstruction.ToCapstoneArm64Instruction();
    };


    public static class TestData
    {
        public static TheoryData<InstructionTestData> Jump
            => new(JumpInstructions.Select(ToInstructionTestData));

        public static TheoryData<InstructionTestData> Call
            => new(CallInstructions.Select(ToInstructionTestData));

        public static TheoryData<InstructionTestData> BranchRelative
            => new(BranchRelativeInstructions.Select(ToInstructionTestData));

        // Following instructions is based on
        // Capstone's group mapping at https://github.com/capstone-engine/capstone/blob/6.0.0-Alpha10/arch/AArch64/AArch64GenCSMappingInsn.inc
        //
        // Gee.External.Capstone internally use v4.0.2 mappings.
        // https://github.com/capstone-engine/capstone/blob/4.0.2/arch/AArch64/AArch64MappingInsn.inc

        // TODO: Some instructions are commented out. Because it's not supported by current Capstone version and failed to decode.

        private static readonly uint[] JumpInstructions =
        [
            Arm64InstructionFactory.B(0x100),                              // B_only_branch_imm:
            Arm64InstructionFactory.B(Arm64ConditionalKind.EQ, 0x100),     // B_only_condbranch:
            // Arm64InstructionFactory.BC(Arm64ConditionalKind.EQ, 0x100), // BC_only_condbranch: // It's introduced at Armv9.2-A
            Arm64InstructionFactory.BR(X0),                                // BR_64_branch_reg:
            // Arm64InstructionFactory.BRAA(X0, X1),                       // BRAA_64p_branch_reg:
            // Arm64InstructionFactory.BRAAZ(X0),                          // BRAAZ_64_branch_reg:
            // Arm64InstructionFactory.BRAB(X0, X1),                       // BRAB_64p_branch_reg:
            // Arm64InstructionFactory.BRABZ(X0),                          // BRABZ_64_branch_reg:
            Arm64InstructionFactory.CBNZ(W0, 0x100),                       // CBNZ_32_compbranch:
            Arm64InstructionFactory.CBNZ(X0, 0x100),                       // CBNZ_64_compbranch:
            Arm64InstructionFactory.CBZ(W0, 0x100),                        // CBZ_32_compbranch:
            Arm64InstructionFactory.CBZ(X0, 0x100),                        // CBZ_64_compbranch:
            // Arm64InstructionFactory.DRPS(),                             // DRPS_64e_branch_reg:
            // Arm64InstructionFactory.ERET(),                             // ERET_64e_branch_reg:
            // Arm64InstructionFactory.ERETAA(),                           // ERETAA_64e_branch_reg:
            // Arm64InstructionFactory.ERETAB(),                           // ERETAB_64e_branch_reg:
            // Arm64InstructionFactory.RET(X0),                            // RET_64r_branch_reg:
            // Arm64InstructionFactory.RETAA(),                            // RETAA_64e_branch_reg:
            // Arm64InstructionFactory.RETAASPPCR(X0),                     // RETAASPPCR_64m_branch_reg:
            // Arm64InstructionFactory.RETAASPPC(-0x100),                  // RETAASPPC_only_miscbranch:
            // Arm64InstructionFactory.RETAB(),                            // RETAB_64e_branch_reg:
            // Arm64InstructionFactory.RETABSPPCR(X0),                     // RETABSPPCR_64m_branch_reg:
            // Arm64InstructionFactory.RETABSPPC(-0x100),                  // RETABSPPC_only_miscbranch:
            Arm64InstructionFactory.TBNZ(X0, imm: 0x10, 0x100),            // TBNZ_only_testbranch:
            Arm64InstructionFactory.TBZ(X0, imm: 0x10, 0x100),             // TBZ_only_testbranch:
        ];

        private static readonly uint[] CallInstructions =
        [
            Arm64InstructionFactory.BL(0x100),        // BL_only_branch_imm
            Arm64InstructionFactory.BLR(X0),          // BLR_64_branch_reg
            // Arm64InstructionFactory.BLRAA(X0, X1), // BLRAA_64p_branch_reg
            // Arm64InstructionFactory.BLRAB(X0, X1), // BLRAB_64p_branch_reg
            // Arm64InstructionFactory.BLRAAZ(X0),    // BLRAAZ_64_branch_reg
            // Arm64InstructionFactory.BLRABZ(X0),    // BLRABZ_64_branch_reg
            Arm64InstructionFactory.HVC(0x100),       // HVC_ex_exception
            Arm64InstructionFactory.SMC(0x100),       // HVC_ex_exception
            Arm64InstructionFactory.SVC(0x100),       // SVC_ex_exception
        ];

        private static readonly uint[] BranchRelativeInstructions =
        [
            Arm64InstructionFactory.B(0x100),                              // B_only_branch_imm
            Arm64InstructionFactory.B(Arm64ConditionalKind.EQ, 0x100),     // B_only_condbranch
            // Arm64InstructionFactory.BC(Arm64ConditionalKind.NE, 0x100), // BC_only_condbranch
            Arm64InstructionFactory.BL(0x100),                             // BL_only_branch_imm
            Arm64InstructionFactory.CBNZ(W0,0x100),                        // CBNZ_32_compbranch
            Arm64InstructionFactory.CBNZ(X0,0x100),                        // CBNZ_64_compbranch
            Arm64InstructionFactory.CBZ(W0,0x100),                         // CBZ_32_compbranch
            Arm64InstructionFactory.CBZ(X0,0x100),                         // CBZ_64_compbranch
            // Arm64InstructionFactory.RETAASPPC(0x100),                   // RETAASPPC_only_miscbranch
            // Arm64InstructionFactory.RETABSPPC(0x100),                   // RETAASPPC_only_miscbranch
            Arm64InstructionFactory.TBNZ(X0,imm:0x10, label: 0x20),        // TBNZ_only_testbranch
            Arm64InstructionFactory.TBZ(X0,imm:0x10, label: 0x20),         // TBZ_only_testbranch
        ];

        private static InstructionTestData ToInstructionTestData(uint rawInstruction)
        {
            AsmArm64.Arm64Instruction instruction = AsmArm64.Arm64Instruction.Decode(rawInstruction);

            return new InstructionTestData
            {
                InstructionId = instruction.Id,
                Nemonic = instruction.Mnemonic,
                Text = instruction.ToString(),
                RawInstruction = rawInstruction,
            };
        }
    }
}
#endif
