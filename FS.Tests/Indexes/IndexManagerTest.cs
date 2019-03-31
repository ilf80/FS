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
        public void ShouldLoadBeforeIncrease()
        {

        }

        private IndexManager CreateInstance()
        {
            return new IndexManager(taskFactory, allocationManager.Object, blockStorage.Object, blockIndex);
        }
    }
}
