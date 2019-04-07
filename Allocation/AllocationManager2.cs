using FS.BlockStorage;
using FS.Indexes;
using System;
using System.Linq;
using System.Threading;

namespace FS.Allocattion
{
    internal class AllocationManager2 : IAllocationManager2
    {
        private readonly IIndex<int> index;
        private readonly IBlockChain<int> blockChain;
        private readonly IBlockStorage2 storage;
        private int releasedBlockCount;

        public AllocationManager2(
            Func<IAllocationManager2, IIndex<int>> indexFacory,
            IBlockStorage2 storage,
            int freeSpaceBlocksCount)
        {
            if (indexFacory == null)
            {
                throw new ArgumentNullException(nameof(indexFacory));
            }

            this.index = indexFacory(this);

            this.blockChain = new BlockChain<int>(this.index);
            this.storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            this.releasedBlockCount = freeSpaceBlocksCount;
        }

        public int ReleasedBlockCount => this.releasedBlockCount;

        public int[] Allocate(int blockCount)
        {
            CheckSize(blockCount);

            var allocatedFromIndexBlocks = new int[0];
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
            return allocatedFromIndexBlocks;
        }

        public void Release(int[] blocks)
        {
            this.index.SetSizeInBlocks(this.releasedBlockCount + blocks.Length);
            this.blockChain.Write(this.releasedBlockCount, blocks);
            this.releasedBlockCount += blocks.Length;
        }

        public void Flush()
        {
            this.index.Flush();
        }
        
        private void CheckSize(int blockCount)
        {

        }
    }
}
