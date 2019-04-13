using FS.Contracts;
using FS.Contracts.Indexes;
using FS.Utils;
using System;
using System.Linq;
using System.Threading;

namespace FS.Allocattion
{
    internal class AllocationManager : IAllocationManager
    {
        private readonly IIndex<int> index;
        private readonly IBlockStream<int> blockChain;
        private readonly IBlockStorage storage;
        private readonly SemaphoreSlim lockObject = new SemaphoreSlim(1, 1);
        private int releasedBlockCount;

        public AllocationManager(
            Func<IAllocationManager, IIndex<int>> indexFacory,
            IBlockStorage storage,
            int freeSpaceBlocksCount)
        {
            if (indexFacory == null)
            {
                throw new ArgumentNullException(nameof(indexFacory));
            }

            this.index = indexFacory(this);

            this.blockChain = new BlockStream<int>(this.index);
            this.storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            this.releasedBlockCount = freeSpaceBlocksCount;
        }

        public int ReleasedBlockCount => this.releasedBlockCount;

        public int BlockId => this.index.BlockId;

        public int[] Allocate(int blockCount)
        {
            CheckSize(blockCount);

            var allocatedFromIndexBlocks = new int[0];

            this.lockObject.Wait();
            try
            {
                var allocatedFromIndexBlockCount = Math.Min(this.releasedBlockCount, blockCount);
                if (allocatedFromIndexBlockCount > 0)
                {
                    allocatedFromIndexBlocks = new int[allocatedFromIndexBlockCount];
                    var position = this.releasedBlockCount - allocatedFromIndexBlockCount;

                    this.blockChain.Read(position, allocatedFromIndexBlocks);
                    this.blockChain.Write(position, new int[allocatedFromIndexBlockCount]);

                    this.releasedBlockCount -= allocatedFromIndexBlockCount;
                }

                var leftBlocks = blockCount - allocatedFromIndexBlockCount;
                if (leftBlocks > 0)
                {
                    var blocks = this.storage.Extend(leftBlocks);
                    return allocatedFromIndexBlocks.Concat(blocks).ToArray();
                }
            }
            finally
            {
                this.lockObject.Release();
            }

            EraseBlocks(allocatedFromIndexBlocks);
            return allocatedFromIndexBlocks;
        }

        public void Release(int[] blocks)
        {
            this.lockObject.Wait();
            try
            {
                this.index.SetSizeInBlocks(Helpers.ModBaseWithCeiling(this.releasedBlockCount + blocks.Length, this.blockChain.Provider.BlockSize));
                this.blockChain.Write(this.releasedBlockCount, blocks);
                this.releasedBlockCount += blocks.Length;
            }
            finally
            {
                this.lockObject.Release();
            }
        }

        public void Flush()
        {
            this.lockObject.Wait();
            try
            {
                this.index.Flush();
            }
            finally
            {
                this.lockObject.Release();
            }
        }

        public void Dispose()
        {
            this.lockObject.Dispose();
        }

        private void EraseBlocks(int[] blocks)
        {
            var zerroBuffer = new byte[this.storage.BlockSize];
            foreach (var blockId in blocks)
            {
                this.storage.WriteBlock(blockId, zerroBuffer);
            }
        }

        private void CheckSize(int blockCount)
        {

        }
    }
}
