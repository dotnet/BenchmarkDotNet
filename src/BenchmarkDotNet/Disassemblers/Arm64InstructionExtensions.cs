using AsmArm64;

namespace BenchmarkDotNet.Disassemblers;

internal static class Arm64InstructionExtensions
{
    private static bool TryGetOperandCore<T>(Arm64Instruction instruction, int index, Arm64OperandKind expectedKind, Func<Arm64Operand, T> castFunc, out T result)
           where T : struct, IArm64Operand
    {
        if ((uint) index >= (uint) instruction.Operands.Count)
        {
            result = default;
            return false;
        }

        var operand = instruction.GetOperand(index);

        if (operand.Kind != expectedKind)
        {
            result = default;
            return false;
        }

        result = castFunc(operand);
        return true;
    }

    // Define TryGetOperand helper extension method per operand types.
    // It's required because when using generics, it can't resolve cast (expicit operator).
    extension(Arm64Instruction instruction)
    {
        public bool TryGetOperand(int index, out Arm64RegisterOperand result)
            => TryGetOperandCore(instruction, index, Arm64OperandKind.Register, x => (Arm64RegisterOperand) x, out result);

        public bool TryGetOperand(int index, out Arm64ImmediateOperand result)
            => TryGetOperandCore(instruction, index, Arm64OperandKind.Immediate, x => (Arm64ImmediateOperand) x, out result);

        public bool TryGetOperand(int index, out Arm64LabelOperand result)
            => TryGetOperandCore(instruction, index, Arm64OperandKind.Label, x => (Arm64LabelOperand) x, out result);

        public bool TryGetOperand(int index, out Arm64ShiftOperand result)
            => TryGetOperandCore(instruction, index, Arm64OperandKind.Shift, x => (Arm64ShiftOperand) x, out result);

        public bool TryGetOperand(int index, out Arm64EnumOperand result)
            => TryGetOperandCore(instruction, index, Arm64OperandKind.Enum, x => (Arm64EnumOperand) x, out result);

        public bool TryGetOperand(int index, out Arm64MemoryOperand result)
            => TryGetOperandCore(instruction, index, Arm64OperandKind.Memory, x => (Arm64MemoryOperand) x, out result);
    }

    extension(Arm64Instruction instruction)
    {
        /// <summary>
        /// Helper method to get immediate value as `ulong`.
        /// arm64 immediate value is expected to be represented as signed long.
        /// Note:
        /// Some SIMD/SVE/SME instruction might have 64-bit immediate value, FP immediate value, or bitmask.
        /// In this case, it needs to use separated API (Currently there is no code path that handles these immediate values).
        /// </summary>
        public bool TryGetShiftedImmediateValueUnsigned(int index, out ulong value)
        {
            if (instruction.TryGetShiftedImmediateValue(index, out long signed))
            {
                value = (ulong) signed;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryGetShiftedImmediateValue(int index, out long value)
        {
            value = 0;
            var operands = instruction.Operands;
            if ((uint) index >= (uint) operands.Count)
                return false;

            if (!instruction.TryGetOperand(index, out Arm64ImmediateOperand immediateOperand))
                return false;

            var immediateValue = immediateOperand.Value;

            // Try to get shift modifier (AsmArm64 handle shift modifier as independent operand)
            if (!instruction.TryGetOperand(index + 1, out Arm64ShiftOperand shiftOperand))
            {
                value = immediateValue;
                return true;
            }

            var shiftAmount = shiftOperand.Amount;
            var shiftKind = shiftOperand.ShiftKind;
            return TryShiftImmediateValue(immediateValue, shiftKind, shiftAmount, out value);
        }

        /// <summary>
        /// Gets the base address used to resolve a label operand.
        /// ADRP encodes a page delta relative to the 4KB page containing the instruction,
        /// while other label operands are relative to the instruction address.
        /// (Matches AsmArm64's Arm64Disassembler.GetTargetAddress.)
        /// </summary>
        public ulong GetTargetAddressBase(ulong instructionAddress)
        {
            if (instruction.Mnemonic == Arm64Mnemonic.ADRP)
                return instructionAddress & ~0xFFFUL;
            return instructionAddress;
        }

        /// <summary>
        /// Resolves the absolute target address of a label operand.
        /// Address arithmetic wraps modulo 2^64, like hardware.
        /// </summary>
        public ulong GetTargetAddress(ulong instructionAddress, long labelOffset)
        {
            var targetAddress = instruction.GetTargetAddressBase(instructionAddress);
            return unchecked(targetAddress + (ulong) labelOffset);
        }

        public bool IsLdrFromTrackedRegister(Arm64RegisterAny trackedRegister)
        {
            if (instruction.Mnemonic != Arm64Mnemonic.LDR)
                return false;

            if (!instruction.TryGetOperand(1, out Arm64MemoryOperand memoryOperand))
                return false;

            var memoryAccessor = memoryOperand.Accessor;

            // Check base register is the register that is currently tracked.
            if (memoryAccessor.BaseRegister != trackedRegister)
                return false;

            // Check displacement.
            if (memoryAccessor.HasOptionalOffset && memoryAccessor.HasImmediate && memoryAccessor.Immediate != 0)
                return false;

            // Check index register is specified.
            if (memoryAccessor.HasIndexRegister)
                return false;

            // Ldr from tracked register (with no displacement/index register).
            return true;
        }

        public bool IsConditionalBranch()
        {
            switch (instruction.Mnemonic)
            {
                case Arm64Mnemonic.CBZ:
                case Arm64Mnemonic.CBNZ:
                case Arm64Mnemonic.TBZ:
                case Arm64Mnemonic.TBNZ:
                    return true;

                case Arm64Mnemonic.B:

                    var conditionalKind = Arm64ConditionalKind.Undefined;
                    if (instruction.TryGetOperand(0, out Arm64EnumOperand enumOperand))
                        conditionalKind = enumOperand.AsConditional;

                    switch (conditionalKind)
                    {
                        // `B` and `B.AL` instructions is unconditional branch.
                        case Arm64ConditionalKind.Undefined:
                        case Arm64ConditionalKind.AL:
                            return false;
                        default:
                            return true;
                    }

                default:
                    return false;
            }
        }

        public bool IsUnconditionalControlFlow()
        {
            return instruction.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE)
                || instruction.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_CALL)
                || instruction.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_JUMP);
        }

