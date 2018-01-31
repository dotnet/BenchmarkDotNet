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
CFA: [0] def_cfa: %rsp+0x8
CFA: [0] offset: unknown at cfa-0x8
CFA: [4] def_cfa_offset: 0x10
CFA: [8] offset: %r15 at cfa-0x10
Basic block 0 starting at offset 0xb
Basic block 2 starting at offset 0xb
Basic block 1 starting at offset 0x27
CFA: [2f] def_cfa: %rsp+0x8
Method void BenchmarkDotNet.Samples.CPU.Cpu_Atomics:NoLock () emitted at 0x1027cdf80 to 0x1027cdfb0 (code length 48) [BenchmarkDotNet.Samples.exe]
/var/folders/ld/p9yn04fs3ys6h_dkyxvv95_40000gn/T/.WuSVhL:
(__TEXT,__text) section
chmarkDotNet_Samples_CPU_Cpu_Atomics_NoLock:
0000000000000000	subq	$0x8, %rsp
0000000000000004	movq	%r15, (%rsp)
0000000000000008	movq	%rdi, %r15
000000000000000b	movslq	0x18(%r15), %rax
000000000000000f	incl	%eax
0000000000000011	movl	%eax, 0x18(%r15)
0000000000000015	incl	%eax
0000000000000017	movl	%eax, 0x18(%r15)
000000000000001b	incl	%eax
000000000000001d	movl	%eax, 0x18(%r15)
0000000000000021	incl	%eax
0000000000000023	movl	%eax, 0x18(%r15)
0000000000000027	movq	(%rsp), %r15
000000000000002b	addq	$0x8, %rsp
000000000000002f	retq";

            var expected = new DisassemblyResult()
            {
                Methods = new[]
                {
                    new DisassembledMethod()
                    {
                        Name = "NoLock",
                        Maps = new[]
                        {
                            new Map()
                            {
                                Instructions = new[]
                                {
                                    new Diagnosers.Code { TextRepresentation = "subq\t$0x8, %rsp" },

                                    new Diagnosers.Code { TextRepresentation = "movq\t%r15, (%rsp)" },
                                    new Diagnosers.Code { TextRepresentation = "movq\t%rdi, %r15" },
                                    new Diagnosers.Code { TextRepresentation = "movslq\t0x18(%r15), %rax" },

                                    new Diagnosers.Code { TextRepresentation = "incl\t%eax" },
                                    new Diagnosers.Code { TextRepresentation = "movl\t%eax, 0x18(%r15)" },
                                    
                                    new Diagnosers.Code { TextRepresentation = "incl\t%eax" },
                                    new Diagnosers.Code { TextRepresentation = "movl\t%eax, 0x18(%r15)" },
                                    
                                    new Diagnosers.Code { TextRepresentation = "incl\t%eax" },
                                    new Diagnosers.Code { TextRepresentation = "movl\t%eax, 0x18(%r15)" },
                                    
                                    new Diagnosers.Code { TextRepresentation = "incl\t%eax" },
                                    new Diagnosers.Code { TextRepresentation = "movl\t%eax, 0x18(%r15)" },
                                    
                                    new Diagnosers.Code { TextRepresentation = "movq\t(%rsp), %r15" },
                                    new Diagnosers.Code { TextRepresentation = "addq\t$0x8, %rsp" },
                                    new Diagnosers.Code { TextRepresentation = "retq" }
                                }
                            }
                        }
                    }
                }
            };

            var disassemblyResult = MonoDisassembler.OutputParser.Parse(
                input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
                "NoLock", commandLine: string.Empty);

            Assert.Equal(expected.Methods.Single().Name, disassemblyResult.Methods.Single().Name);
            Assert.Equal(expected.Methods[0].Maps[0].Instructions.Length, disassemblyResult.Methods[0].Maps[0].Instructions.Length);

            for (int i = 0; i < expected.Methods[0].Maps[0].Instructions.Length; i++)
                Assert.Equal(expected.Methods[0].Maps[0].Instructions[i].TextRepresentation, 
                    disassemblyResult.Methods[0].Maps[0].Instructions[i].TextRepresentation);
        }
    }
}