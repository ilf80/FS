using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class UnsafeDirectoryReaderFixture
    {
        private Mock<IDirectoryCache> directoryCache;
        private Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>> indexBlockProviderFactory;
        private Mock<IFactory<IIndex<short>, IIndexBlockProvider, ICommonAccessParameters>> indexFactory;
        private Mock<IFactory<IBlockStream<short>, IBlockProvider<short>>> blockStreamFactory;
        private Mock<IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters>> directoryIndexFactory;
        private Mock<IFactory<IUnsafeDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader>> directoryFactory;
        private Mock<IFactory<IBlockStream<DirectoryItem>, IBlockProvider<DirectoryItem>>> directoryBlockStreamFactory;
        private Mock<IBlockStream<DirectoryItem>> directoryBlockStream;
        private Mock<IIndexBlockProvider> indexBlockProvider;
        private Mock<IIndex<short>> nameIndex;
        private Mock<IBlockStream<short>> nameBlockStream;
        private Mock<IAllocationManager> allocationManager;
        private Mock<IIndex<DirectoryItem>> directoryIndex;
        private Mock<IUnsafeDirectory> directory;


        [SetUp]
        public void SetUp()
        {
            directoryCache = new Mock<IDirectoryCache>();

            allocationManager = new Mock<IAllocationManager>();
            directoryCache.SetupGet(x => x.AllocationManager).Returns(allocationManager.Object);

            indexBlockProvider = new Mock<IIndexBlockProvider>();
            indexBlockProviderFactory = new Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>>();
            indexBlockProviderFactory.Setup(x => x.Create(It.IsAny<int>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(indexBlockProvider.Object);

            nameIndex = new Mock<IIndex<short>>();
            nameIndex.SetupGet(x => x.BlockSize).Returns(10);
            indexFactory = new Mock<IFactory<IIndex<short>, IIndexBlockProvider, ICommonAccessParameters>>();
            indexFactory.Setup(x => x.Create(It.IsAny<IIndexBlockProvider>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(nameIndex.Object);

            nameBlockStream = new Mock<IBlockStream<short>>();
            blockStreamFactory = new Mock<IFactory<IBlockStream<short>, IBlockProvider<short>>>();
            blockStreamFactory.Setup(x => x.Create(It.IsAny<IBlockProvider<short>>())).Returns(nameBlockStream.Object);

            directoryIndex = new Mock<IIndex<DirectoryItem>>();
            directoryIndexFactory = new Mock<IFactory<IIndex<DirectoryItem>, IIndexBlockProvider, ICommonAccessParameters>>();
            directoryIndexFactory
                .Setup(x => x.Create(It.IsAny<IIndexBlockProvider>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(directoryIndex.Object);

            directory = new Mock<IUnsafeDirectory>();
            directoryFactory =
                new Mock<IFactory<IUnsafeDirectory, IIndex<DirectoryItem>, IDirectoryCache, DirectoryHeader>>();
            directoryFactory
                .Setup(x => x.Create(It.IsAny<IIndex<DirectoryItem>>(), It.IsAny<IDirectoryCache>(),
                    It.IsAny<DirectoryHeader>())).Returns(directory.Object);

            directoryBlockStream = new Mock<IBlockStream<DirectoryItem>>();
            directoryBlockStreamFactory = new Mock<IFactory<IBlockStream<DirectoryItem>, IBlockProvider<DirectoryItem>>>();
            directoryBlockStreamFactory.Setup(x => x.Create(It.IsAny<IBlockProvider<DirectoryItem>>()))
                .Returns(directoryBlockStream.Object);
        }

        [Test]
        public void ShouldRead()
        {
            // Given
            var instance = CreateInstance();
            directoryBlockStream.Setup(x => x.Read(0, It.IsAny<DirectoryItem[]>()))
                .Callback(
                    (int index, DirectoryItem[] buffer) =>
                    {
                        buffer[0].Header.NameBlockIndex = 1;
                        buffer[0].Header.FirstEmptyItemOffset = 2;
                        buffer[0].Header.ItemsCount = 3;
                        buffer[0].Header.LastNameOffset = 4;
                        buffer[0].Header.ParentDirectoryBlockIndex = 5;
                    });

            // When
            var result = instance.Read(123);

            // Then
            Assert.AreEqual(directory.Object, result);
            indexBlockProviderFactory.Verify(x => x.Create(123, It.IsAny<ICommonAccessParameters>()), Times.Once);
            directoryBlockStream.Verify(x => x.Read(0, It.IsAny<DirectoryItem[]>()), Times.Once);
            directoryFactory.Verify(
                x => x.Create(directoryIndex.Object, directoryCache.Object,
                    It.Is<DirectoryHeader>(y =>
                        y.NameBlockIndex == 1 && y.FirstEmptyItemOffset == 2 && y.ItemsCount == 3 &&
                        y.LastNameOffset == 4 && y.ParentDirectoryBlockIndex == 5)), Times.Once);
        }

        private UnsafeDirectoryReader CreateInstance()
        {
            return new UnsafeDirectoryReader(directoryCache.Object, indexBlockProviderFactory.Object,
                directoryIndexFactory.Object, directoryFactory.Object, directoryBlockStreamFactory.Object);
        }
    }

}
