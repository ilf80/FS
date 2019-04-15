using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class DeletionFileFixture
    {
        private Mock<IDirectoryCache> directoryCache;
        private Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>> indexBlockProviderFactory;
        private Mock<IFactory<IIndex<byte>, IIndexBlockProvider, ICommonAccessParameters>> indexFactory;
        private IFileParameters fileParameters;
        private Mock<IIndexBlockProvider> indexBlockProvider;
        private Mock<IIndex<byte>> index;

        [SetUp]
        public void SetUp()
        {
            directoryCache = new Mock<IDirectoryCache>();
            indexBlockProvider = new Mock<IIndexBlockProvider>();
            indexBlockProviderFactory = new Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>>();
            indexBlockProviderFactory.Setup(x => x.Create(It.IsAny<int>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(indexBlockProvider.Object);
            index = new Mock<IIndex<byte>>();
            indexFactory = new Mock<IFactory<IIndex<byte>, IIndexBlockProvider, ICommonAccessParameters>>();
            indexFactory.Setup(x => x.Create(It.IsAny<IIndexBlockProvider>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(index.Object);
            fileParameters = Mock.Of<IFileParameters>(x => x.BlockId == 123 && x.ParentDirectoryBlockId == 321);
        }

        [Test]
        public void ShouldDelete()
        {
            // Given
            var instance = CreateInstance();
            var directory = new Mock<IDirectory>();
            directory.SetupGet(x => x.BlockId).Returns(321);

            var allocationManager = new Mock<IAllocationManager>();

            directoryCache.Setup(x => x.ReadDirectory(321)).Returns(directory.Object);
            directoryCache.SetupGet(x => x.AllocationManager).Returns(allocationManager.Object);

            // When
            instance.Delete();

            // Then
            directory.Verify(x => x.UpdateEntry(123,
                    It.Is<IDirectoryEntryInfoOverrides>(y =>
                        y.Flags == (DirectoryFlags.File | DirectoryFlags.Deleted))),
                Times.Once);
            directoryCache.Verify(x => x.UnRegisterDirectory(321), Times.Once);
            directoryCache.Verify(x => x.UnRegisterFile(123), Times.Once);
            index.Verify(x => x.SetSizeInBlocks(0), Times.Once);
            allocationManager.Verify(x => x.Release(It.Is<int[]>(y => Helpers.CollectionsAreEqual(new[] {123}, y))),
                Times.Once);
        }

        private DeletionFile CreateInstance()
        {
            return new DeletionFile(directoryCache.Object, indexBlockProviderFactory.Object, indexFactory.Object, fileParameters);
        }
    }
}
