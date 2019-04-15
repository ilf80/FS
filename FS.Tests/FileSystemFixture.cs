using System;
using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class FileSystemFixture
    {
        private Mock<IFactory<IFileSystemProvider>> providerFactory;

        private Mock<IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool>> directoryFactory;
        private Mock<IFileSystemProvider> provider;
        private Mock<IDirectoryEntry> directory;
        private IDirectoryCache directoryCache;
        private IDirectory rootDirectory;

        [SetUp]
        public void SetUp()
        {
            directoryCache = Mock.Of<IDirectoryCache>();
            rootDirectory = Mock.Of<IDirectory>();

            provider = new Mock<IFileSystemProvider>();
            provider.SetupGet(x => x.DirectoryCache).Returns(directoryCache);
            provider.SetupGet(x => x.RootDirectory).Returns(rootDirectory);

            providerFactory = new Mock<IFactory<IFileSystemProvider>>();
            providerFactory.Setup(x => x.Create()).Returns(provider.Object);

            directory = new Mock<IDirectoryEntry>();
            directoryFactory = new Mock<IFactory<IDirectoryEntry, IDirectoryCache, IDirectory, bool>>();
            directoryFactory.Setup(x => x.Create(It.IsAny<IDirectoryCache>(), It.IsAny<IDirectory>(), It.IsAny<bool>()))
                .Returns(directory.Object);
        }

        [Test]
        public void ShouldNotCreateIfProviderFactoryIsNull()
        {
            // Given
            // When
            // Then
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(delegate { new FileSystem(null, directoryFactory.Object); });
        }

        [Test]
        public void ShouldNotCreateIfDirectoryFactoryIsNull()
        {
            // Given
            // When
            // Then
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(delegate { new FileSystem(providerFactory.Object, null); });
        }

        [Theory]
        public void ShouldNotOpenIfNameIsNull(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<ArgumentNullException>( delegate {  instance.Open(null, mode); });
        }

        [Theory]
        public void ShouldNotOpenTwice(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Open("1", mode);

            // Then
            Assert.Throws<InvalidOperationException>(delegate { instance.Open("1", mode); });
        }

        [Theory]
        public void ShouldNotOpenIfDisposed(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Dispose();

            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.Open("1", mode); });
        }

        [Theory]
        public void ShouldDelegateOpen(OpenMode mode)
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Open("1", mode);

            // Then
            provider.Verify(x => x.Open("1", mode), Times.Once);
        }

        [Test]
        public void ShouldNotGetRootDirectoryIfDisposed()
        {
            // Given
            var instance = CreateInstance();
            instance.Open("1", OpenMode.Create);

            // When
            instance.Dispose();

            // Then
            Assert.Throws<ObjectDisposedException>(delegate { instance.GetRootDirectory(); });
        }

        [Test]
        public void ShouldNotGetRootDirectoryIfNotOpened()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.Throws<InvalidOperationException>(delegate { instance.GetRootDirectory(); });
        }

        [Test]
        public void ShouldGetRootDirectory()
        {
            // Given
            var instance = CreateInstance();
            instance.Open("Test", OpenMode.OpenExisting);

            // When
            var result = instance.GetRootDirectory();

            // Then
            Assert.AreEqual(directory.Object, result);
            directoryFactory.Verify(x => x.Create(directoryCache, rootDirectory, false), Times.Once);
        }

        private FileSystem CreateInstance()
        {
            return new FileSystem(providerFactory.Object, directoryFactory.Object);
        }
    }
}
