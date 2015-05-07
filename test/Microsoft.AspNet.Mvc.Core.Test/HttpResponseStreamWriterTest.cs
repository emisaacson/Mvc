// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpResponseStreamWriterTest
    {
        [Fact]
        public async Task DoesNotWriteBOM()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var encodingWithBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var writer = new HttpResponseStreamWriter(memoryStream, encodingWithBOM);
            var expectedData = new byte[] { 97, 98, 99, 100 }; // without BOM

            // Act
            using (writer)
            {
                await writer.WriteAsync("abcd");
            }

            // Assert
            Assert.Equal(expectedData, memoryStream.ToArray());
        }

        [Fact]
        public async Task DoesNotFlush_UnderlyingStream_OnClosingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello");
            writer.Close();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
        }

        [Fact]
        public async Task DoesNotFlush_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello");
            writer.Dispose();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
        }

        [Fact]
        public async Task DoesNotClose_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello");
            writer.Close();

            // Assert
            Assert.Equal(0, stream.CloseCallCount);
        }

        [Fact]
        public async Task DoesNotDispose_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello world");
            writer.Dispose();

            // Assert
            Assert.Equal(0, stream.DisposeCallCount);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task FlushesBuffer_OnClose(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
            writer.Close();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task FlushesBuffer_OnDispose(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
            writer.Dispose();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.FlushAsyncCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Fact]
        public void NoDataWritten_Flush_DoesNotFlushUnderlyingStream()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            writer.Flush();

            // Assert
            Assert.Equal(0, stream.FlushCallCount);
            Assert.Equal(0, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public void FlushesBuffer_OnFlush(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            writer.Write(new string('a', byteLength));

            // Act
            writer.Flush();

            // Assert
            Assert.Equal(1, stream.FlushCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Fact]
        public async Task NoDataWritten_FlushAsync_DoesNotFlushUnderlyingStream()
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            await writer.FlushAsync();

            // Assert
            Assert.Equal(0, stream.FlushAsyncCallCount);
            Assert.Equal(0, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task FlushesBuffer_OnFlushAsync(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
            await writer.FlushAsync();

            // Assert
            Assert.Equal(1, stream.FlushAsyncCallCount);
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public void WriteChar_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                for (var i = 0; i < byteLength; i++)
                {
                    writer.Write('a');
                }
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public void WriteCharArray_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                writer.Write((new string('a', byteLength)).ToCharArray());
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task WriteCharAsync_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                for (var i = 0; i < byteLength; i++)
                {
                    await writer.WriteAsync('a');
                }
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        [Theory]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1050)]
        [InlineData(2048)]
        public async Task WriteCharArrayAsync_WritesToStream(int byteLength)
        {
            // Arrange
            var stream = new TestMemoryStream();
            var writer = new HttpResponseStreamWriter(stream, Encoding.UTF8);

            // Act
            using (writer)
            {
                await writer.WriteAsync((new string('a', byteLength)).ToCharArray());
            }

            // Assert
            Assert.Equal(byteLength, stream.Length);
        }

        private class TestMemoryStream : MemoryStream
        {
            private int _flushCallCount;
            private int _flushAsyncCallCount;
            private int _closeCallCount;
            private int _disposeCallCount;

            public int FlushCallCount { get { return _flushCallCount; } }

            public int FlushAsyncCallCount { get { return _flushAsyncCallCount; } }

            public int CloseCallCount { get { return _closeCallCount; } }

            public int DisposeCallCount { get { return _disposeCallCount; } }

            public override void Flush()
            {
                _flushCallCount++;
                base.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                _flushAsyncCallCount++;
                return base.FlushAsync(cancellationToken);
            }

            public override void Close()
            {
                _closeCallCount++;
                base.Close();
            }

            protected override void Dispose(bool disposing)
            {
                _disposeCallCount++;
                base.Dispose(disposing);
            }
        }
    }
}
