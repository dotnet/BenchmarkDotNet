using System;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class MonoDisassemblyOutputParserTests
    {
        [Fact]
        public void CanParseMonoDisassemblyOutput()
        {
            const string input = @"
LOCAL REGALLOC BLOCK 2:
	1  il_seq_point il: 0x0
	2  loadi4_membase R11 <- [%edi + 0xc]
	3  int_add_imm R13 <- R11 [1] clobbers: 1
	4  storei4_membase_reg [%edi + 0xc] <- R13
	5  il_seq_point il: 0xe
	6  move R16 <- R13
	7  int_add_imm R18 <- R16 [1] clobbers: 1
	8  storei4_membase_reg [%edi + 0xc] <- R18
	9  il_seq_point il: 0x1c
	10 move R21 <- R18
	11 int_add_imm R23 <- R21 [1] clobbers: 1
	12 storei4_membase_reg [%edi + 0xc] <- R23
	13 il_seq_point il: 0x2a
	14 move R26 <- R23
	15 int_add_imm R28 <- R26 [1] clobbers: 1
	16 storei4_membase_reg [%edi + 0xc] <- R28
	17 il_seq_point il: 0x38
liveness: %edi [4 - 0]
liveness: R11 [2 - 2]
liveness: R13 [3 - 3]
liveness: R16 [6 - 6]
liveness: R18 [7 - 7]
liveness: R21 [10 - 10]
liveness: R23 [11 - 11]
liveness: R26 [14 - 14]
liveness: R28 [15 - 15]
processing:	17 il_seq_point il: 0x38
	17 il_seq_point il: 0x38
processing:	16 storei4_membase_reg [%edi + 0xc] <- R28
	assigned sreg1 %eax to R28
	16 storei4_membase_reg [%edi + 0xc] <- %eax
processing:	15 int_add_imm R28 <- R26 [1] clobbers: 1
	assigned dreg %eax to dest R28
	freeable %eax (R28) (born in 15)
	assigned sreg1 %eax to R26
	15 int_add_imm %eax <- %eax [1] clobbers: 1
processing:	14 move R26 <- R23
	assigned dreg %eax to dest R26
	freeable %eax (R26) (born in 14)
	assigned sreg1 %eax to R23
	14 move %eax <- %eax
processing:	13 il_seq_point il: 0x2a
	13 il_seq_point il: 0x2a
processing:	12 storei4_membase_reg [%edi + 0xc] <- R23
	12 storei4_membase_reg [%edi + 0xc] <- %eax
processing:	11 int_add_imm R23 <- R21 [1] clobbers: 1
	assigned dreg %eax to dest R23
	freeable %eax (R23) (born in 11)
	assigned sreg1 %eax to R21
	11 int_add_imm %eax <- %eax [1] clobbers: 1
processing:	10 move R21 <- R18
	assigned dreg %eax to dest R21
	freeable %eax (R21) (born in 10)
	assigned sreg1 %eax to R18
	10 move %eax <- %eax
processing:	9  il_seq_point il: 0x1c
	9  il_seq_point il: 0x1c
processing:	8  storei4_membase_reg [%edi + 0xc] <- R18
	8  storei4_membase_reg [%edi + 0xc] <- %eax
processing:	7  int_add_imm R18 <- R16 [1] clobbers: 1
	assigned dreg %eax to dest R18
	freeable %eax (R18) (born in 7)
	assigned sreg1 %eax to R16
	7  int_add_imm %eax <- %eax [1] clobbers: 1
processing:	6  move R16 <- R13
	assigned dreg %eax to dest R16
	freeable %eax (R16) (born in 6)
	assigned sreg1 %eax to R13
	6  move %eax <- %eax
processing:	5  il_seq_point il: 0xe
	5  il_seq_point il: 0xe
processing:	4  storei4_membase_reg [%edi + 0xc] <- R13
	4  storei4_membase_reg [%edi + 0xc] <- %eax
processing:	3  int_add_imm R13 <- R11 [1] clobbers: 1
	assigned dreg %eax to dest R13
	freeable %eax (R13) (born in 3)
	assigned sreg1 %eax to R11
	3  int_add_imm %eax <- %eax [1] clobbers: 1
processing:	2  loadi4_membase R11 <- [%edi + 0xc]
	assigned dreg %eax to dest R11
	freeable %eax (R11) (born in 2)
	2  loadi4_membase %eax <- [%edi + 0xc]
processing:	1  il_seq_point il: 0x0
	1  il_seq_point il: 0x0
CFA: [0] def_cfa: %esp+0x4
CFA: [0] offset: unknown at cfa-0x4
CFA: [1] def_cfa_offset: 0x8
CFA: [1] offset: %ebp at cfa-0x8
CFA: [3] def_cfa_reg: %ebp
CFA: [4] offset: %edi at cfa-0xc
Argument 0 assigned to register %edi
Basic block 0 starting at offset 0xa
Basic block 2 starting at offset 0xa
Basic block 1 starting at offset 0x1d
Method void BenchmarkDotNet.Samples.CPU.Cpu_Atomics:NoLock () emitted at 03AC11D0 to 03AC11F6 (code length 38) [BenchmarkDotNet.Samples.exe]
";

            var expected = new DisassemblyResult()
            {
                Methods = new[]
                {
                    new DisassembledMethod()
                    {
                        Name = "NoLock",
                        Maps = new Map[]
                        {
                            new Map()
                            {
                                Instructions = new[]
                                {
                                    new Diagnosers.Code { TextRepresentation = "loadi4_membase %eax <- [%edi + 0xc]" },

                                    new Diagnosers.Code { TextRepresentation = "int_add_imm %eax <- %eax [1] clobbers: 1" },
                                    new Diagnosers.Code { TextRepresentation = "storei4_membase_reg [%edi + 0xc] <- %eax" },
                                    new Diagnosers.Code { TextRepresentation = "move %eax <- %eax" },

                                    new Diagnosers.Code { TextRepresentation = "int_add_imm %eax <- %eax [1] clobbers: 1" },
                                    new Diagnosers.Code { TextRepresentation = "storei4_membase_reg [%edi + 0xc] <- %eax" },
                                    new Diagnosers.Code { TextRepresentation = "move %eax <- %eax" },

                                    new Diagnosers.Code { TextRepresentation = "int_add_imm %eax <- %eax [1] clobbers: 1" },
                                    new Diagnosers.Code { TextRepresentation = "storei4_membase_reg [%edi + 0xc] <- %eax" },
                                    new Diagnosers.Code { TextRepresentation = "move %eax <- %eax" },

                                    new Diagnosers.Code { TextRepresentation = "int_add_imm %eax <- %eax [1] clobbers: 1" },
                                    new Diagnosers.Code { TextRepresentation = "storei4_membase_reg [%edi + 0xc] <- %eax" },
                                }
                            }
                        }
                    }
                }
            };

            var disassemblyResult = MonoDisassembler.OutputParser.Parse(
                input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
                "BenchmarkDotNet.Samples.CPU.Cpu_Atomics:NoLock",
                "NoLock");

            Assert.Equal(expected.Methods.Single().Name, disassemblyResult.Methods.Single().Name);
            Assert.Equal(expected.Methods[0].Maps[0].Instructions.Length, disassemblyResult.Methods[0].Maps[0].Instructions.Length);

            for (int i = 0; i < expected.Methods[0].Maps[0].Instructions.Length; i++)
            {
                Assert.Equal(expected.Methods[0].Maps[0].Instructions[i].TextRepresentation, 
                    disassemblyResult.Methods[0].Maps[0].Instructions[i].TextRepresentation);
            }
        }
    }
}