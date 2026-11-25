using AsmArm64;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using Iced.Intel;
using System.Security.Cryptography;
using static AsmArm64.Arm64InstructionFactory;
using static AsmArm64.Arm64RegisterD;
using static AsmArm64.Arm64RegisterSP;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

public partial class Arm64InstructionFormatterTests : Arm64DisassemblerTestBase
{
    [Theory]
    [MemberData(nameof(TestData.MnemonicPatterns), MemberType = typeof(TestData))]
    public void VerifyMnemonicAlias(string expected, bool useBaseInstruction, uint rawInstruction)
    {
        // Arrange
        var asm = ToArm64Asm(rawInstruction, address: 0);

        // Act
        var result = Arm64InstructionFormatter.GetInstructionDetails(asm.Instruction, asm.InstructionPointer).Mnemonic;

        // Assert
        result.Should().Be(expected);

        var instruction = Arm64Instruction.Decode(rawInstruction);
        if (useBaseInstruction)
        {
            result.Should().NotBe(instruction.Mnemonic.ToText());
            instruction = instruction.AsBaseInstruction();
        }
        result.Should().Be(instruction.Mnemonic.ToText());
    }

    public static partial class TestData
    {
        public static TheoryData<string, bool, uint> MnemonicPatterns => new()
        {
            { "mov",  false, MOV(X0, 1) }, // `Arm64InstructionFactory.MOV` don't accept `0`. See: https://github.com/xoofx/AsmArm64/issues/16
            { "movi", false, MOVI(D0, 0) },
            { "movk", false, MOVK(X0, 0) },

            // Folowing instructions are resolved to `mov` alias by default.
            { "movn", true,  MOVN(X0, 0) },
            { "movn", true,  MOV_MOVN(X0, new Arm64InvertedShiftedImmediate64(0, 0)) },
            { "movz", true,  MOVZ(X0, 0) },

            // Folowing instructions are resolved to `subs` by default.
            { "cmp",  false,  CMP(X0, X1) },
            { "cmn",  false,  CMN(X0, X1) },

            // Other instructions are resolved with default setting(Arm64InstructionAliasMode.Preferred)
            { "retaa",  false,  RETAA() },
        };
    }
}

