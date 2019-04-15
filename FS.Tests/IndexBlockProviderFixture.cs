using System;
using System.Linq;
using FS.Core;
using Moq;
using NUnit.Framework;

namespace FS.Tests
{
    [TestFixture]
    internal sealed class IndexBlockProviderFixture
    {
        [SetUp]
        public void SetUp()
        {
            rootBlockIndex = 123;

            storage = new Mock<IBlockStorage>();
            storage.SetupGet(x => x.IndexPageSize).Returns(3);
            storage.SetupGet(x => x.MaxItemsInIndexPage).Returns(2);


            allocationManager = new Mock<IAllocationManager>();
            accessParameters = Mock.Of<ICommonAccessParameters>(
                x => x.Storage == storage.Object && x.AllocationManager == allocationManager.Object);
        }

        private Mock<IBlockStorage> storage;
        private Mock<IAllocationManager> allocationManager;
        private ICommonAccessParameters accessParameters;
        private int rootBlockIndex;

        private IndexBlockProvider CreateInstance()
        {
            return new IndexBlockProvider(rootBlockIndex, accessParameters);
        }

        [Test]
        public void ShouldLoadAllIndexPages()
        {
            // Given
            var instance = CreateInstance();
            storage.Setup(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] {1, 2, 77}, buffer, 3));
            storage.Setup(x => x.ReadBlock(77, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] {3, 4, 0}, buffer, 3));

            // When
            var result = new int[2];
            instance.Read(0, result);

            // Then
            storage.Verify(x => x.ReadBlock(123, It.IsAny<int[]>()), Times.Exactly(1));
            storage.Verify(x => x.ReadBlock(77, It.IsAny<int[]>()), Times.Exactly(1));
            storage.Verify(x => x.ReadBlock(It.IsAny<int>(), It.IsAny<int[]>()), Times.Exactly(2));

            CollectionAssert.AreEqual(result, new[] {1, 2});
        }

        [Test]
        public void ShouldReadIndexBeforeGettingSize()
        {
            // Given
            var instance = CreateInstance();

            // When
            // ReSharper disable once UnusedVariable
            var result = instance.SizeInBlocks;

            // Then
            storage.Verify(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()), Times.Exactly(1));
        }

        [Test]
        public void ShouldReadIndexBeforeGettingUsedEntryCount()
        {
            // Given
            var instance = CreateInstance();

            // When
            // ReSharper disable once UnusedVariable
            var result = instance.UsedEntryCount;

            // Then
            storage.Verify(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()), Times.Exactly(1));
        }

        [Test]
        public void ShouldReadIndexBeforeRead()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Read(0, new int[1]);

            // Then
            storage.Verify(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()), Times.Exactly(1));
        }

        [Test]
        public void ShouldReadIndexBeforeSetSize()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.SetSizeInBlocks(1);

            // Then
            storage.Verify(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()), Times.Exactly(1));
        }

        [Test]
        public void ShouldReadIndexBeforeWrite()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Write(0, new int[1]);

            // Then
            storage.Verify(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()), Times.Exactly(1));
        }

        [Test]
        public void ShouldReadIndexCorrectly()
        {
            // Given
            var instance = CreateInstance();
            storage.Setup(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] {1, 2, 77}, buffer, 3));
            storage.Setup(x => x.ReadBlock(77, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] {3, 4, 0}, buffer, 3));

            // When
            var result = new int[2];
            instance.Read(1, result);

            // Then
            storage.Verify(x => x.ReadBlock(123, It.IsAny<int[]>()), Times.Exactly(1));
            storage.Verify(x => x.ReadBlock(77, It.IsAny<int[]>()), Times.Exactly(1));
            storage.Verify(x => x.ReadBlock(It.IsAny<int>(), It.IsAny<int[]>()), Times.Exactly(2));

            CollectionAssert.AreEqual(result, new[] {3, 4});
        }

        [Test]
        public void ShouldWriteAndFlushIndexCorrectly()
        {
            // Given
            var instance = CreateInstance();
            storage.Setup(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] { 1, 2, 77 }, buffer, 3));
            storage.Setup(x => x.ReadBlock(77, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] { 3, 4, 0 }, buffer, 3));

            // When
            var result = new[] { 6, 5 };
            instance.Write(0, result);
            instance.Flush();

            // Then
            storage.Verify(x => x.ReadBlock(123, It.IsAny<int[]>()), Times.Exactly(1));
            storage.Verify(x => x.ReadBlock(77, It.IsAny<int[]>()), Times.Exactly(1));
            storage.Verify(x => x.ReadBlock(It.IsAny<int>(), It.IsAny<int[]>()), Times.Exactly(2));

            storage.Verify(x => x.WriteBlock(rootBlockIndex, It.Is<int[]>(y => Helpers.CollectionsAreEqual(new[] { 6, 5, 77 }, y))),
                Times.Exactly(1));
            storage.Verify(x => x.WriteBlock(77, It.Is<int[]>(y => Helpers.CollectionsAreEqual(new[] { 3, 4, 0 }, y))),
                Times.Exactly(1));
        }

        [Test]
        public void ShouldNotFlushIfNoWrites()
        {
            // Given
            var instance = CreateInstance();
            storage.Setup(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] { 1, 2, 0 }, buffer, 3));

            var result = new int[2];
            instance.Read(0, result);

            // When
            instance.Flush();

            // Then
            storage.Verify(x => x.WriteBlock(It.IsAny<int>(), It.IsAny<int[]>()), Times.Never);
        }

        [Test]
        public void ShouldComputeUsedEntryCount()
        {
            // Given
            var instance = CreateInstance();
            storage.Setup(x => x.ReadBlock(rootBlockIndex, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] { 1, 2, 77 }, buffer, 3));
            storage.Setup(x => x.ReadBlock(77, It.IsAny<int[]>()))
                .Callback((int index, int[] buffer) => Array.Copy(new[] { 3, 0, 0 }, buffer, 3));

            // When
            var result = instance.UsedEntryCount;

            // Then
            Assert.AreEqual(3, result);
        }

        [Test]
        public void ShouldIncreaseSize()
        {
            // Given
            var instance = CreateInstance();
            var buffer = Enumerable.Range(1, 10).ToArray();
            allocationManager.Setup(x => x.Allocate(It.IsAny<int>()))
                .Returns(buffer);

            // When
            instance.SetSizeInBlocks(10);

            // Then
            allocationManager.Verify(x => x.Allocate(9), Times.Exactly(1));
            allocationManager.Verify(x => x.Allocate(It.IsAny<int>()), Times.Exactly(1));
        }

        [Test]
        public void ShouldCorrectlyReturnAllocatedIndexes()
        {
            // Given
            var instance = CreateInstance();
            var buffer = Enumerable.Range(1, 10).ToArray();
            allocationManager.Setup(x => x.Allocate(It.IsAny<int>()))
                .Returns(buffer);

            instance.SetSizeInBlocks(10);

            // When
            var result = new int[3];
            instance.Read(5, result);
            var count = instance.UsedEntryCount;

            // Then
            CollectionAssert.AreEqual(new[] { 0, 0, 6 }, result);
            Assert.AreEqual(20, count);
        }

        [Test]
        public void ShouldShrink()
        {
            // Given
            var instance = CreateInstance();
            var buffer = Enumerable.Range(1, 10).ToArray();
            allocationManager.Setup(x => x.Allocate(It.IsAny<int>()))
                .Returns(buffer);

            instance.SetSizeInBlocks(10);

            // When
            instance.SetSizeInBlocks(1);

            // Then
            allocationManager.Verify(x => x.Release(It.Is<int[]>(y => Helpers.CollectionsAreEqual(buffer, y))), Times.Exactly(1));
            allocationManager.Verify(x => x.Release(It.IsAny<int[]>()), Times.Exactly(1));
        }
    }
}