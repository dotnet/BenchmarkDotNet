using Gee.External.Capstone.Arm64;
using Microsoft.Diagnostics.Runtime.Interfaces;

namespace BenchmarkDotNet.Disassemblers;

internal struct Arm64RegisterValueAccumulator
{
    internal enum State
    {
        LookingForPattern,
        ExpectingMovk,
        ExpectingAdd,
        LookingForPossibleLdr
    }

    private State _state;
    private long _value; // TODO: Change address value type to ulong (address with offset value)
    private int _expectedMovkShift;
    private Arm64RegisterId _registerId;
    private IClrRuntime _runtime;

    public void Init(IClrRuntime runtime)
    {
        _runtime = runtime;
        Reset();
    }

    internal void Reset(
        State state = State.LookingForPattern,
        int expectedMovkShift = 0,
        long value = 0,
        Arm64RegisterId registerId = Arm64RegisterId.Invalid)
    {
        _state = state;
        _expectedMovkShift = expectedMovkShift;
        _value = value;
        _registerId = registerId;
    }

    public void Feed(Arm64Instruction instruction)
    {
        switch (_state)
        {
            case State.LookingForPattern:
                if (TryHandleLookingForPattern(instruction))
                    return;

                // TODO: Check register overwrite.
                // goto default;
                break;

            case State.ExpectingMovk:
                if (TryHandleExpectingMovk(instruction))
                    return;

                // If we didn't find a expecting MOVK instruction, we might be looking for a possible LDR
                _state = State.LookingForPossibleLdr;
                goto case State.LookingForPossibleLdr;

            case State.ExpectingAdd:
                if (TryHandleExpectingAdd(instruction))
                    return;

                // TODO: Check register overwrite.
                // goto default;
                break;

            case State.LookingForPossibleLdr:
                if (TryHandleLookingForPossibleLdr(instruction))
                    return;

                goto default;

            default:
                ResetIfTrackedRegisterIsOverwritten(instruction);
                break;
        }
    }

    // TODO: Include ExpectingAdd check. (ADRP instruction without ADD)
    public bool HasValue => _state == State.ExpectingMovk || _state == State.LookingForPossibleLdr;

    public long Value => _value;

    public Arm64RegisterId RegisterId => _registerId;

    private bool TryHandleLookingForPattern(Arm64Instruction instruction)
    {
        switch (instruction.Id)
        {
            case Arm64InstructionId.ARM64_INS_MOVZ:
                StartMovzSequence(instruction);
                return true;
            case Arm64InstructionId.ARM64_INS_ADRP:
                StartAdrpSequence(instruction);
                return true;
            default:
                return false;
        }
    }

    private bool TryHandleExpectingMovk(Arm64Instruction instruction)
    {
        if (instruction.Id != Arm64InstructionId.ARM64_INS_MOVK)
            return false;

        if (instruction.GetRegisterId(0) != _registerId)
            return false;

        // TODO: Remove this condition because MOVK accept only LSL.
        var details = instruction.Details;
        if (instruction.GetShiftOperation(1) != Arm64ShiftOperation.ARM64_SFT_LSL)
            return false;

        if (instruction.GetShiftAmount(1) != _expectedMovkShift)
            return false;

        // TODO: Clear existing 16-bits values before setting MOVK immediate value.
        _value |= instruction.GetShiftedImmediateValue(1);
        _expectedMovkShift += 16;
        return true;
    }

    /// <summary>
    /// Handle following expecting ADD instruction that source/target registers match value tracked register.
    ///   ADD Xd|SP, Xn|SP, #imm, {shift}
    ///   ADD Wd|SP, Wn|SP, #imm, {shift}
    /// If condition is not matched. skip processing. and wait succeeding ADD instruction. 
    /// </summary>
    private bool TryHandleExpectingAdd(Arm64Instruction instruction)
    {
        if (instruction.Id != Arm64InstructionId.ARM64_INS_ADD)
            return false;

        if (instruction.GetRegisterId(0) != _registerId)
            return false;

        if (instruction.GetRegisterId(1) != _registerId)
            return false;

        if (!instruction.TryGetOperand(2, Arm64OperandType.Immediate, out var operand))
            return false;

        // TODO: Replace to use shifted immediate value.
        _value += operand.Immediate;
        _state = State.LookingForPossibleLdr;
        return true;
    }

