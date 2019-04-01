using FS.Allocattion;
using FS.BlockStorage;
using FS.Indexes;
using FS.Tests.Utils;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace FS.Tests.Indexes
{
    [TestFixture]
    public class IndexManagerTest
    {
        const int MaxPageSize = 128;

        private TaskFactory taskFactory;
        private Mock<IAllocationManager> allocationManager;
        private Mock<IBlockStorage> blockStorage;
        private uint blockIndex;

        [SetUp]
        public void SetUp()
        {

            taskFactory = new TaskFactory(new TestTaskScheduler());
            allocationManager = new Mock<IAllocationManager>();
            blockStorage = new Mock<IBlockStorage>();
            blockIndex = 1;
        }

        [Test]
        public void ShouldCreate()
        {
            // Given
            // When
            // Then
            CreateInstance();
        }

        [Test]
        public async Task ShouldLoadBeforeIncrease()
        {
            // Given
            var instance = CreateInstance();
            ListItemBlockIndexes indexes = new ListItemBlockIndexes { Indexes = new uint[MaxPageSize] };
            blockStorage
                .Setup(x => x.ReadBlock<ListItemBlockIndexes>(blockIndex))
                .Returns(() => Task.FromResult(indexes));

            // When
            await instance.Increase(1);

            // Then
            blockStorage.Verify(x => x.ReadBlock<ListItemBlockIndexes>(blockIndex), Times.Once);
        }

        [Test]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public async Task ShouldRequestBlockWhenIncrease(int blockCount)
        {
            // Given
            var instance = CreateInstance();
            ListItemBlockIndexes indexes = new ListItemBlockIndexes { Indexes = new uint[MaxPageSize] };
            blockStorage
                .Setup(x => x.ReadBlock<ListItemBlockIndexes>(blockIndex))
                .Returns(() => Task.FromResult(indexes));

            // When
            await instance.Increase(blockCount);

            // Then
            allocationManager.Verify(x => x.Allocate(blockCount), Times.Once);
        }

        private IndexManager CreateInstance()
        {
            return new IndexManager(taskFactory, allocationManager.Object, blockStorage.Object, blockIndex);
        }
    }
}
