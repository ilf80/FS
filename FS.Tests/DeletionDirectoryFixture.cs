using System;
using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class DeletionDirectoryFixture
    {
        private Mock<IDirectoryCache> directoryCache;
        private int blockId;
        private Mock<IUnsafeDirectoryReader> unsafeDirectoryReader;
        private Mock<IFactory<IUnsafeDirectoryReader, IDirectoryCache>> unsafeDirectoryReaderFactory;

        [SetUp]
        public void SetUp()
        {
            blockId = 123;
            directoryCache = new Mock<IDirectoryCache>();

            unsafeDirectoryReader = new Mock<IUnsafeDirectoryReader>();

            unsafeDirectoryReaderFactory = new Mock<IFactory<IUnsafeDirectoryReader, IDirectoryCache>>();
            unsafeDirectoryReaderFactory.Setup(x => x.Create(directoryCache.Object)).Returns(unsafeDirectoryReader.Object);
        }

        [Test]
        public void ShouldDelete()
        {
            // Given
            var instance = CreateInstance();
            var directory = new Mock<IUnsafeDirectory>();
            unsafeDirectoryReader.Setup(x => x.Read(blockId)).Returns(directory.Object);

            // When
            instance.Delete();

            // Then
            directory.Verify(x => x.UnsafeDeleteDirectory(), Times.Once);
            directoryCache.Verify(x => x.UnRegisterDirectory(blockId));
        }

        [Test]
        public void ShouldNotDeleteIfNotEmpty()
        {
            // Given
            var instance = CreateInstance();
            var directory = new Mock<IUnsafeDirectory>();
            directory.Setup(x => x.GetDirectoryEntries()).Returns(new[] {Mock.Of<IDirectoryEntryInfo>()});
            unsafeDirectoryReader.Setup(x => x.Read(blockId)).Returns(directory.Object);

            // When
            Assert.Throws<InvalidOperationException>(delegate { instance.Delete(); });

            // Then
            directory.Verify(x => x.UnsafeDeleteDirectory(), Times.Never);
            directoryCache.Verify(x => x.UnRegisterDirectory(blockId));
        }

        private DeletionDirectory CreateInstance()
        {
            return new DeletionDirectory(directoryCache.Object, blockId, unsafeDirectoryReaderFactory.Object);
        }
    }
}
