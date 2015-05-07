// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Writes to the <see cref="Stream"/> using the supplied <see cref="Encoding"/>.
    /// It does not write the BOM and also does not close the stream.
    /// </summary>
    public class HttpResponseStreamWriter : TextWriter
    {
        private const int DefaultBufferSize = 1024;
        private readonly Stream _stream;
        private Encoder _encoder;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private int _charBufferSize;
        private int _charBufferCount;

        public HttpResponseStreamWriter(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize)
        {
        }

        public HttpResponseStreamWriter([NotNull] Stream stream, [NotNull] Encoding encoding, int bufferSize)
        {
            this._stream = stream;
            Encoding = encoding;
            _encoder = encoding.GetEncoder();
            _charBufferSize = bufferSize;
            _charBuffer = new char[bufferSize];
            _byteBuffer = new byte[encoding.GetMaxByteCount(bufferSize)];
        }

        public override Encoding Encoding { get; }

        public override void Write(char value)
        {
            if (_charBufferCount == _charBufferSize)
            {
                Flush();
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override void Write(char[] values)
        {
            if (values == null)
            {
                return;
            }

            Write(values, 0, values.Length);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                return;
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    Flush();
                }

                int remaining = _charBufferSize - _charBufferCount;
                if (remaining > count)
                {
                    remaining = count;
                }

                Buffer.BlockCopy(
                    src: buffer,
                    srcOffset: index * sizeof(char),
                    dst: _charBuffer,
                    dstOffset: _charBufferCount * sizeof(char),
                    count: remaining * sizeof(char));

                _charBufferCount += remaining;
                index += remaining;
                count -= remaining;
            }
        }

        public override void Write(string value)
        {
            if (value != null)
            {
                int count = value.Length;
                int index = 0;
                while (count > 0)
                {
                    if (_charBufferCount == _charBufferSize)
                    {
                        Flush();
                    }

                    int remaining = _charBufferSize - _charBufferCount;
                    if (remaining > count)
                    {
                        remaining = count;
                    }

                    value.CopyTo(
                        sourceIndex: index,
                        destination: _charBuffer,
                        destinationIndex: _charBufferCount,
                        count: remaining);

                    _charBufferCount += remaining;
                    index += remaining;
                    count -= remaining;
                }
            }
        }

        public override async Task WriteAsync(char value)
        {
            if (_charBufferCount == _charBufferSize)
            {
                await FlushAsync();
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                return;
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushAsync();
                }

                int remaining = _charBufferSize - _charBufferCount;
                if (remaining > count)
                {
                    remaining = count;
                }

                Buffer.BlockCopy(
                    src: buffer,
                    srcOffset: index * sizeof(char),
                    dst: _charBuffer,
                    dstOffset: _charBufferCount * sizeof(char),
                    count: remaining * sizeof(char));

                _charBufferCount += remaining;
                index += remaining;
                count -= remaining;
            }
        }

        public override async Task WriteAsync(string value)
        {
            if (value != null)
            {
                int count = value.Length;
                int index = 0;
                while (count > 0)
                {
                    if (_charBufferCount == _charBufferSize)
                    {
                        await FlushAsync();
                    }

                    int charBufferFreeSpace = _charBufferSize - _charBufferCount;
                    if (charBufferFreeSpace > count)
                    {
                        charBufferFreeSpace = count;
                    }

                    value.CopyTo(
                        sourceIndex: index,
                        destination: _charBuffer,
                        destinationIndex: _charBufferCount,
                        count: charBufferFreeSpace);

                    _charBufferCount += charBufferFreeSpace;
                    index += charBufferFreeSpace;
                    count -= charBufferFreeSpace;
                }
            }
        }

        // Do not flush the stream on Close/Dispose, as this will cause response to be
        // sent in chunked encoding in case of Helios.
        // We however want to flush the stream when Flush/FlushAsync is explicitly
        // called by the user (example: from a Razor view).

        public override void Flush()
        {
            Flush(true, true);
        }

        public override async Task FlushAsync()
        {
            await FlushAsync(true, true);
        }

#if DNX451
        public override void Close()
        {
            Flush(flushStream: false, flushEncoder: true);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            Flush(flushStream: false, flushEncoder: true);
        }

        private void Flush(bool flushStream = false, bool flushEncoder = false)
        {
            if (_charBufferCount == 0)
            {
                return;
            }

            int count = _encoder.GetBytes(_charBuffer, 0, _charBufferCount, _byteBuffer, 0, flushEncoder);
            if (count > 0)
            {
                _stream.Write(_byteBuffer, 0, count);
            }

            _charBufferCount = 0;

            if (flushStream)
            {
                _stream.Flush();
            }
        }

        private async Task FlushAsync(bool flushStream = false, bool flushEncoder = false)
        {
            if (_charBufferCount == 0)
            {
                return;
            }

            int count = _encoder.GetBytes(_charBuffer, 0, _charBufferCount, _byteBuffer, 0, flushEncoder);
            if (count > 0)
            {
                await _stream.WriteAsync(_byteBuffer, 0, count);
            }

            _charBufferCount = 0;

            if (flushStream)
            {
                await _stream.FlushAsync();
            }
        }
    }
}

