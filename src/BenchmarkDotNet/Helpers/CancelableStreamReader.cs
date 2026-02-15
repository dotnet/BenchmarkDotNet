using BenchmarkDotNet.Extensions;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace BenchmarkDotNet.Helpers;

internal sealed class CancelableStreamReader : IDisposable
{
#if NET7_0_OR_GREATER
    private readonly StreamReader _defaultReader;

    public CancelableStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize = -1, bool leaveOpen = false)
        => _defaultReader = new(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);

    public ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        => _defaultReader.ReadLineAsync(cancellationToken);

    public void Dispose()
        => _defaultReader.Dispose();
#else
    // Impl copied from https://github.com/dotnet/runtime/blob/407f9f1476709c3e5aea25511b330e5c1df13fb8/src/libraries/System.Private.CoreLib/src/System/IO/StreamReader.cs
    // slightly adjusted to work in netstandard2.0.

    private const int DefaultBufferSize = 1024;
    private const int MinBufferSize = 128;

    // Full Framework on Windows Arm runs on a compatibility layer rather than native.
    // This difference was observed to cause hangs with async stream APIs over TcpClient that doesn't happen on other native supported environments.
    // In that environment we change the async read to the default sync read via Task.Run.
    private readonly StreamReader _defaultReader;

    private readonly Stream _stream;
    private Encoding _encoding;
    private Decoder _decoder;
    private readonly byte[] _byteBuffer;
    private char[] _charBuffer;
    private int _charPos;
    private int _charLen;
    private int _byteLen;
    private int _bytePos;
    private int _maxCharsPerBuffer;
    private bool _disposed;
    private bool _detectEncoding;
    private bool _checkPreamble;
    private readonly bool _closable;

    public CancelableStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize = -1, bool leaveOpen = false)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("Stream is not readable", nameof(stream));

        if (Portability.RuntimeInformation.IsFullFrameworkCompatibilityLayer)
        {
            _defaultReader = new(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize == -1 ? DefaultBufferSize : bufferSize, leaveOpen);
            _closable = true;
            _stream = null!;
            _encoding = null!;
            _decoder = null!;
            _byteBuffer = null!;
            _charBuffer = null!;
            return;
        }

        if (bufferSize == -1)
        {
            bufferSize = DefaultBufferSize;
        }
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        _stream = stream;
        _encoding = encoding ??= Encoding.UTF8;
        _decoder = encoding.GetDecoder();
        if (bufferSize < MinBufferSize)
        {
            bufferSize = MinBufferSize;
        }

        _byteBuffer = new byte[bufferSize];
        _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
        _charBuffer = new char[_maxCharsPerBuffer];
        _detectEncoding = detectEncodingFromByteOrderMarks;

        int preambleLength = encoding.GetPreamble().Length;
        _checkPreamble = preambleLength > 0 && preambleLength <= bufferSize;

        _closable = !leaveOpen;
        _defaultReader = null!;
    }

    ~CancelableStreamReader()
        => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (_closable)
        {
            try
            {
                if (disposing)
                {
                    if (Portability.RuntimeInformation.IsFullFrameworkCompatibilityLayer)
                    {
                        _defaultReader.Dispose();
                    }
                    else
                    {
                        _stream.Close();
                    }
                }
            }
            finally
            {
                _charPos = 0;
                _charLen = 0;
            }
        }
    }

    public async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        if (Portability.RuntimeInformation.IsFullFrameworkCompatibilityLayer)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => _defaultReader.ReadLine(), cancellationToken).WaitAsync(cancellationToken);
        }

        if (_charPos == _charLen && (await ReadBufferAsync(cancellationToken).ConfigureAwait(false)) == 0)
        {
            return null;
        }

        string retVal;
        char[]? arrayPoolBuffer = null;
        int arrayPoolBufferPos = 0;

        do
        {
            char[] charBuffer = _charBuffer;
            int charLen = _charLen;
            int charPos = _charPos;

            Debug.Assert(charPos < charLen, "ReadBuffer returned > 0 but didn't bump _charLen?");

            int idxOfNewline = charBuffer.AsSpan(charPos, charLen - charPos).IndexOfAny('\r', '\n');
            if (idxOfNewline >= 0)
            {
                if (arrayPoolBuffer is null)
                {
                    retVal = new string(charBuffer, charPos, idxOfNewline);
                }
                else
                {
                    retVal = new string(arrayPoolBuffer, 0, arrayPoolBufferPos) + new string(charBuffer, charPos, idxOfNewline);
                    ArrayPool<char>.Shared.Return(arrayPoolBuffer);
                }

                charPos += idxOfNewline;
                char matchedChar = charBuffer[charPos++];
                _charPos = charPos;

                if (matchedChar == '\r')
                {
                    if (charPos < charLen || (await ReadBufferAsync(cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        if (_charBuffer[_charPos] == '\n')
                        {
                            _charPos++;
                        }
                    }
                }

                return retVal;
            }

            if (arrayPoolBuffer is null)
            {
                arrayPoolBuffer = ArrayPool<char>.Shared.Rent(charLen - charPos + 80);
            }
            else if ((arrayPoolBuffer.Length - arrayPoolBufferPos) < (charLen - charPos))
            {
                char[] newBuffer = ArrayPool<char>.Shared.Rent(checked(arrayPoolBufferPos + charLen - charPos));
                arrayPoolBuffer.AsSpan(0, arrayPoolBufferPos).CopyTo(newBuffer);
                ArrayPool<char>.Shared.Return(arrayPoolBuffer);
                arrayPoolBuffer = newBuffer;
            }
            charBuffer.AsSpan(charPos, charLen - charPos).CopyTo(arrayPoolBuffer.AsSpan(arrayPoolBufferPos));
            arrayPoolBufferPos += charLen - charPos;
        }
        while (await ReadBufferAsync(cancellationToken).ConfigureAwait(false) > 0);

        if (arrayPoolBuffer is not null)
        {
            retVal = new string(arrayPoolBuffer, 0, arrayPoolBufferPos);
            ArrayPool<char>.Shared.Return(arrayPoolBuffer);
        }
        else
        {
            retVal = string.Empty;
        }

        return retVal;
    }

    private async ValueTask<int> ReadBufferAsync(CancellationToken cancellationToken)
    {
        _charLen = 0;
        _charPos = 0;
        byte[] tmpByteBuffer = _byteBuffer;
        Stream tmpStream = _stream;

        if (!_checkPreamble)
        {
            _byteLen = 0;
        }

        bool eofReached = false;

        do
        {
            if (_checkPreamble)
            {
                Debug.Assert(_bytePos <= _encoding.GetPreamble().Length, "possible bug in _compressPreamble. Are two threads using this StreamReader at the same time?");
                int tmpBytePos = _bytePos;
                int len = await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos, cancellationToken).ConfigureAwait(false);
                Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                if (len == 0)
                {
                    eofReached = true;
                    break;
                }

                _byteLen += len;
            }
            else
            {
                Debug.Assert(_bytePos == 0, "_bytePos can be non zero only when we are trying to _checkPreamble. Are two threads using this StreamReader at the same time?");
                _byteLen = await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length, cancellationToken).ConfigureAwait(false);
                Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  Bug in stream class.");

                if (_byteLen == 0)
                {
                    eofReached = true;
                    break;
                }
            }

            if (IsPreamble())
            {
                continue;
            }

            if (_detectEncoding && _byteLen >= 2)
            {
                DetectEncoding();
            }

            Debug.Assert(_charPos == 0 && _charLen == 0, "We shouldn't be trying to decode more data if we made progress in an earlier iteration.");
            _charLen = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0, flush: false);
        } while (_charLen == 0);

        if (eofReached)
        {
            Debug.Assert(_charPos == 0 && _charLen == 0, "We shouldn't be looking for EOF unless we have an empty char buffer.");
            _charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: true);
            _bytePos = 0;
            _byteLen = 0;
        }

        return _charLen;
    }

    private bool IsPreamble()
    {
        if (!_checkPreamble)
        {
            return false;
        }

        return IsPreambleWorker();

        bool IsPreambleWorker()
        {
            Debug.Assert(_checkPreamble);
            ReadOnlySpan<byte> preamble = _encoding.GetPreamble();

            Debug.Assert(_bytePos < preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?");
            int len = Math.Min(_byteLen, preamble.Length);

            for (int i = _bytePos; i < len; i++)
            {
                if (_byteBuffer[i] != preamble[i])
                {
                    _bytePos = 0;
                    _checkPreamble = false;
                    return false;
                }
            }
            _bytePos = len;

            Debug.Assert(_bytePos <= preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");

            if (_bytePos == preamble.Length)
            {
                CompressBuffer(preamble.Length);
                _bytePos = 0;
                _checkPreamble = false;
                _detectEncoding = false;
            }

            return _checkPreamble;
        }
    }

    private void DetectEncoding()
    {
        Debug.Assert(_byteLen >= 2, "Caller should've validated that at least 2 bytes were available.");

        byte[] byteBuffer = _byteBuffer;
        _detectEncoding = false;
        bool changedEncoding = false;

        ushort firstTwoBytes = BinaryPrimitives.ReadUInt16LittleEndian(byteBuffer);
        if (firstTwoBytes == 0xFFFE)
        {
            _encoding = Encoding.BigEndianUnicode;
            CompressBuffer(2);
            changedEncoding = true;
        }
        else if (firstTwoBytes == 0xFEFF)
        {
            if (_byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
            {
                _encoding = Encoding.Unicode;
                CompressBuffer(2);
                changedEncoding = true;
            }
            else
            {
                _encoding = Encoding.UTF32;
                CompressBuffer(4);
                changedEncoding = true;
            }
        }
        else if (_byteLen >= 3 && firstTwoBytes == 0xBBEF && byteBuffer[2] == 0xBF)
        {
            _encoding = Encoding.UTF8;
            CompressBuffer(3);
            changedEncoding = true;
        }
        else if (_byteLen >= 4 && firstTwoBytes == 0 && byteBuffer[2] == 0xFE && byteBuffer[3] == 0xFF)
        {
            _encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
            CompressBuffer(4);
            changedEncoding = true;
        }
        else if (_byteLen == 2)
        {
            _detectEncoding = true;
        }

        if (changedEncoding)
        {
            _decoder = _encoding.GetDecoder();
            int newMaxCharsPerBuffer = _encoding.GetMaxCharCount(byteBuffer.Length);
            if (newMaxCharsPerBuffer > _maxCharsPerBuffer)
            {
                _charBuffer = new char[newMaxCharsPerBuffer];
            }
            _maxCharsPerBuffer = newMaxCharsPerBuffer;
        }
    }

    private void CompressBuffer(int n)
    {
        Debug.Assert(_byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?");
        byte[] byteBuffer = _byteBuffer;
        _ = byteBuffer.Length;
        new ReadOnlySpan<byte>(byteBuffer, n, _byteLen - n).CopyTo(byteBuffer);
        _byteLen -= n;
    }
#endif
}
