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
    /// Writes to the supplied <see cref="Stream"/> using the supplied <see cref="Encoding"/>.
    /// It does not write the BOM and also does not close the stream.
    /// </summary>
    public class HttpResponseStreamWriter : TextWriter
    {
        private const int DefaultBufferSize = 1024;
        private readonly Stream _stream;
        private readonly int _bufferSize;
        private byte[] _buffer;
        private int _numOfBytesWrittenToBuffer;

        public HttpResponseStreamWriter(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize)
        {
        }

        public HttpResponseStreamWriter([NotNull] Stream stream, [NotNull] Encoding encoding, int bufferSize)
        {
            _stream = stream;
            Encoding = encoding;
            _bufferSize = bufferSize;
        }

        public override Encoding Encoding { get; }

        public override void Write(char value)
        {
            var bytes = Encoding.GetBytes(new[] { value });
            WriteBytes(bytes);
        }

        public override void Write(char[] buffer)
        {
            var bytes = Encoding.GetBytes(buffer);
            WriteBytes(bytes);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            var bytes = Encoding.GetBytes(buffer, index, count);
            WriteBytes(bytes);
        }

        public override void Write(string value)
        {
            var bytes = Encoding.GetBytes(value);
            WriteBytes(bytes);
        }

        public override async Task WriteAsync(char value)
        {
            var bytes = Encoding.GetBytes(new[] { value });
            await WriteBytesAsync(bytes);
        }

        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            var bytes = Encoding.GetBytes(buffer, index, count);
            await WriteBytesAsync(bytes);
        }

        public override async Task WriteAsync(string value)
        {
            var bytes = Encoding.GetBytes(value);
            await WriteBytesAsync(bytes);
        }

        public override async Task FlushAsync()
        {
            await WriteRemainingBytesAsync();
        }

        public override void Flush()
        {
            WriteRemainingBytes();
        }

#if DNX451
        public override void Close()
        {
            WriteRemainingBytes();
        }
#endif

        protected override void Dispose(bool disposing)
        {
            //In CoreCLR this is equivalent to Close.
            WriteRemainingBytes();
        }

        private byte[] Buffer
        {
            get
            {
                if (_buffer == null)
                {
                    _buffer = new byte[_bufferSize];
                }

                return _buffer;
            }
        }

        private void WriteBytes(byte[] bytes)
        {
            if ((_numOfBytesWrittenToBuffer + bytes.Length) >= _bufferSize)
            {
                if (_numOfBytesWrittenToBuffer > 0)
                {
                    _stream.Write(Buffer, 0, _numOfBytesWrittenToBuffer);
                }

                _stream.Write(bytes, 0, bytes.Length);
                _numOfBytesWrittenToBuffer = 0;
            }
            else
            {
                CopyToBuffer(bytes);
            }
        }

        private async Task WriteBytesAsync(byte[] bytes)
        {
            if ((_numOfBytesWrittenToBuffer + bytes.Length) >= _bufferSize)
            {
                if (_numOfBytesWrittenToBuffer > 0)
                {
                    await _stream.WriteAsync(Buffer, 0, _numOfBytesWrittenToBuffer);
                }

                await _stream.WriteAsync(bytes, 0, bytes.Length);
                _numOfBytesWrittenToBuffer = 0;
            }
            else
            {
                CopyToBuffer(bytes);
            }
        }

        private void WriteRemainingBytes()
        {
            if (_numOfBytesWrittenToBuffer > 0)
            {
                _stream.Write(Buffer, 0, _numOfBytesWrittenToBuffer);
                _stream.Flush();
                _numOfBytesWrittenToBuffer = 0;
            }
        }

        private async Task WriteRemainingBytesAsync()
        {
            if (_numOfBytesWrittenToBuffer > 0)
            {
                await _stream.WriteAsync(Buffer, 0, _numOfBytesWrittenToBuffer);
                await _stream.FlushAsync();
                _numOfBytesWrittenToBuffer = 0;
            }
        }

        private void CopyToBuffer(byte[] bytes)
        {
            var bufferStartIndex = (_numOfBytesWrittenToBuffer == 0) ? 0 : _numOfBytesWrittenToBuffer;
            Array.Copy(bytes, 0, Buffer, bufferStartIndex, bytes.Length);
            _numOfBytesWrittenToBuffer = _numOfBytesWrittenToBuffer + bytes.Length;
        }
    }
}