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

        public AllocationManager2(
            Func<IAllocationManager2, IIndex<int>> indexFacory,
            IBlockStorage2 storage)
        {
            if (indexFacory == null)
            {
                throw new ArgumentNullException(nameof(indexFacory));
            }

            this.index = indexFacory(this);

            this.blockChain = new BlockChain<int>(this.index);
            this.storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
        }

        public int[] Allocate(int blockCount)
        {
            CheckSize(blockCount);

            var allocatedFromIndexBlocks = new int[0];
            var allocatedFromIndexBlockCount = Math.Min(this.index.SizeInBlocks, blockCount);
            if (allocatedFromIndexBlockCount > 0)
            {
                allocatedFromIndexBlocks = new int[allocatedFromIndexBlockCount];
                var position = this.index.SizeInBlocks - allocatedFromIndexBlockCount;

                this.blockChain.Read(position, allocatedFromIndexBlocks);
                this.blockChain.Write(position, new int[allocatedFromIndexBlockCount]);
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
            this.blockChain.Write(this.index.SizeInBlocks, blocks);
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
