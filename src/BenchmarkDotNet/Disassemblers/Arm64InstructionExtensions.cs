using AsmArm64;
using Gee.External.Capstone.Arm64;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AsmArm64Instruction = AsmArm64.Arm64Instruction;
using AsmArm64Operand = AsmArm64.Arm64Operand;
using Arm64InstructionId = AsmArm64.Arm64InstructionId;
using CapstoneArm64Instruction = Gee.External.Capstone.Arm64.Arm64Instruction;
using CapstoneArm64InstructionId = Gee.External.Capstone.Arm64.Arm64InstructionId;
using CapstoneArm64Operand = Gee.External.Capstone.Arm64.Arm64Operand;
using CapstoneArm64OperandType = Gee.External.Capstone.Arm64.Arm64OperandType;
using CapstoneArm64RegisterId = Gee.External.Capstone.Arm64.Arm64RegisterId;

namespace BenchmarkDotNet.Disassemblers;

internal static class Arm64InstructionExtensions
{
    #region Extension methods for Capstone
    extension(CapstoneArm64Instruction instruction)
    {
        public CapstoneArm64RegisterId GetRegisterId(int index)
        {
            var operands = instruction.Details.Operands;
            if ((uint)index >= (uint)operands.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return operands[index].Register.Id;
        }

        public Arm64ShiftOperation GetShiftOperation(int index)
        {
            var operands = instruction.Details.Operands;
            if ((uint)index >= (uint)operands.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var operand = operands[index];
            var shiftOperation = operand.ShiftOperation;

            switch (instruction.Id)
            {
                case CapstoneArm64InstructionId.ARM64_INS_ADD:
                case CapstoneArm64InstructionId.ARM64_INS_ADRP:
                case CapstoneArm64InstructionId.ARM64_INS_MOVK:
                case CapstoneArm64InstructionId.ARM64_INS_MOVZ:
                    switch (shiftOperation)
                    {
                        case Arm64ShiftOperation.ARM64_SFT_LSL:
                            return shiftOperation;

                        case Arm64ShiftOperation.Invalid:
                            if (operand.ShiftValue == 0)
                                return Arm64ShiftOperation.ARM64_SFT_LSL; // Capstone represents LSL #0 as ARM64_SFT_INVALID.
                            goto default;

                        // Currently used instructions don't accept other shift operations.
                        default:
                            throw new NotSupportedException($"Unexpected shift operation({shiftOperation}) is specified on instruction({instruction.Id})");
                    }

                // Currently other instructions are not supported.
                default:
                    throw new UnreachableException($"Unexpected instruction({instruction.Id}) is specified");
            }
        }

        public int GetShiftAmount(int index)
        {
            var operands = instruction.Details.Operands;
            if ((uint)index >= (uint)operands.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var operand = operands[index];

            // Capstone represents LSL #0 as ARM64_SFT_INVALID.
            if (operand.ShiftOperation == Arm64ShiftOperation.Invalid)
                return 0;

            return operand.ShiftValue;
        }

        public long GetRawImmediateValue(int index)
        {
            var operands = instruction.Details.Operands;
            if ((uint)index >= (uint)operands.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return operands[index].Immediate;
        }

        public long GetShiftedImmediateValue(int index)
        {
            var operands = instruction.Details.Operands;
            if ((uint)index >= (uint)operands.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var operand = operands[index];

            var shiftOperation = instruction.GetShiftOperation(index);
            switch (shiftOperation)
            {
                case Arm64ShiftOperation.ARM64_SFT_LSL:
                    var shiftAmount = instruction.GetShiftAmount(index);
                    return operand.Immediate << shiftAmount;
                default:
                    throw new NotSupportedException($"Unexpected shift operation({shiftOperation}) is specified on instruction({instruction.Id})");
            }
        }

        public bool TryGetOperand(
          int index,
          CapstoneArm64OperandType type,
          [NotNullWhen(true)] out CapstoneArm64Operand? result)
        {
            var operands = instruction.Details.Operands;
            if ((uint)index >= (uint)operands.Length)
            {
                result = null;
                return false;
            }

            result = operands[index];
            if (result.Type == type)
                return true;

            result = null;
            return false;
        }

        public bool BelongsToGroup(Arm64InstructionGroupId expectedGroup)
        {
            if (instruction.Bytes.Length < 4)
                return false;
            var rawInstruction = BinaryPrimitives.ReadUInt32LittleEndian(instruction.Bytes);
            return AsmArm64Instruction.Decode(rawInstruction).BelongsToGroup(expectedGroup);
        }

        public bool IsLdrFromTrackedRegister(CapstoneArm64RegisterId trackedRegister)
        {
            if (instruction.Id != CapstoneArm64InstructionId.ARM64_INS_LDR)
                return false;

            if (!instruction.TryGetOperand(1, CapstoneArm64OperandType.Memory, out var memoryOperand))
                return false;

            // Check base register is the register that is currently tracked.
            var memory = memoryOperand.Memory;
            if (memory.Base.Id != trackedRegister)
                return false;

            // Check displacement.
            if (memory.Displacement != 0)
                return false;

            // Check index register is specified.
            if (memory.Index != null)
                return false;

            // Ldr from tracked register (with no displacement/index register).
            return true;
        }

        public bool IsConditionalBranch()
        {
            switch (instruction.Id)
            {
                case Gee.External.Capstone.Arm64.Arm64InstructionId.ARM64_INS_CBZ:
                case Gee.External.Capstone.Arm64.Arm64InstructionId.ARM64_INS_CBNZ:
                case Gee.External.Capstone.Arm64.Arm64InstructionId.ARM64_INS_TBZ:
                case Gee.External.Capstone.Arm64.Arm64InstructionId.ARM64_INS_TBNZ:
                    return true;

                case Gee.External.Capstone.Arm64.Arm64InstructionId.ARM64_INS_B:
                    switch (instruction.Details.ConditionCode)
                    {
                        // `B` and `B.AL` instructions is unconditional branch.
                        case Gee.External.Capstone.Arm64.Arm64ConditionCode.Invalid:
                        case Gee.External.Capstone.Arm64.Arm64ConditionCode.ARM64_CC_AL:
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

    }
    #endregion

    #region Extension methods for AsmArm64
    extension(AsmArm64Instruction instruction)
    {
        public Arm64RegisterOperand GetRegisterOperand(int index)
        => (Arm64RegisterOperand)instruction.GetOperand(index, Arm64OperandKind.Register);

        public Arm64ImmediateOperand GetImmediateOperand(int index)
           => (Arm64ImmediateOperand)instruction.GetOperand(index, Arm64OperandKind.Immediate);

        private AsmArm64Operand GetOperand(int index, Arm64OperandKind expectedKind)
        {
            var operand = instruction.GetOperand(index);
            if (operand.Kind != expectedKind)
                throw new ArgumentException($"The operand at index {index} is not of the expected kind {expectedKind}. Actual kind: {operand.Kind}");

            return operand;
        }

        public bool BelongsToGroup(Arm64InstructionGroupId expectedGroup)
        {
            // AsmArm64 don't provide mapping for instruction groups.
            // So it need to simulate Capstone's BelongsToGroup behavior.
            // Capstone's group mappings are defined at https://github.com/capstone-engine/capstone/blob/6.0.0-Alpha10/arch/AArch64/AArch64GenCSMappingInsn.inc
            switch (expectedGroup)
            {
                case Arm64InstructionGroupId.ARM64_GRP_JUMP:
                    switch (instruction.Id)
                    {
                        case Arm64InstructionId.B_only_branch_imm:
                        case Arm64InstructionId.B_only_condbranch:
                        case Arm64InstructionId.BC_only_condbranch:
                        case Arm64InstructionId.BR_64_branch_reg:
                        case Arm64InstructionId.BRAA_64p_branch_reg:
                        case Arm64InstructionId.BRAAZ_64_branch_reg:
                        case Arm64InstructionId.BRAB_64p_branch_reg:
                        case Arm64InstructionId.BRABZ_64_branch_reg:
                        case Arm64InstructionId.CBNZ_32_compbranch:
                        case Arm64InstructionId.CBNZ_64_compbranch:
                        case Arm64InstructionId.CBZ_32_compbranch:
                        case Arm64InstructionId.CBZ_64_compbranch:
                        case Arm64InstructionId.DRPS_64e_branch_reg:
                        case Arm64InstructionId.ERET_64e_branch_reg:
                        case Arm64InstructionId.ERETAA_64e_branch_reg:
                        case Arm64InstructionId.ERETAB_64e_branch_reg:
                        case Arm64InstructionId.RET_64r_branch_reg:
                        case Arm64InstructionId.RETAA_64e_branch_reg:
                        case Arm64InstructionId.RETAASPPCR_64m_branch_reg:
                        case Arm64InstructionId.RETAASPPC_only_miscbranch:
                        case Arm64InstructionId.RETAB_64e_branch_reg:
                        case Arm64InstructionId.RETABSPPCR_64m_branch_reg:
                        case Arm64InstructionId.RETABSPPC_only_miscbranch:
                        case Arm64InstructionId.TBNZ_only_testbranch:
                        case Arm64InstructionId.TBZ_only_testbranch:
                            return true;

                        default:
                            return false;
                    }

                case Arm64InstructionGroupId.ARM64_GRP_CALL:
                    switch (instruction.Id)
                    {
                        case Arm64InstructionId.BL_only_branch_imm:
                        case Arm64InstructionId.BLR_64_branch_reg:    // BLR: Branch with link
                        case Arm64InstructionId.BLRAA_64p_branch_reg: // BLRAA: Branch with link to register with PAC
                        case Arm64InstructionId.BLRAB_64p_branch_reg: // BLRAB: Branch with link to register with PAC
                        case Arm64InstructionId.BLRAAZ_64_branch_reg: // Z: Zero modifier
                        case Arm64InstructionId.BLRABZ_64_branch_reg: // Z: Zero modifier
                        case Arm64InstructionId.HVC_ex_exception:
                        case Arm64InstructionId.SMC_ex_exception:
                        case Arm64InstructionId.SVC_ex_exception:
                            return true;
                        default:
                            return false;
                    }
                case Arm64InstructionGroupId.ARM64_GRP_RET:
                    switch (instruction.Id)
                    {
                        case Arm64InstructionId.DRPS_64e_branch_reg:
                        case Arm64InstructionId.ERET_64e_branch_reg:
                        case Arm64InstructionId.ERETAA_64e_branch_reg:
                        case Arm64InstructionId.ERETAB_64e_branch_reg:
                        case Arm64InstructionId.RET_64r_branch_reg:
                        case Arm64InstructionId.RETAA_64e_branch_reg:
                        case Arm64InstructionId.RETAASPPC_only_miscbranch:
                        case Arm64InstructionId.RETAASPPCR_64m_branch_reg:
                        case Arm64InstructionId.RETAB_64e_branch_reg:
                        case Arm64InstructionId.RETABSPPC_only_miscbranch:
                        case Arm64InstructionId.RETABSPPCR_64m_branch_reg:
                            return true;
                        default:
                            return false;
                    }
                case Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE:
                    switch (instruction.Id)
                    {
                        case Arm64InstructionId.B_only_branch_imm:
                        case Arm64InstructionId.B_only_condbranch:
                        case Arm64InstructionId.BC_only_condbranch:
                        case Arm64InstructionId.BL_only_branch_imm:
                        case Arm64InstructionId.CBNZ_32_compbranch:
                        case Arm64InstructionId.CBNZ_64_compbranch:
                        case Arm64InstructionId.CBZ_32_compbranch:
                        case Arm64InstructionId.CBZ_64_compbranch:
                        case Arm64InstructionId.RETAASPPC_only_miscbranch:
                        case Arm64InstructionId.RETABSPPC_only_miscbranch:
                        case Arm64InstructionId.TBNZ_only_testbranch:
                        case Arm64InstructionId.TBZ_only_testbranch:
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
                        var registerOperand = (Arm64RegisterOperand)operand;

                        if ((registerOperand.Flags & Arm64OperandFlags.Write) != 0)
                            yield return registerOperand.Value;

                        continue;

                    case Arm64OperandKind.RegisterGroup:
                        var registerGroupOperand = (Arm64RegisterGroupOperand)operand;

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
    #endregion
}
