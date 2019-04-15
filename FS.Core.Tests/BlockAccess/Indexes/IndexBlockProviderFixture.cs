using System;
using FakeItEasy;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.BlockAccess.Indexes;
using FS.Core.Api.Common;
using FS.Core.BlockAccess.Indexes;
using NUnit.Framework;

namespace FS.Core.Tests.BlockAccess.Indexes
{
    [TestFixture]
    internal sealed class IndexBlockProviderFixture
    {
        private IBlockStorage storage;
        private IAllocationManager allocationManager;
        private ICommonAccessParameters accessParameters;
        private int rootBlockIndex;
        private byte[] readBuffer;
        private byte[] readBuffer2;

        [SetUp]
        public void SetUp()
        {
            rootBlockIndex = 123;

            storage = A.Fake<IBlockStorage>();
            A.CallTo(() => storage.IndexPageSize).Returns(3);
            A.CallTo(() => storage.MaxItemsInIndexPage).Returns(2);
            

            allocationManager = A.Fake<IAllocationManager>();
            accessParameters = A.Fake<ICommonAccessParameters>();

            A.CallTo(() => accessParameters.Storage).Returns(storage);
            A.CallTo(() => accessParameters.AllocationManager).Returns(allocationManager);

            //A.CallTo(() => provider.Read(0, A<byte[]>._)).Invokes((int position, byte[] buffer) =>
            //{
            //    Array.Copy(readBuffer, buffer, readBuffer.Length);
            //});
            //A.CallTo(() => provider.Read(1, A<byte[]>._)).Invokes((int position, byte[] buffer) =>
            //{
            //    Array.Copy(readBuffer2, buffer, readBuffer2.Length);
            //});

            //readBuffer = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17};
            //readBuffer2 = new byte[] {21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37};
        }

        [Test]
        public void ShouldReadIndexBeforeGettingSize()
        {
            // Given
            var instance = CreateInstance();

            // When
            var result = instance.SizeInBlocks;

            // Then
            A.CallTo(() => storage.ReadBlock(rootBlockIndex, A<int[]>._)).MustHaveHappened(1, Times.Exactly);

        }

        [Test]
        public void ShouldReadIndexBeforeGettingUsedEntryCount()
        {
            // Given
            var instance = CreateInstance();

            // When
            var result = instance.UsedEntryCount;

            // Then
            A.CallTo(() => storage.ReadBlock(rootBlockIndex, A<int[]>._)).MustHaveHappened(1, Times.Exactly);

        }

        [Test]
        public void ShouldReadIndexBeforeRead()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Read(0, new int[1]);

            // Then
            A.CallTo(() => storage.ReadBlock(rootBlockIndex, A<int[]>._)).MustHaveHappened(1, Times.Exactly);

        }

        [Test]
        public void ShouldReadIndexBeforeWrite()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Write(0, new int[1]);

            // Then
            A.CallTo(() => storage.ReadBlock(rootBlockIndex, A<int[]>._)).MustHaveHappened(1, Times.Exactly);

        }

        [Test]
        public void ShouldReadIndexBeforeSetSize()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.SetSizeInBlocks(1);

            // Then
            A.CallTo(() => storage.ReadBlock(rootBlockIndex, A<int[]>._)).MustHaveHappened(1, Times.Exactly);

        }

        [Test]
        public void ShouldLoadAllIndexPages()
        {
            // Given
            var instance = CreateInstance();

            // When
            instance.Read(0, new int[1]);

            // Then
            A.CallTo(() => storage.ReadBlock(rootBlockIndex, A<int[]>._)).MustHaveHappened(1, Times.Exactly);

        }

        private IndexBlockProvider CreateInstance()
        {
            return new IndexBlockProvider(rootBlockIndex, accessParameters);

        }
    }
}
