// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
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
        public async Task DoesNotClose_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new Mock<Stream>();
            stream.Setup(s => s.Close()).Verifiable();
            var writer = new HttpResponseStreamWriter(stream.Object, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello");
            writer.Close();

            // Assert
            stream.Verify(s => s.Close(), Times.Never());
        }

        [Fact]
        public async Task DoesNotDispose_UnderlyingStream_OnDisposingWriter()
        {
            // Arrange
            var stream = new Mock<Stream>();
            stream.Protected().Setup("Dispose", ItExpr.IsAny<bool>()).Verifiable();
            var writer = new HttpResponseStreamWriter(stream.Object, Encoding.UTF8);

            // Act
            await writer.WriteAsync("Hello world");
            writer.Dispose();

            // Assert
            stream.Protected().Verify("Dispose", Times.Never(), ItExpr.IsAny<bool>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(1023)]
        public async Task DoesNotWriteToStream_IfBufferIsNotFull(int byteLength)
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(memoryStream, Encoding.UTF8);

            // Act
            await writer.WriteAsync(new string('a', byteLength));

            // Assert
            Assert.Empty(memoryStream.ToArray());
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public async Task WritesToStream_IfBufferIsFull(int byteLength)
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(memoryStream, Encoding.UTF8);

            // Act
            await writer.WriteAsync(new string('a', byteLength));

            // Assert
            Assert.Equal(byteLength, memoryStream.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1023)]
        [InlineData(1050)]
        public async Task FlushesBuffer_OnClose(int byteLength)
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(memoryStream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
            writer.Close();

            // Assert
            Assert.Equal(byteLength, memoryStream.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1023)]
        [InlineData(1050)]
        public async Task FlushesBuffer_OnDispose(int byteLength)
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(memoryStream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
            writer.Dispose();

            // Assert
            Assert.Equal(byteLength, memoryStream.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1023)]
        [InlineData(1050)]
        public void FlushesBuffer_OnFlush(int byteLength)
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(memoryStream, Encoding.UTF8);
            writer.Write(new string('a', byteLength));

            // Act
            writer.Flush();

            // Assert
            Assert.Equal(byteLength, memoryStream.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1023)]
        [InlineData(1050)]
        public async Task FlushesBuffer_OnFlushAsync(int byteLength)
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var writer = new HttpResponseStreamWriter(memoryStream, Encoding.UTF8);
            await writer.WriteAsync(new string('a', byteLength));

            // Act
            await writer.FlushAsync();

            // Assert
            Assert.Equal(byteLength, memoryStream.Length);
        }
    }
}
