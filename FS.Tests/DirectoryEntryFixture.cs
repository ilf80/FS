using System;
using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class DirectoryEntryFixture
    {
        private Mock<IDirectoryCache> directoryCache;
        private Mock<IDirectory> directory;
        private bool unRegisterDirectoryOnDispose;
        private Mock<IDirectoryEntry> newDirectory;
        private Mock<IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool>> directoryFactory;
        private Mock<IFileEntry> newFile;
        private Mock<IFactory<IFileEntry, IDirectoryCache, IFile>> fileFactory;

        [SetUp]
        public void SetUp()
        {
            unRegisterDirectoryOnDispose = false;

            directoryCache = new Mock<IDirectoryCache>();
            directory = new Mock<IDirectory>();
            directory.SetupGet(x => x.BlockId).Returns(123);
            directory.Setup(x => x.GetDirectoryEntries()).Returns(new[]
            {
                Mock.Of<IDirectoryEntryInfo>(y => y.IsDirectory && y.BlockId == 1 && y.Name == "Dir 1" ),
                Mock.Of<IDirectoryEntryInfo>(y => y.IsDirectory && y.BlockId == 2 && y.Name == "Dir 2" ),
                Mock.Of<IDirectoryEntryInfo>(y => y.IsDirectory == false && y.BlockId == 3 && y.Name == "File 1" ),
            });

            newDirectory = new Mock<IDirectoryEntry>();
            directoryFactory = new Mock<IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool>>();
            directoryFactory.Setup(x => x.Create(It.IsAny<IDirectoryCache>(), It.IsAny<IDirectory>(), It.IsAny<bool>()))
                .Returns(newDirectory.Object);

            newFile = new Mock<IFileEntry>();
            fileFactory = new Mock<IFactory<IFileEntry, IDirectoryCache, IFile>>();
            fileFactory.Setup(x => x.Create(It.IsAny<IDirectoryCache>(), It.IsAny<IFile>())).Returns(newFile.Object);
        }

        [Test]
        public void ShouldThrowIfDirectoryCacheIsNull()
        {
            // Given
            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate
            {
                // ReSharper disable once ObjectCreationAsStatement
                new DirectoryEntry(null, directory.Object, unRegisterDirectoryOnDispose,
                    directoryFactory.Object, fileFactory.Object);
            });
        }

        [Test]
        public void ShouldThrowIfDirectoryIsNull()
        {
            // Given
            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate
            {
                // ReSharper disable once ObjectCreationAsStatement
                new DirectoryEntry(directoryCache.Object, null, unRegisterDirectoryOnDispose,
                    directoryFactory.Object, fileFactory.Object);
            });
        }

        [Test]
        public void ShouldThrowIfDirectoryFactoryIsNull()
        {
            // Given
            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate
            {
                // ReSharper disable once ObjectCreationAsStatement
                new DirectoryEntry(directoryCache.Object, directory.Object, unRegisterDirectoryOnDispose,
                    null, fileFactory.Object);
            });
        }

        [Test]
        public void ShouldThrowIfFileFactoryIsNull()
        {
            // Given
            // When
            // Then
            Assert.Throws<ArgumentNullException>(delegate
            {
                // ReSharper disable once ObjectCreationAsStatement
                new DirectoryEntry(directoryCache.Object, directory.Object, unRegisterDirectoryOnDispose,
                    directoryFactory.Object, null);
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
            directoryCache.Verify(x => x.UnRegisterDirectory(123), Times.Never);
        }

        [Test]
        public void ShouldUnRegisterWhenDispose()
        {
            // Given
            unRegisterDirectoryOnDispose = true;
            var instance = CreateInstance();

            // When
            instance.Dispose();

            // Then
            directoryCache.Verify(x => x.UnRegisterDirectory(123), Times.Once);
        }

        [Test]
        public void ShouldNotTryGetEntryWhenDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            // ReSharper disable once UnusedVariable
            Assert.Throws<ObjectDisposedException>(delegate { instance.TryGetEntry("Test", out var entry); });
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ShouldNotTryGetEntryWhenNameIsNullOrEmpty(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            // ReSharper disable once UnusedVariable
            Assert.Throws<ArgumentException>(delegate { instance.TryGetEntry(name, out var entry); });
        }

        [Test]
        [TestCase("Dir 1")]
        [TestCase("Dir 2")]
        [TestCase("File 1")]
        public void ShouldTryGetEntryWhenNameReturnValue(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            var result = instance.TryGetEntry(name, out var entry);

            // Then
            Assert.IsTrue(result);
            Assert.AreEqual(name, entry.Name);
        }

        [Test]
        [TestCase("Dir 3")]
        [TestCase("File 2")]
        public void ShouldTryGetEntryWhenNameNotReturnValue(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            var result = instance.TryGetEntry(name, out var entry);

            // Then
            Assert.IsFalse(result);
            Assert.IsNull(entry);
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
            directory.Verify(x => x.Flush(), Times.Once);
        }

        [Theory]
        public void ShouldNotOpenDirectoryWhenDisposed(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.OpenDirectory("Test", mode); });
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ShouldNotOpenDirectoryWhenNameIsNullOrEmpty(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.OpenDirectory(name, OpenMode.Create); });
        }

        [Test]
        [TestCase(-1)]
        [TestCase(100)]
        public void ShouldNotOpenDirectoryWhenModeIsIncorrect(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.OpenDirectory("Test 1", mode); });
        }

        [Test]
        [TestCase("Dir 1", OpenMode.Create)]
        [TestCase("Dir 1", OpenMode.OpenOrCreate)]
        [TestCase("Dir 2", OpenMode.OpenOrCreate)]
        public void ShouldOpenDirectory(string name, OpenMode mode)
        {
            // Given
            var instance = CreateInstance();
            var openedDirectory = Mock.Of<IDirectory>();
            directory.Setup(x => x.OpenDirectory(name, mode)).Returns(openedDirectory);

            // When
            var result = instance.OpenDirectory(name, mode);

            // Then
            Assert.AreEqual(newDirectory.Object, result);
            directory.Verify(x => x.OpenDirectory(name, mode), Times.Once);
            directoryFactory.Verify(x => x.Create(directoryCache.Object, openedDirectory, true), Times.Once);
        }

        [Theory]
        public void ShouldNotOpenFileWhenDisposed(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.OpenFile("Test", mode); });
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ShouldNotOpenFileWhenNameIsNullOrEmpty(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.OpenFile(name, OpenMode.Create); });
        }

        [Test]
        [TestCase(-1)]
        [TestCase(100)]
        public void ShouldNotOpenFileWhenModeIsIncorrect(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.OpenFile("Test 1", mode); });
        }

        [Test]
        [TestCase("File 1", OpenMode.Create)]
        [TestCase("File 1", OpenMode.OpenOrCreate)]
        public void ShouldOpenFile(string name, OpenMode mode)
        {
            // Given
            var instance = CreateInstance();
            var openedFile = Mock.Of<IFile>();
            directory.Setup(x => x.OpenFile(name, mode)).Returns(openedFile);

            // When
            var result = instance.OpenFile(name, mode);

            // Then
            Assert.AreEqual(newFile.Object, result);
            directory.Verify(x => x.OpenFile(name, mode), Times.Once);
            fileFactory.Verify(x => x.Create(directoryCache.Object, openedFile), Times.Once);
        }

        [Test]
        public void ShouldNotDeleteFileWhenDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.DeleteFile("Test"); });
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ShouldNotDeleteFileWhenNameIsNullOrEmpty(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.DeleteFile(name); });
        }


        [Test]
        [TestCase("File 1")]
        [TestCase("File 2")]
        public void ShouldDeleteFile(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.DeleteFile(name);

            // Then
            directory.Verify(x => x.DeleteFile(name), Times.Once);
        }

        //
        [Test]
        public void ShouldNotDeleteDirectoryWhenDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Dispose();

            // When
            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.DeleteDirectory("Test"); });
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ShouldNotDeleteDirectoryWhenNameIsNullOrEmpty(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentException>(delegate { instance.DeleteDirectory(name); });
        }


        [Test]
        [TestCase("Dir 1")]
        [TestCase("Dir 2")]
        public void ShouldDeleteDirectory(string name)
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.DeleteDirectory(name);

            // Then
            directory.Verify(x => x.DeleteDirectory(name), Times.Once);
        }

        private DirectoryEntry CreateInstance()
        {
            return new DirectoryEntry(directoryCache.Object, directory.Object, unRegisterDirectoryOnDispose,
                directoryFactory.Object, fileFactory.Object);
        }
    }
}
