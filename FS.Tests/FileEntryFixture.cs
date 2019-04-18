using System;
using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class FileEntryFixture
    {
        private Mock<IDirectoryCache> directoryCache;
        private Mock<IFile> file;

        [SetUp]
        public void SetUp()
        {
            directoryCache = new Mock<IDirectoryCache>();

            file = new Mock<IFile>();
            file.SetupGet(x => x.BlockId).Returns(123);
        }

        [Test]
        public void ShouldThrowWhenDirectoryCacheIsNull()
        {
            // Given
            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate
            {
                // ReSharper disable once ObjectCreationAsStatement
                new FileEntry(null, file.Object);
            });
        }

        [Test]
        public void ShouldThrowWhenFileIsNull()
        {
            // Given
            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate
            {
                // ReSharper disable once ObjectCreationAsStatement
                new FileEntry(directoryCache.Object, null);
            });
        }

        [Test]
        public void ShouldDispose()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Dispose();

            // Then
            directoryCache.Verify(x => x.UnRegisterFile(123), Times.Once);
        }

        [Test]
        public void ShouldNotUnRegisterTwiceWhenDispose()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            directoryCache.Invocations.Clear();

            // When
            instance.Dispose();

            // Then
            directoryCache.Verify(x => x.UnRegisterDirectory(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void ShouldNotFlushWhenDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.Flush(); });
        }

        [Test]
        public void ShouldFlush()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Flush();

            // Then
            file.Verify(x => x.Flush(), Times.Once);
        }

        [Test]
        public void ShouldNotReadWhenDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.Read(0, new byte[1]); });
        }

        [Test]
        public void ShouldNotReadWhenBufferNull()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate { instance.Read(0, null); });
        }

        [Test]
        public void ShouldNotReadWhenBufferIsEmpty()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.Read(0, new byte[0]); });
        }

        [Test]
        public void ShouldNotReadIfPositionNegative()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentOutOfRangeException>(delegate { instance.Read(-1, new byte[10]); });
        }

        [Test]
        [TestCase(10, 0, 11)]
        [TestCase(10, 5, 6)]
        [TestCase(10, 11, 1)]
        public void ShouldNotReadIfPositionOutOfFileSize(int fileSize, int position, int bufferLength)
        {
            // Given
            var instance = CreateInstance();
            file.SetupGet(x => x.Size).Returns(fileSize);

            // When
            // Then
            Assert.Throws<ArgumentOutOfRangeException>(delegate { instance.Read(position, new byte[bufferLength]); });
        }

        [Test]
        public void ShouldRead()
        {
            //Given
            var instance = CreateInstance();
            file.SetupGet(x => x.Size).Returns(512);
            var buffer = new byte[12];

            // When
            instance.Read(321, buffer);

            // Then
            file.Verify(x => x.Read(321, buffer), Times.Once);
        }

        [Test]
        public void ShouldNotWriteWhenDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.Write(0, new byte[1]); });
        }

        [Test]
        public void ShouldNotWriteWhenBufferNull()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate { instance.Write(0, null); });
        }

        [Test]
        public void ShouldNotWriteWhenBufferIsEmpty()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.Write(0, new byte[0]); });
        }

        [Test]
        public void ShouldNotWriteIfPositionNegative()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentOutOfRangeException>(delegate { instance.Write(-1, new byte[10]); });
        }

        [Test]
        [TestCase(10, 0, 11)]
        [TestCase(10, 5, 6)]
        [TestCase(10, 11, 1)]
        public void ShouldNotWriteIfPositionOutOfFileSize(int fileSize, int position, int bufferLength)
        {
            // Given
            var instance = CreateInstance();
            file.SetupGet(x => x.Size).Returns(fileSize);

            // When
            // Then
            Assert.Throws<ArgumentOutOfRangeException>(delegate { instance.Write(position, new byte[bufferLength]); });
        }

        [Test]
        public void ShouldWrite()
        {
            //Given
            var instance = CreateInstance();
            file.SetupGet(x => x.Size).Returns(512);
            var buffer = new byte[12];

            // When
            instance.Write(321, buffer);

            // Then
            file.Verify(x => x.Write(321, buffer), Times.Once);
        }

        [Test]
        public void ShouldReturnSize()
        {
            // Given
            var instance = CreateInstance();
            file.SetupGet(x => x.Size).Returns(511);

            // When
            var result = instance.Size;

            // Then
            Assert.AreEqual(511, result);
        }

        [Test]
        public void ShouldNotSetSizeWhenDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.SetSize(123); });
        }

        [Test]
        public void ShouldNotSetSizeIfSizeNegative()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentOutOfRangeException>(delegate { instance.SetSize(-1); });
        }

        [Test]
        public void ShouldSetSize()
        {
            //Given
            var instance = CreateInstance();

            // When
            instance.SetSize(12345);

            // Then
            file.Verify(x => x.SetSize(12345), Times.Once);
        }

        private FileEntry CreateInstance()
        {
            return new FileEntry(directoryCache.Object, file.Object);
        }
    }
}
