---
uid: BenchmarkDotNet.Samples.IntroDisassembly
---

## Sample: IntroDisassembly

### Source code

[!code-csharp[IntroDisassembly.cs](../../../samples/BenchmarkDotNet.Samples/IntroDisassembly.cs)]

### Output

```x86asm
; .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3110.0
05452718 BenchmarkDotNet.Samples.IntroDisassembly.Sum()
IL_0000: ldc.r8 0
IL_0009: stloc.0
0545271c d9ee            fldz
IL_000a: ldc.i4.0
IL_000b: stloc.1
IL_000c: br.s IL_0017
0545271e 33c0            xor     eax,eax
IL_000e: ldloc.0
IL_000f: ldloc.1
IL_0010: conv.r8
IL_0011: add
IL_0012: stloc.0
05452720 8945fc          mov     dword ptr [ebp-4],eax
05452723 db45fc          fild    dword ptr [ebp-4]
05452726 dec1            faddp   st(1),st
IL_0013: ldloc.1
IL_0014: ldc.i4.1
IL_0015: add
IL_0016: stloc.1
05452728 40              inc     eax
IL_0017: ldloc.1
IL_0018: ldc.i4.s 64
IL_001a: blt.s IL_000e
05452729 83f840          cmp     eax,40h
0545272c 7cf2            jl      05452720
IL_001c: ldloc.0
IL_001d: ret
0545272e 8be5            mov     esp,ebp
```

```x86asm
; .NET Core 2.1.0 (CoreCLR 4.6.26515.07, CoreFX 4.6.26515.06), 64bit RyuJIT
00007ffa`6c621320 BenchmarkDotNet.Samples.IntroDisassembly.Sum()
IL_0000: ldc.r8 0
IL_0009: stloc.0
00007ffa`6c621323 c4e17857c0      vxorps  xmm0,xmm0,xmm0
IL_000a: ldc.i4.0
IL_000b: stloc.1
IL_000c: br.s IL_0017
00007ffa`6c621328 33c0            xor     eax,eax
IL_000e: ldloc.0
IL_000f: ldloc.1
IL_0010: conv.r8
IL_0011: add
IL_0012: stloc.0
00007ffa`6c62132a c4e17057c9      vxorps  xmm1,xmm1,xmm1
00007ffa`6c62132f c4e1732ac8      vcvtsi2sd xmm1,xmm1,eax
00007ffa`6c621334 c4e17b58c1      vaddsd  xmm0,xmm0,xmm1
IL_0013: ldloc.1
IL_0014: ldc.i4.1
IL_0015: add
IL_0016: stloc.1
00007ffa`6c621339 ffc0            inc     eax
IL_0017: ldloc.1
IL_0018: ldc.i4.s 64
IL_001a: blt.s IL_000e
00007ffa`6c62133b 83f840          cmp     eax,40h
00007ffa`6c62133e 7cea            jl      00007ffa`6c62132a
IL_001c: ldloc.0
IL_001d: ret
00007ffa`6c621340 c3              ret
```

```x86asm
Mono 5.12.0 (Visual Studio), 64bit
 Sum
sub    $0x18,%rsp
mov    %rsi,(%rsp)
xorpd  %xmm0,%xmm0
movsd  %xmm0,0x8(%rsp)
xor    %esi,%esi
jmp    2e 
xchg   %ax,%ax
movsd  0x8(%rsp),%xmm0
cvtsi2sd %esi,%xmm1
addsd  %xmm1,%xmm0
movsd  %xmm0,0x8(%rsp)
inc    %esi
cmp    $0x40,%esi
jl     18 
movsd  0x8(%rsp),%xmm0
mov    (%rsp),%rsi
add    $0x18,%rsp
retq   
```

### Links

* @docs.diagnosers
* @docs.disassembler
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroDisassembly

---