        public bool BelongsToGroup(Arm64InstructionGroupId expectedGroup)
        {
            // AsmArm64 don't provide mapping for instruction groups.
            // So it need to simulate Capstone's BelongsToGroup behavior.
            // Capstone's group mappings are defined at https://github.com/capstone-engine/capstone/blob/6.0.0-Alpha11/arch/AArch64/AArch64GenCSMappingInsn.inc
            switch (expectedGroup)
            {
                case Arm64InstructionGroupId.ARM64_GRP_JUMP:
                    switch (instruction.Mnemonic)
                    {
                        case Arm64Mnemonic.B:
                        case Arm64Mnemonic.BC:
                        case Arm64Mnemonic.BR:
                        case Arm64Mnemonic.BRAA:
                        case Arm64Mnemonic.BRAAZ:
                        case Arm64Mnemonic.BRAB:
                        case Arm64Mnemonic.BRABZ:
                        case Arm64Mnemonic.CBNZ:
                        case Arm64Mnemonic.CBZ:
                        case Arm64Mnemonic.DRPS:
                        case Arm64Mnemonic.ERET:
                        case Arm64Mnemonic.ERETAA:
                        case Arm64Mnemonic.ERETAB:
                        case Arm64Mnemonic.RET:
                        case Arm64Mnemonic.RETAA:
                        case Arm64Mnemonic.RETAASPPC:
                        case Arm64Mnemonic.RETAASPPCR:
                        case Arm64Mnemonic.RETAB:
                        case Arm64Mnemonic.RETABSPPC:
                        case Arm64Mnemonic.RETABSPPCR:
                        case Arm64Mnemonic.TBZ:
                        case Arm64Mnemonic.TBNZ:
                            return true;
                        default:
                            return false;
                    }

                case Arm64InstructionGroupId.ARM64_GRP_CALL:
                    switch (instruction.Mnemonic)
                    {
                        case Arm64Mnemonic.BL:
                        case Arm64Mnemonic.BLR:
                        case Arm64Mnemonic.BLRAA:
                        case Arm64Mnemonic.BLRAB:
                        case Arm64Mnemonic.BLRAAZ:
                        case Arm64Mnemonic.BLRABZ:
                        case Arm64Mnemonic.HVC:
                        case Arm64Mnemonic.SMC:
                        case Arm64Mnemonic.SVC:
                            return true;
                        default:
                            return false;
                    }
                case Arm64InstructionGroupId.ARM64_GRP_RET:
                    switch (instruction.Mnemonic)
                    {
                        case Arm64Mnemonic.DRPS:
                        case Arm64Mnemonic.ERET:
                        case Arm64Mnemonic.ERETAA:
                        case Arm64Mnemonic.ERETAB:
                        case Arm64Mnemonic.RET:
                        case Arm64Mnemonic.RETAA:
                        case Arm64Mnemonic.RETAASPPC:
                        case Arm64Mnemonic.RETAASPPCR:
                        case Arm64Mnemonic.RETAB:
                        case Arm64Mnemonic.RETABSPPC:
                        case Arm64Mnemonic.RETABSPPCR:
                            return true;
                        default:
                            return false;
                    }
                case Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE:
                    switch (instruction.Mnemonic)
                    {
                        case Arm64Mnemonic.B:
                        case Arm64Mnemonic.BC:
                        case Arm64Mnemonic.BL:
                        case Arm64Mnemonic.CBNZ:
                        case Arm64Mnemonic.CBZ:
                        case Arm64Mnemonic.RETAASPPC:
                        case Arm64Mnemonic.RETABSPPC:
                        case Arm64Mnemonic.TBNZ:
                        case Arm64Mnemonic.TBZ:
                            return true;
                        default:
                            return false;
                    }

                // Currently, other group ids are not supported.
                case Arm64InstructionGroupId.Invalid:
                case Arm64InstructionGroupId.ARM64_GRP_INT:
                case Arm64InstructionGroupId.ARM64_GRP_PRIVILEGE:
                case Arm64InstructionGroupId.ARM64_GRP_CRYPTO:
                case Arm64InstructionGroupId.ARM64_GRP_FPARMV8:
                case Arm64InstructionGroupId.ARM64_GRP_NEON:
                case Arm64InstructionGroupId.ARM64_GRP_CRC:
                default:
                    throw new NotSupportedException($"Group {expectedGroup} is not supported.");
            }
        }

