using AsmArm64;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Diagnostics;

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
    private ulong _addressValue;
    private long _addressOffset;
    private Arm64RegisterAny _trackedRegister;
    private IClrRuntime _runtime;

    private static readonly Arm64RegisterAny InvalidRegister = Arm64RegisterAny.Create(Arm64RegisterKind.Invalid, 0, Arm64RegisterVKind.Default, 0, 0);

    public void Init(IClrRuntime runtime)
    {
        _runtime = runtime;
        Reset();
    }

    internal void Reset(
        State state = State.LookingForPattern,
        ulong value = 0,
        Arm64RegisterAny register = default)
    {
        _state = state;
        _addressValue = value;
        _addressOffset = 0;
        _trackedRegister = register == default
            ? InvalidRegister
            : register;
    }

    public void Feed(Arm64Instruction instruction, ulong instructionAddress = 0)
    {
        switch (_state)
        {
            case State.LookingForPattern:
                if (TryHandleLookingForPattern(instruction, instructionAddress))
                    return;
                break;

            case State.ExpectingMovk:
                if (TryHandleExpectingMovk(instruction))
                    return;

                // MOVK instructions are expected to be placed after MOVZ instruction. 
                // If other instruction found on ExpectingMovk state.
                // Then state is transitioned to LookingForPossibleLdr.
                _state = State.LookingForPossibleLdr;
                goto case State.LookingForPossibleLdr;

            case State.ExpectingAdd:
                if (TryHandleExpectingAdd(instruction))
                    return;

                goto default;

            case State.LookingForPossibleLdr:
                if (TryHandleLdrFromTrackedRegister(instruction))
                    return;

                // Try handle MOVZ/ADRP for new constand load.
                if (TryHandleLookingForPattern(instruction, instructionAddress))
                    return;

                goto default;

            default:
                if (TryHandleConditionalInstruction(instruction))
                    return;

                // Reset accumulated state if instruction overwriting the accumulated register.
                if (IsTrackedRegisterOverwritten(instruction))
                {
                    Reset();

                    // Reprocess current instruction.
                    TryHandleLookingForPattern(instruction, instructionAddress);
                }
                return;
        }
    }

    public bool HasValue
    {
        get
        {
            // Value == 0 means invalid(negative combined clamps to 0, LDR resets on 0),
            // So it is excluded to avoid a useless address lookup in the disassembler.
            if (Value == 0)
                return false;

            switch (_state)
            {
                case State.ExpectingMovk:
                case State.LookingForPossibleLdr:
                case State.ExpectingAdd:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Gets accumulated address value.
    /// Address arithmetic wraps modulo 2^64, like hardware.
    /// A negative combined value is invalid as a real address, so 0 is returned
    /// (0 is already treated as invalid: LDR resets on 0, disassembler filters it).
    /// </summary>
    public ulong Value
    {
        get
        {
            if (_addressOffset < 0)
            {
                // Magnitude of the negative offset, safe even for long.MinValue.
                ulong magnitude = unchecked((ulong)(-(_addressOffset + 1)) + 1);
                if (_addressValue < magnitude)
                    return 0;
            }
            return unchecked(_addressValue + (ulong)_addressOffset);
        }
    }

    public Arm64RegisterAny TrackedRegister => _trackedRegister;

    private bool TryHandleLookingForPattern(Arm64Instruction instruction, ulong instructionAddress)
    {
        switch (instruction.Mnemonic)
        {
            // MOVZ is decoded as MOV instruction alias by default.
            case Arm64Mnemonic.MOV:
                var baseInstruction = instruction.AsBaseInstruction();
                if (baseInstruction.Mnemonic != Arm64Mnemonic.MOVZ)
                    goto default;

                return TryStartMovzSequence(baseInstruction);

            case Arm64Mnemonic.MOVZ:
                //return TryStartMovzSequence(instruction);
                throw new UnreachableException();

            case Arm64Mnemonic.ADRP:
                return TryStartAdrpSequence(instruction, instructionAddress);
            default:
                return false;
        }
    }

    private bool TryHandleExpectingMovk(Arm64Instruction instruction)
    {
        if (instruction.Mnemonic != Arm64Mnemonic.MOVK)
            return false;

        if (!instruction.TryGetOperand(0, out Arm64RegisterOperand registerOperand))
            return false;

        if (registerOperand.Value != _trackedRegister)
            return false;

        if (!instruction.TryGetOperand(1, out Arm64ImmediateOperand _))
            return false;

        var shiftAmount = 0;
        if (instruction.TryGetOperand(2, out Arm64ShiftOperand shiftOperand))
        {
            if (shiftOperand.ShiftKind != Arm64ShiftKind.LSL)
                return false; // MOVK only supports LSL shift kind. So this code path should never be hit.
            shiftAmount = shiftOperand.Amount;
        }

        if (shiftAmount is not (0 or 16 or 32 or 48))
            return false;

        if (!instruction.TryGetShiftedImmediateValueUnsigned(1, out var shiftedImmediateValue))
            return false;

        const ulong mask = 0xFFFF;
        _addressValue &= ~(mask << shiftAmount); // Clear 16-bits of MOVK target
        _addressValue |= shiftedImmediateValue;  // Set shifted immediate value

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
        if (instruction.Mnemonic != Arm64Mnemonic.ADD)
            return false;

        if (!instruction.TryGetOperand(0, out Arm64RegisterOperand registerOperand0) || registerOperand0.Value != _trackedRegister)
            return false;

        if (!instruction.TryGetOperand(1, out Arm64RegisterOperand registerOperand1) || registerOperand1.Value != _trackedRegister)
            return false;

        if (!instruction.TryGetOperand(2, out Arm64ImmediateOperand _))
            return false;

        if (!instruction.TryGetShiftedImmediateValueUnsigned(2, out var shiftedImmediateValue))
            return false;

        _addressValue += shiftedImmediateValue;
        _state = State.LookingForPossibleLdr;
        return true;
    }

    private bool TryStartMovzSequence(Arm64Instruction instruction)
    {
        if (!instruction.TryGetOperand(0, out Arm64RegisterOperand registerOperand))
            return false;

        if (!instruction.TryGetOperand(1, out Arm64ImmediateOperand _))
            return false;

        _trackedRegister = registerOperand.Value;

        if (!instruction.TryGetShiftedImmediateValueUnsigned(1, out var immediateValue))
            return false;

        _addressValue = immediateValue;
        _addressOffset = 0;
        _state = State.ExpectingMovk;
        return true;
    }

    private bool TryStartAdrpSequence(Arm64Instruction instruction, ulong instructionAddress)
    {
        if (!instruction.TryGetOperand(0, out Arm64RegisterOperand registerOperand)
         || !instruction.TryGetOperand(1, out Arm64LabelOperand labelOperand))
            return false;

        _trackedRegister = registerOperand.Value;

        // ADRP label offset is a page delta relative to the page containing the instruction,
        // so resolve it to an absolute address here.
        _addressValue = instruction.GetTargetAddressBase(instructionAddress);
        _addressOffset = labelOperand.Offset;
        _state = State.ExpectingAdd;
        return true;
    }

    private bool TryHandleLdrFromTrackedRegister(Arm64Instruction instruction)
    {
        if (!instruction.IsLdrFromTrackedRegister(_trackedRegister))
            return false;

        if (!instruction.TryGetOperand(0, out Arm64RegisterOperand registerOperand))
            return false;

        // Address 0 is never valid to dereference (e.g. `movz x0, #0` or clamped negative ADRP+ADD).
        if (Value == 0)
        {
            Reset();
            return true;
        }

        // Simulate the LDR instruction.
        // Use combined Value: after ADRP+ADD the effective address is _addressValue + _addressOffset.
        // Use the Try-pattern so an unreadable address drops tracking instead of aborting disassembly.
        if (!_runtime.DataTarget.DataReader.ReadPointer(Value, out ulong dereferenced))
        {
            Reset();
            return true;
        }

        _addressValue = dereferenced;
        _addressOffset = 0;

        if (_addressValue == 0)
        {
            Reset();
            return true;
        }

        // The LDR might have loaded the result in another register
        _trackedRegister = registerOperand.Value;
        return true;

    }

    private bool TryHandleConditionalInstruction(Arm64Instruction instruction)
    {
        if (instruction.IsConditionalBranch())
            return true;

        if (instruction.IsUnconditionalControlFlow())
        {
            Reset();
            return true;
        }

        return false;
    }

    private bool IsTrackedRegisterOverwritten(Arm64Instruction instruction)
    {
        // Finally check if the current instruction modified the register that was accumulating the constant
        // and reset the state machine in case it did.
        foreach (var writtenRegister in instruction.GetWrittenRegisters())
        {
            if (writtenRegister != _trackedRegister)
                continue;

            return true;
        }

        return false;
    }
}
