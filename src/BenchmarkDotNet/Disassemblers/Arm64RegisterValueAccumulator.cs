using Gee.External.Capstone.Arm64;
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
    private Arm64RegisterId _registerId;
    private IClrRuntime _runtime;

    public void Init(IClrRuntime runtime)
    {
        _runtime = runtime;
        Reset();
    }

    internal void Reset(
        State state = State.LookingForPattern,
        ulong value = 0,
        Arm64RegisterId registerId = Arm64RegisterId.Invalid)
    {
        _state = state;
        _addressValue = value;
        _addressOffset = 0;
        _registerId = registerId;
    }

    public void Feed(Arm64Instruction instruction)
    {
        switch (_state)
        {
            case State.LookingForPattern:
                if (TryHandleLookingForPattern(instruction))
                    return;
                break;

            case State.ExpectingMovk:
                if (TryHandleExpectingMovk(instruction))
                    return;

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
                if (TryHandleLookingForPattern(instruction))
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
                    TryHandleLookingForPattern(instruction);
                }
                return;
        }
    }

    public bool HasValue
    {
        get
        {
            // Value == 0 means invalid (negative combined clamps to 0, LDR resets on 0),
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

    public Arm64RegisterId RegisterId => _registerId;

    private bool TryHandleLookingForPattern(Arm64Instruction instruction)
    {
        switch (instruction.Id)
        {
            case Arm64InstructionId.ARM64_INS_MOVZ:
                return TryStartMovzSequence(instruction);
            case Arm64InstructionId.ARM64_INS_ADRP:
                return TryStartAdrpSequence(instruction);
            default:
                return false;
        }
    }

    private bool TryStartMovzSequence(Arm64Instruction instruction)
    {
        try
        {
            if (!instruction.TryGetOperand(0, Arm64OperandType.Register, out _))
                return false;

            if (!instruction.TryGetOperand(1, Arm64OperandType.Immediate, out _))
                return false;

            _registerId = instruction.GetRegisterId(0);
            _addressValue = instruction.GetUnsignedShiftedImmediateValue(1);
            _addressOffset = 0;
            _state = State.ExpectingMovk;
            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryStartAdrpSequence(Arm64Instruction instruction)
    {
        try
        {
            if (!instruction.TryGetOperand(0, Arm64OperandType.Register, out _))
                return false;

            if (!instruction.TryGetOperand(1, Arm64OperandType.Immediate, out _))
                return false;

            _registerId = instruction.GetRegisterId(0);
            _addressOffset = instruction.GetShiftedImmediateValue(1);
            _addressValue = 0;
            _state = State.ExpectingAdd;
            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryHandleExpectingMovk(Arm64Instruction instruction)
    {
        try
        {
            if (instruction.Id != Arm64InstructionId.ARM64_INS_MOVK)
                return false;

            if (instruction.GetRegisterId(0) != _registerId)
                return false;

            // Note: MOVK instruction accept only LSL shift operation.
            var shiftAmount = instruction.GetShiftAmount(1);
            if (shiftAmount is not (0 or 16 or 32 or 48))
                return false;

            var shiftedImmediateValue = instruction.GetUnsignedShiftedImmediateValue(1);
            const ulong mask = 0xFFFF;
            _addressValue &= ~(mask << shiftAmount); // Clear 16-bits of MOVK target
            _addressValue |= shiftedImmediateValue;  // Set shifted immediate value

            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryHandleExpectingAdd(Arm64Instruction instruction)
    {
        try
        {
            if (instruction.Id != Arm64InstructionId.ARM64_INS_ADD)
                return false;

            if (instruction.GetRegisterId(0) != _registerId)
                return false;

            if (instruction.GetRegisterId(1) != _registerId)
                return false;

            if (!instruction.TryGetOperand(2, Arm64OperandType.Immediate, out _))
                return false;

            var shiftedImmediateValue = instruction.GetUnsignedShiftedImmediateValue(2);
            _addressValue += shiftedImmediateValue;
            _state = State.LookingForPossibleLdr;
            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryHandleLdrFromTrackedRegister(Arm64Instruction instruction)
    {
        try
        {
            if (!instruction.IsLdrFromTrackedRegister(_registerId))
                return false;

            // Simulate the LDR instruction.
            // Use combined Value: after ADRP+ADD the effective address is _addressValue + _addressOffset.
            // Use the Try-pattern so an unreadable address drops tracking instead of aborting disassembly.
            if (Value == 0)
            {
                _state = State.LookingForPattern;
                return true;
            }

            if (!_runtime.DataTarget.DataReader.ReadPointer(Value, out ulong dereferenced))
            {
                _state = State.LookingForPattern;
                return true;
            }

            _addressValue = dereferenced;
            _addressOffset = 0;
            if (_addressValue == 0)
            {
                _state = State.LookingForPattern;
            }
            else
            {
                // The LDR might have loaded the result in another register
                _registerId = instruction.GetRegisterId(0);
            }

            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
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
        try
        {
            // Finally check if the current instruction modified the register that was accumulating the constant
            // and reset the state machine in case it did.
            var details = instruction.Details;
            if (details is null)
                return true; // Conservative: unknown writes invalidate tracking (avoid false HasValue).

            foreach (Arm64Register writtenRegister in details.AllWrittenRegisters)
            {
                if (writtenRegister.Id != _registerId)
                    continue;

                return true;
            }

            return false;
        }
        catch (Exception ex) when (IsDecodingException(ex) || ex is NullReferenceException)
        {
            // Conservative: on decoding failure assume overwritten so a stale value
            // is not reported as a false-positive ReferencedAddress.
            return true;
        }
    }

    /// <summary>
    /// Only decoding failures (missing/mistyped operands, unexpected shift/instruction)
    /// are treated as "not matched". Anything else (e.g. OOM) must propagate.
    /// </summary>
    private static bool IsDecodingException(Exception ex)
        => ex is ArgumentOutOfRangeException
            or ArgumentException
            or NotSupportedException
            or InvalidOperationException
            or UnreachableException;
}
