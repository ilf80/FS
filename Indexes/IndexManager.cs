using FS.Allocattion;
using FS.BlockStorage;
using FS.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FS.Indexes
{
    internal class IndexManager : IIndexManager
    {
        private readonly TaskFactory taskFactory;
        private readonly IAllocationManager allocationManager;
        private readonly IBlockStorage storage;
        private readonly uint rootBlockIndex;
        private readonly ReaderWriterLockSlim indexLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly LinkedList<uint[]> indexList = new LinkedList<uint[]>();
        private int indexLoaded = 0;

        public IndexManager(
            TaskFactory taskFactory,
            IAllocationManager allocationManager,
            IBlockStorage storage,
            uint blockIndex)
        {
            this.taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.rootBlockIndex = blockIndex;
        }

        public Task Increase(int blockCount)
        {
            if (blockCount <= 0)
            {
                throw new ArgumentException($"{nameof(blockCount)} should be more than 0 but was {blockCount}");
            }

            return this.taskFactory
                .StartNew(EnsureLoaded)
                .ContinueWith(t => IncreaseInternal(blockCount));
        }

        public Task Shrink(int totalBlockCount)
        {
            if (totalBlockCount < 0)
            {
                throw new ArgumentException($"{nameof(totalBlockCount)} should be 0 or more than 0 but was {totalBlockCount}");
            }

            return this.taskFactory
                .StartNew(EnsureLoaded)
                .ContinueWith(t => ShrinkInternal(totalBlockCount), TaskContinuationOptions.ExecuteSynchronously);
        }

        public Task<uint[]> GetBlocksForOffset(int offset, int count)
        {
            return this.taskFactory
                .StartNew(EnsureLoaded)
                .ContinueWith(t => GetBlocksForOffsetInternal(offset, count));
        }

        public void Lock()
        {
            this.indexLock.EnterWriteLock();
        }

        public void Release()
        {
            this.indexLock.ExitWriteLock();
        }

        private async void EnsureLoaded()
        {
            if (Interlocked.CompareExchange(ref this.indexLoaded, 1, 1) == 0)
            {
                this.indexLock.EnterWriteLock();

                try
                {
                    if (Interlocked.CompareExchange(ref this.indexLoaded, 1, 1) == 0)
                    {
                        Load().Wait();

                        Interlocked.Exchange(ref this.indexLoaded, 1);
                    }
                }
                finally
                {
                    this.indexLock.ExitWriteLock();
                }
            }
        }

        private async Task Load()
        {
            uint[] index;
            for (var extentionBlockIndex = this.rootBlockIndex;
                extentionBlockIndex != Constants.EmptyBlockIndex;
                extentionBlockIndex = GetNextExtentionBlockIndex(index))
            {
                ListItemBlockIndexes blockIndexes = await this.storage.ReadBlock<ListItemBlockIndexes>(extentionBlockIndex);
                index = blockIndexes.Indexes;
                this.indexList.AddLast(index);
            }
        }

        private uint GetNextExtentionBlockIndex(uint[] index)
        {
            return index[index.Length - 1];
        }

        private uint[] GetBlocksForOffsetInternal(int offset, int count)
        {
            this.indexLock.EnterReadLock();
            try
            {
                GetPositionByOffset(offset, out int pageNo, out int pageOffset);
                return this.indexList.Skip(pageNo).Take(1).SelectMany(x => x).Skip(pageOffset).Take(count).ToArray();
            }
            finally
            {
                this.indexLock.ExitReadLock();
            }
        }

        private void GetPositionByOffset(int offset, out int pageNo, out int pageOffset)
        {
            var usedBlockCountInLastIndexPage = GetUsedBlockCountInLastIndexPage();
            pageNo = offset / Constants.MaxItemsInIndexPage;
            if (pageNo < this.indexList.Count)
            {
                pageOffset = offset % Constants.MaxItemsInIndexPage;
                if (pageOffset < usedBlockCountInLastIndexPage)
                {
                    return;
                }
            }

            var totalSize = GetTotalSize();
            throw new IndexOutOfRangeException($"File consists of {totalSize} blocks. Offset {offset} is incorrect");
        }

        private int GetUsedBlockCountInLastIndexPage()
        {
            return this.indexList.Last.Value.Where(x => x != Constants.EmptyBlockIndex).Count();
        }

        private int GetTotalSize()
        {
            return (this.indexList.Count - 1) * Constants.MaxItemsInIndexPage + GetUsedBlockCountInLastIndexPage();
        }

        private async Task IncreaseInternal(int blockCount)
        {
            this.indexLock.EnterWriteLock();
            try
            {
                var blockIndex = this.indexList.Count == 1
                    ? this.rootBlockIndex
                    : this.indexList.Last.Previous.Value[Constants.IndexPageSize - 1];

                var restItems = Constants.MaxItemsInIndexPage - GetUsedBlockCountInLastIndexPage();
                var indexPagesToRequest = blockCount < restItems ? 0 : (blockCount - restItems) / Constants.MaxItemsInIndexPage + 1;
                var requestedBlockCount = blockCount + indexPagesToRequest;

                var blocks = await this.allocationManager.Allocate(requestedBlockCount);

                var lastIndexesPage = this.indexList.Last.Value;
                var indexOfNewPage = -1;
                var indexOfBock = indexPagesToRequest;
                while (restItems > 0)
                {
                    for (int i = Constants.MaxItemsInIndexPage - restItems; i < Constants.MaxItemsInIndexPage && indexOfBock < blockCount - indexPagesToRequest; i++)
                    {
                        lastIndexesPage[i] = blocks[indexOfBock++];
                    }

                    indexOfNewPage++;
                    bool hasNextPage = indexOfNewPage < indexPagesToRequest;
                    if (hasNextPage)
                    {
                        lastIndexesPage[Constants.IndexPageSize - 1] = blocks[indexOfNewPage];
                    }

                    var indexPage = new ListItemBlockIndexes { Indexes = lastIndexesPage };
                    await this.storage.WriteBlock(blockIndex, ref indexPage);

                    if (hasNextPage)
                    {
                        blockIndex = blocks[indexOfNewPage];
                        lastIndexesPage = new uint[Constants.IndexPageSize];
                        this.indexList.AddLast(lastIndexesPage);
                        restItems = Constants.MaxItemsInIndexPage;
                    }
                    else
                    {
                        restItems = 0;
                    }
                }
            }
            finally
            {
                this.indexLock.ExitWriteLock();
            }
        }

        private async Task ShrinkInternal(int totalBlockCount)
        {
            this.indexLock.EnterWriteLock();
            try
            {
                GetPositionByOffset(totalBlockCount, out int pageNo, out int pageOffset);

                if (pageOffset == Constants.MaxItemsInIndexPage - 1)
                {
                    pageNo++; pageOffset = 0;
                }

                var blocksToRelease = this.indexList
                    .Skip(pageNo)
                    .SelectMany((x, index) => index == 0 ? x.Skip(pageOffset) : x).ToArray();

                await this.allocationManager.Release(blocksToRelease);

                for (var i = 0; i < this.indexList.Count - pageNo - 1; i++)
                {
                    this.indexList.RemoveLast();
                }

                var lastIndexesPage = this.indexList.Last.Value;
                for (var i = pageOffset; i < Constants.IndexPageSize; i++)
                {
                    lastIndexesPage[i] = Constants.EmptyBlockIndex;
                }

                var blockIndex = this.indexList.Count == 1
                    ? this.rootBlockIndex
                    : this.indexList.Last.Previous.Value[Constants.IndexPageSize - 1];
                var indexPage = new ListItemBlockIndexes { Indexes = lastIndexesPage };
                await this.storage.WriteBlock(blockIndex, ref indexPage);
            }
            finally
            {
                this.indexLock.ExitWriteLock();
            }
        }
    }
}