        public IEnumerable<Arm64RegisterAny> GetWrittenRegisters()
        {
            foreach (var operand in instruction.Operands)
            {
                switch (operand.Kind)
                {
                    case Arm64OperandKind.Register:
                        var registerOperand = (Arm64RegisterOperand) operand;

                        if ((registerOperand.Flags & Arm64OperandFlags.Write) != 0)
                            yield return registerOperand.Value;

                        continue;

                    case Arm64OperandKind.RegisterGroup:
                        var registerGroupOperand = (Arm64RegisterGroupOperand) operand;

                        if ((registerGroupOperand.Flags & Arm64OperandFlags.Write) != 0)
                        {
                            var registerGroupAny = registerGroupOperand.Value;
                            var baseRegister = registerGroupAny.BaseRegister;
                            for (int i = 0; i < registerGroupAny.Count; ++i)
                            {
                                yield return Arm64RegisterAny.Create(baseRegister.Kind, baseRegister.Index + i, baseRegister.VKind, baseRegister.ElementCount, baseRegister.ElementIndex);
                            }
                        }
                        continue;

                    default:
                        continue;
                }
            }
        }
    }

    /// <summary>
    /// Applies a shift to an immediate value. Returns false for unsupported/invalid shift kinds.
    /// Use internal modifier for unit tests.
    /// </summary>
    internal static bool TryShiftImmediateValue(
        long immediateValue,
        Arm64ShiftKind shiftKind,
        byte shiftAmount,
        out long result)
    {
        result = 0;
        switch (shiftKind)
        {
            case Arm64ShiftKind.LSL:
                result = immediateValue << shiftAmount;
                return true;

            case Arm64ShiftKind.LSR:
                result = (long) ((ulong) immediateValue >> shiftAmount);
                return true;

            case Arm64ShiftKind.ASR:
                result = immediateValue >> shiftAmount;
                return true;

            case Arm64ShiftKind.ROR:
                {
                    var unsigned = (ulong) immediateValue;
                    var amount = shiftAmount & 63;
                    result = amount == 0
                        ? (long) unsigned
                        : (long) ((unsigned >> amount) | (unsigned << (64 - amount)));
                    return true;
                }

            // MSL (mask shift left) is used by bitmask immediates.
            // Treat as unsupported to avoid producing an incorrect value.
            case Arm64ShiftKind.MSL:
            default:
                return false;
        }
    }
}
