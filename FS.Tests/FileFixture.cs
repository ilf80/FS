using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class FileFixture
    {
        private Mock<IDirectoryCache> directoryCache;
        private IFileParameters fileParameters;
        private Mock<IIndexBlockProvider> indexBlockProvider;
        private Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>> indexBlockProviderFactory;
        private Mock<IIndex<byte>> index;
        private Mock<IFactory<IIndex<byte>, IIndexBlockProvider, ICommonAccessParameters>> indexFactory;
        private Mock<IBlockStream<byte>> blockStream;
        private Mock<IFactory<IBlockStream<byte>, IBlockProvider<byte>>> blockStreamFactory;
        private Mock<IDirectory> directory;

        [SetUp]
        public void SetUp()
        {
            directoryCache = new Mock<IDirectoryCache>();

            directory = new Mock<IDirectory>();
            directory.SetupGet(x => x.BlockId).Returns(321);
            directoryCache.Setup(x => x.ReadDirectory(321)).Returns(directory.Object);

            fileParameters =
                Mock.Of<IFileParameters>(x => x.BlockId == 123 && x.ParentDirectoryBlockId == 321 && x.Size == 511);

            indexBlockProvider = new Mock<IIndexBlockProvider>();
            indexBlockProviderFactory = new Mock<IFactory<IIndexBlockProvider, int, ICommonAccessParameters>>();
            indexBlockProviderFactory.Setup(x => x.Create(123, It.IsAny<ICommonAccessParameters>()))
                .Returns(indexBlockProvider.Object);

            index = new Mock<IIndex<byte>>();
            index.SetupGet(x => x.BlockId).Returns(123);

            indexFactory = new Mock<IFactory<IIndex<byte>, IIndexBlockProvider, ICommonAccessParameters>>();
            indexFactory.Setup(x => x.Create(It.IsAny<IIndexBlockProvider>(), It.IsAny<ICommonAccessParameters>()))
                .Returns(index.Object);

            blockStream = new Mock<IBlockStream<byte>>();
            blockStreamFactory = new Mock<IFactory<IBlockStream<byte>, IBlockProvider<byte>>>();
            blockStreamFactory.Setup(x => x.Create(It.IsAny<IBlockProvider<byte>>())).Returns(blockStream.Object);
        }

        [Test]
        public void ShouldReturnBlockId()
        {
            // Given
            var instance = CreateInstance();

            // When
            var result = instance.BlockId;

            // Then
            Assert.AreEqual(123, result);
        }

        [Test]
        public void ShouldFlush()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Flush();

            // Then
            index.Verify(x => x.Flush(), Times.Once);
            directory.Verify(x => x.UpdateEntry(123, It.Is<IDirectoryEntryInfoOverrides>(y => y.Size != null && y.Updated != null)));
            directoryCache.Verify(x => x.UnRegisterDirectory(321), Times.Once);
        }

        [Test]
        public void ShouldRead()
        {
            // Given
            var instance = CreateInstance();
            var buffer = new byte[120];

            // When
            instance.Read(10, buffer);

            // Then
            blockStream.Verify(x => x.Read(10, buffer), Times.Once);
        }

        [Test]
        public void ShouldWrite()
        {
            // Given
            var instance = CreateInstance();
            var buffer = new byte[120];

            // When
            instance.Write(10, buffer);

            // Then
            blockStream.Verify(x => x.Write(10, buffer), Times.Once);
        }

        [Test]
        public void ShouldDispose()
        {
            // Given
            var instance = CreateInstance();

            // When
            // Then
            Assert.DoesNotThrow(delegate { instance.Dispose(); });
        }

        [Test]
        [TestCase(17, 2, 12)]
        [TestCase(17, 1, 17)]
        [TestCase(16, 1, 17)]
        [TestCase(0, 0, 17)]
        [TestCase(1024, 2, 512)]
        [TestCase(1025, 3, 512)]
        public void ShouldSetSize(int size, int sizeInBlocks, int blockSize)
        {
            // Given
            var instance = CreateInstance();
            index.SetupGet(x => x.BlockSize).Returns(blockSize);

            // When
            instance.SetSize(size);

            // Then
            Assert.AreEqual(size, instance.Size);
            index.Verify(x => x.SetSizeInBlocks(sizeInBlocks), Times.Once);
            directory.Verify(x => x.UpdateEntry(123, It.Is<IDirectoryEntryInfoOverrides>(y => y.Size == size && y.Updated != null)));
            directoryCache.Verify(x => x.UnRegisterDirectory(321), Times.Once);
        }

        private File CreateInstance()
        {
            return new File(directoryCache.Object, fileParameters, indexBlockProviderFactory.Object,
                indexFactory.Object, blockStreamFactory.Object);
        }

    }
}
