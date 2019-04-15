using System;
using System.Linq;
using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    public sealed class AllocationManagerFixture
    {
        private Mock<IFactory<IIndex<int>, IAllocationManager>> indexFactory;
        private Mock<IBlockStorage> storage;
        private Mock<IFactory<IBlockStream<int>, IIndex<int>>> blockStreamFactory;
        private int freeSpaceBlocksCount;
        private Mock<IIndex<int>> index;
        private Mock<IBlockStream<int>> blockStream;

        [SetUp]
        public void SetUp()
        {
            storage = new Mock<IBlockStorage>();

            index = new Mock<IIndex<int>>();
            indexFactory = new Mock<IFactory<IIndex<int>, IAllocationManager>>();
            indexFactory.Setup(x => x.Create(It.IsAny<IAllocationManager>())).Returns(index.Object);

            blockStream = new Mock<IBlockStream<int>>();
            blockStreamFactory = new Mock<IFactory<IBlockStream<int>, IIndex<int>>>();
            blockStreamFactory.Setup(x => x.Create(It.IsAny<IIndex<int>>())).Returns(blockStream.Object);
        }

        [Test]
        public void ShouldAllocateFromStorageIfNoSpaceInIndex()
        {
            // Given
            freeSpaceBlocksCount = 0;
            var instance = CreateInstance();
            var buffer = Enumerable.Range(1, 10).ToArray();
            storage.Setup(x => x.Extend(10)).Returns(buffer);

            // When
            var result = instance.Allocate(10);

            // Then
            CollectionAssert.AreEqual(buffer, result);

        }

        [Test]
        public void ShouldAllocateFromIndex()
        {
            // Given
            freeSpaceBlocksCount = 10;

            var instance = CreateInstance();
            var buffer = Enumerable.Range(1, 10).ToArray();

            blockStream.Setup(x => x.Read(10 - 5, It.IsAny<int[]>())).
                Callback((int position, int[] tmp) => Array.Copy(buffer, position, tmp, 0, tmp.Length));

            // When
            var result = instance.Allocate(5);

            // Then
            CollectionAssert.AreEqual(new[] { 6, 7, 8, 9, 10}, result);
            blockStream.Verify(x => x.Write(10 - 5, It.Is<int[]>(y => Helpers.CollectionsAreEqual(new int[5], y))));

        }

        [Test]
        public void ShouldAllocateFromStoreAndIndex()
        {
            // Given
            freeSpaceBlocksCount = 2;

            var instance = CreateInstance();
            var buffer = Enumerable.Range(1, 2).ToArray();

            blockStream.Setup(x => x.Read(0, It.IsAny<int[]>())).
                Callback((int position, int[] tmp) => Array.Copy(buffer, tmp, tmp.Length));

            var storageBuffer = Enumerable.Range(10, 3).ToArray();
            storage.Setup(x => x.Extend(3)).Returns(storageBuffer);

            // When
            var result = instance.Allocate(5);

            // Then
            CollectionAssert.AreEqual(new[] { 1, 2, 10, 11, 12 }, result);
            blockStream.Verify(x => x.Write(0, It.Is<int[]>(y => Helpers.CollectionsAreEqual(new int[2], y))));
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
        }

        [Test]
        public void ShouldRelease()
        {
            // Given
            freeSpaceBlocksCount = 0;
            var instance = CreateInstance();
            blockStream.SetupGet(x => x.Provider).Returns(Mock.Of<IBlockProvider<int>>(y => y.BlockSize == 3));

            var blocks = Enumerable.Range(10, 10).ToArray();

            // When
            instance.Release(blocks);

            // Then
            index.Verify(x => x.SetSizeInBlocks(4), Times.Once);
            blockStream.Verify(x => x.Write(0, It.Is<int[]>(y => Helpers.CollectionsAreEqual(blocks, y))), Times.Once);
        }

        private AllocationManager CreateInstance()
        {
            return new AllocationManager(indexFactory.Object, blockStreamFactory.Object, storage.Object, freeSpaceBlocksCount);
        }
    }
}