    private bool TryHandleLookingForPossibleLdr(Arm64Instruction instruction)
    {
        Arm64InstructionDetail details = instruction.Details;

        if (TryHandleLdrFromTrackedRegister(instruction))
            return true;

        // TODO: Replace to AsmArm64 based IsConditionalBranch implementation
        if (IsConditionalBranch(instruction))
            return true;

        // TODO: Replace to AsmArm64 based IsUnconditionalControlFlow implementation
        if (IsUnconditionalControlFlow(instruction))
        {
            // We've encountered an unconditional jump or call, the accumulated registers value is not valid anymore
            // TODO: Call Reset() to clear internal state.
            _state = State.LookingForPattern;
            return true;
        }

        if (instruction.Id == Arm64InstructionId.ARM64_INS_MOVZ)
        {
            // Another constant loading is starting, reprocess it as a new pattern.
            _state = State.LookingForPattern;
            if (TryHandleLookingForPattern(instruction))
                return true;
        }

        return false;
    }

    private void StartMovzSequence(Arm64Instruction instruction)
    {
        _registerId = instruction.GetRegisterId(0);
        _value = instruction.GetRawImmediateValue(1); // TODO: Replace to shifted immediate value.
        _expectedMovkShift = 16;
        _state = State.ExpectingMovk;
    }

    private void StartAdrpSequence(Arm64Instruction instruction)
    {
        _registerId = instruction.GetRegisterId(0);
        _value = instruction.GetRawImmediateValue(1);// TODO: Replace to shifted immediate value.
        _state = State.ExpectingAdd;
    }

    private bool TryHandleLdrFromTrackedRegister(Arm64Instruction instruction)
    {
        if (!instruction.IsLdrFromTrackedRegister(_registerId))
            return false;

        // Simulate the LDR instruction.
        var newValue = (long)_runtime.DataTarget.DataReader.ReadPointer((ulong)_value);
        _value = newValue;
        if (_value == 0)
        {
            // TODO: Call Reset() to clear internal state.
            _state = State.LookingForPattern;
            return true;
        }

        // The LDR might have loaded the result in another register
        _registerId = instruction.GetRegisterId(0);
        return true;
    }

    // TODO: Remove this method and replace to AsmArm64 based implementation.
    private static bool IsConditionalBranch(Arm64Instruction instruction)
    {
        return instruction.Id == Arm64InstructionId.ARM64_INS_CBZ
            || instruction.Id == Arm64InstructionId.ARM64_INS_CBNZ
            || (instruction.Id == Arm64InstructionId.ARM64_INS_B && instruction.Details.ConditionCode != Arm64ConditionCode.Invalid);
    }

    // TODO: Remove this method and replace to AsmArm64 based implementation.
    private static bool IsUnconditionalControlFlow(Arm64Instruction instruction)
    {
        var details = instruction.Details;
        return details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE)
            || details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_CALL)
            || details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_JUMP);
    }

    private void ResetIfTrackedRegisterIsOverwritten(Arm64Instruction instruction)
    {
        // Finally check if the current instruction modified the register that was accumulating the constant
        // and reset the state machine in case it did.
        foreach (Arm64Register writtenRegister in instruction.Details.AllWrittenRegisters)
        {
            if (writtenRegister.Id != _registerId)
                continue;

            // Some unexpected instruction overwriting the accumulated register
            // TODO: Use Reset() to clear internal state.
            _state = State.LookingForPattern;
        }
    }
}
