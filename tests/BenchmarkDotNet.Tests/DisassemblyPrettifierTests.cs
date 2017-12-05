using BenchmarkDotNet.Diagnosers;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class DisassemblyPrettifierTests
    {
        [Fact]
        public void SimpleMethodCanBePrettified()
        {
            var method = new DisassembledMethod
            {
                Maps = new []
                {
                    new Map
                    {
                        Instructions = new Diagnosers.Code[]
                        {
                            new Asm { TextRepresentation = "00007ff7`ffbfd2da 33ff            xor     edi,edi" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd2dc 48b9e04be659f87f0000 mov rcx,offset System_Private_CoreLib+0x8f4be0 (00007ff8`59e64be0)" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd2e6 e80570ae5f      call    coreclr!MetaDataGetDispenser+0x72810 (00007ff8`5f6e42f0)", Comment = "not managed method" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd2eb 488bd8          mov     rbx,rax" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd2ee 8b4e08          mov     ecx,dword ptr [rsi+8]" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd2f1 894b08          mov     dword ptr [rbx+8],ecx" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd2f4 48b9e04be659f87f0000 mov rcx,offset System_Private_CoreLib+0x8f4be0 (00007ff8`59e64be0)" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd2fe e8ed6fae5f      call    coreclr!MetaDataGetDispenser+0x72810 (00007ff8`5f6e42f0)", Comment = "not managed method" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd303 488bd0          mov     rdx,rax" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd306 c742080c000000  mov     dword ptr [rdx+8],0Ch" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd30d 488bcb          mov     rcx,rbx" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd310 e84ba1ee59      call    System_Private_CoreLib+0x577460 (00007ff8`59ae7460)", Comment = "HasFlag" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd315 88460c          mov     byte ptr [rsi+0Ch],al" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd318 ffc7            inc     edi" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd31a 81ffe8030000    cmp     edi,3E8h" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd320 7cba            jl      00007ff7`ffbfd2dc" },
                            new Asm { TextRepresentation = "00007ff7`ffbfd322 4883c420        add     rsp,20h" }
                        }
                    }
                },
                Name = "Test"
            };

            var expectedOutput = new string[]
            {
                "xor     edi,edi",

                "T_L00",
                "mov     rcx,offset System_Private_CoreLib+0x8f4be0",
                "call    coreclr!MetaDataGetDispenser+0x72810",
                "mov     rbx,rax",
                "mov     ecx,dword ptr [rsi+8]",
                "mov     dword ptr [rbx+8],ecx",
                "mov     rcx,offset System_Private_CoreLib+0x8f4be0",
                "call    coreclr!MetaDataGetDispenser+0x72810",
                "mov     rdx,rax",
                "mov     dword ptr [rdx+8],0Ch",
                "mov     rcx,rbx",
                "call    HasFlag",
                "mov     byte ptr [rsi+0Ch],al",
                "inc     edi",
                "cmp     edi,3E8h",
                "jl      T_L00",

                "add     rsp,20h"
            };

            var prettyOutput = DisassemblyPrettifier.Prettify(method, "T");

            for (int i = 0; i < expectedOutput.Length; i++)
                Assert.Equal(expectedOutput[i], prettyOutput[i].TextRepresentation);
        }

        [Fact]
        public void MethodWithFewJumpsCanBePrettified()
        {
            var method = new DisassembledMethod
            {
                Maps = new[]
                {
                    new Map
                    {
                        Instructions = new Diagnosers.Code[]
                        {
                            new Asm { TextRepresentation = "00007ffd`a6304a70 8b4108          mov     eax,dword ptr [rcx+8]" },
                            new Asm { TextRepresentation = "00007ffd`a6304a73 48894c2410      mov     qword ptr [rsp+10h],rcx" },
                            new Asm { TextRepresentation = "00007ffd`a6304a78 4885c9          test    rcx,rcx" },
                            new Asm { TextRepresentation = "00007ffd`a6304a7b 7404            je      00007ffd`a6304a81" },
                            new Asm { TextRepresentation = "00007ffd`a6304a7d 4883c10c        add     rcx,0Ch" },
                            new Asm { TextRepresentation = "00007ffd`a6304a81 4889542408      mov     qword ptr [rsp+8],rdx" },
                            new Asm { TextRepresentation = "00007ffd`a6304a86 4885d2          test    rdx,rdx" },
                            new Asm { TextRepresentation = "00007ffd`a6304a89 7404            je      00007ffd`a6304a8f" },
                            new Asm { TextRepresentation = "00007ffd`a6304a8b 4883c20c        add     rdx,0Ch" },
                            new Asm { TextRepresentation = "00007ffd`a6304a8f 85c0            test    eax,eax" },
                            new Asm { TextRepresentation = "00007ffd`a6304a91 741b            je      00007ffd`a6304aae" },
                            new Asm { TextRepresentation = "00007ffd`a6304a93 440fb701        movzx   r8d,word ptr [rcx]" },
                            new Asm { TextRepresentation = "00007ffd`a6304a97 440fb70a        movzx   r9d,word ptr [rdx]" },
                            new Asm { TextRepresentation = "00007ffd`a6304a9b 453bc1          cmp     r8d,r9d" },
                            new Asm { TextRepresentation = "00007ffd`a6304a9e 7518            jne     00007ffd`a6304ab8" },
                            new Asm { TextRepresentation = "00007ffd`a6304aa0 4883c102        add     rcx,2" },
                            new Asm { TextRepresentation = "00007ffd`a6304aa4 4883c202        add     rdx,2" },
                            new Asm { TextRepresentation = "00007ffd`a6304aa8 ffc8            dec     eax" },
                            new Asm { TextRepresentation = "00007ffd`a6304aaa 85c0            test    eax,eax" },
                            new Asm { TextRepresentation = "00007ffd`a6304aac 75e5            jne     00007ffd`a6304a93" },
                            new Asm { TextRepresentation = "00007ffd`a6304aae b801000000      mov     eax,1" },
                            new Asm { TextRepresentation = "00007ffd`a6304ab3 4883c418        add     rsp,18h" },
                            new Asm { TextRepresentation = "00007ffd`a6304ab7 c3              ret" },
                            new Asm { TextRepresentation = "00007ffd`a6304ab8 33c0            xor     eax,eax" }
                        }
                    }
                },
                Name = "Test"
            };

            var expectedOutput = new[] {
                "mov     eax,dword ptr [rcx+8]",
                "mov     qword ptr [rsp+10h],rcx",
                "test    rcx,rcx",
                "je      M00_L00",
                "add     rcx,0Ch",

                "M00_L00",
                "mov     qword ptr [rsp+8],rdx",
                "test    rdx,rdx",
                "je      M00_L01",
                "add     rdx,0Ch",

                "M00_L01",
                "test    eax,eax",
                "je      M00_L03",

                "M00_L02",
                "movzx   r8d,word ptr [rcx]",
                "movzx   r9d,word ptr [rdx]",
                "cmp     r8d,r9d",
                "jne     M00_L04",
                "add     rcx,2",
                "add     rdx,2",
                "dec     eax",
                "test    eax,eax",
                "jne     M00_L02",

                "M00_L03",
                "mov     eax,1",
                "add     rsp,18h",
                "ret",

                "M00_L04",
                "xor     eax,eax"
            };

            var prettyOutput = DisassemblyPrettifier.Prettify(method, "M00");

            for (int i = 0; i < expectedOutput.Length; i++)
                Assert.Equal(expectedOutput[i], prettyOutput[i].TextRepresentation);
        }

    }
}