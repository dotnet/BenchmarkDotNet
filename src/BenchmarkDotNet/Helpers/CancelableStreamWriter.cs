using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Helpers;

internal sealed class CancelableStreamWriter : IDisposable
{
#if !NETSTANDARD2_0
    private readonly StreamWriter _writer;

    public bool AutoFlush
    {
        get => _writer.AutoFlush;
        set => _writer.AutoFlush = value;
    }

    public CancelableStreamWriter(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen = false)
        => _writer = new(stream, encoding, bufferSize, leaveOpen);

    public Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken)
        => _writer.WriteLineAsync(buffer, cancellationToken);

    public void Dispose()
        => _writer.Dispose();
#else
    // Impl copied from https://github.com/dotnet/runtime/blob/4f65fec40cc7115d13d4925e688750308f0968b3/src/libraries/System.Private.CoreLib/src/System/IO/StreamWriter.cs
    // slightly adjusted to work in netstandard2.0.

    private const int DefaultBufferSize = 1024;
    private const int MinBufferSize = 128;

    private readonly Stream _stream;
    private readonly Encoding _encoding;
    private readonly Encoder _encoder;
    private byte[]? _byteBuffer;
    private readonly char[] _charBuffer;
    private int _charPos;
    private int _charLen;
    private bool _autoFlush;
    private bool _haveWrittenPreamble;
    private readonly bool _closable;
    private bool _disposed;

    public bool AutoFlush
    {
        get => _autoFlush;
        set
        {
            _autoFlush = value;
            if (value)
            {
                Flush(true, false);
            }
        }
    }

    public CancelableStreamWriter(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen = false)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite) throw new ArgumentException("Stream is not writeable", nameof(stream));

        if (bufferSize == -1)
        {
            bufferSize = DefaultBufferSize;
        }
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        _stream = stream;
        _encoding = encoding ??= IpcHelper.UTF8NoBOM;
        _encoder = encoding.GetEncoder();
        if (bufferSize < MinBufferSize)
        {
            bufferSize = MinBufferSize;
        }

        _charBuffer = new char[bufferSize];
        _charLen = bufferSize;
        if (_stream.CanSeek && _stream.Position > 0)
        {
            _haveWrittenPreamble = true;
        }

        _closable = !leaveOpen;
    }

    ~CancelableStreamWriter()
        => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        try
        {
            if (!_disposed && disposing)
            {
                Flush(flushStream: true, flushEncoder: true);
            }
        }
        finally
        {
            CloseStreamFromDispose(disposing);
        }
    }

    private void CloseStreamFromDispose(bool disposing)
    {
        if (_closable && !_disposed)
        {
            try
            {
                if (disposing)
                {
                    _stream.Close();
                }
            }
            finally
            {
                _disposed = true;
                _charLen = 0;
            }
        }
    }

    public Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken)
        => WriteAsyncInternal(buffer, appendNewLine: false, cancellationToken);

    public Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken)
        => WriteAsyncInternal(buffer, appendNewLine: true, cancellationToken);

    public Task FlushAsync(CancellationToken cancellationToken)
        => FlushAsyncInternal(flushStream: true, flushEncoder: false, cancellationToken);

    private async Task WriteAsyncInternal(ReadOnlyMemory<char> source, bool appendNewLine, CancellationToken cancellationToken)
    {
        int copied = 0;
        while (copied < source.Length)
        {
            if (_charPos == _charLen)
            {
                await FlushAsyncInternal(flushStream: false, flushEncoder: false, cancellationToken).ConfigureAwait(false);
            }

            int n = Math.Min(_charLen - _charPos, source.Length - copied);
            Debug.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");

            source.Span.Slice(copied, n).CopyTo(new Span<char>(_charBuffer, _charPos, n));
            _charPos += n;
            copied += n;
        }

        if (appendNewLine)
        {
            var newLine = Environment.NewLine;
            for (int i = 0; i < newLine.Length; i++)
            {
                if (_charPos == _charLen)
                {
                    await FlushAsyncInternal(flushStream: false, flushEncoder: false, cancellationToken).ConfigureAwait(false);
                }

                _charBuffer[_charPos++] = newLine[i];
            }
        }

        if (_autoFlush)
        {
            await FlushAsyncInternal(flushStream: true, flushEncoder: false, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task FlushAsyncInternal(bool flushStream, bool flushEncoder, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_charPos == 0 && !flushStream && !flushEncoder)
        {
            return Task.CompletedTask;
        }

        return Core(flushStream, flushEncoder, cancellationToken);

        async Task Core(bool flushStream, bool flushEncoder, CancellationToken cancellationToken)
        {
            if (!_haveWrittenPreamble)
            {
                _haveWrittenPreamble = true;
                byte[] preamble = _encoding.GetPreamble();
                if (preamble.Length > 0)
                {
                    await _stream.WriteAsync(preamble, 0, preamble.Length, cancellationToken).ConfigureAwait(false);
                }
            }

            byte[] byteBuffer = _byteBuffer ??= new byte[_encoding.GetMaxByteCount(_charBuffer.Length)];

            int count = _encoder.GetBytes(_charBuffer, 0, _charPos, byteBuffer, 0, flushEncoder);
            _charPos = 0;
            if (count > 0)
            {
                await _stream.WriteAsync(byteBuffer, 0, count, cancellationToken).ConfigureAwait(false);
            }

            if (flushStream)
            {
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void Flush(bool flushStream, bool flushEncoder)
    {
        ThrowIfDisposed();

        if (_charPos == 0 && !flushStream && !flushEncoder)
        {
            return;
        }

        if (!_haveWrittenPreamble)
        {
            _haveWrittenPreamble = true;
            var preamble = _encoding.GetPreamble();
            if (preamble.Length > 0)
            {
                _stream.Write(preamble, 0, preamble.Length);
            }
        }

        byte[] byteBuffer = _byteBuffer ??= new byte[_encoding.GetMaxByteCount(_charBuffer.Length)];

        int count = _encoder.GetBytes(_charBuffer, 0, _charPos, byteBuffer, 0, flushEncoder);
        _charPos = 0;
        if (count > 0)
        {
            _stream.Write(byteBuffer, 0, count);
        }

        if (flushStream)
        {
            _stream.Flush();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            ThrowObjectDisposedException();
        }

        static void ThrowObjectDisposedException() => throw new ObjectDisposedException(nameof(CancelableStreamWriter));
    }
#endif
}
