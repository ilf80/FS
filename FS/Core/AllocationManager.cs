using System;
using System.Collections.Generic;
using System.Linq;

namespace FS.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class AllocationManager : IAllocationManager
    {
        private readonly IBlockStream<int> blockStream;
        private readonly IIndex<int> index;
        private readonly object lockObject = new object();
        private readonly IBlockStorage storage;

        public AllocationManager(
            IFactory<IIndex<int>, IAllocationManager> indexFactory,
            IBlockStorage storage,
            int freeSpaceBlocksCount)
        {
            if (indexFactory == null)
            {
                throw new ArgumentNullException(nameof(indexFactory));
            }

            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

            index = indexFactory.Create(this);
            blockStream = new BlockStream<int>(index);
            ReleasedBlockCount = freeSpaceBlocksCount;
        }

        public int ReleasedBlockCount { get; private set; }

        public int BlockId => index.BlockId;

        public int[] Allocate(int blockCount)
        {
            if (blockCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockCount));
            }

            var allocatedFromIndexBlocks = new int[0];

            lock(lockObject)
            {
                var allocatedFromIndexBlockCount = Math.Min(ReleasedBlockCount, blockCount);
                if (allocatedFromIndexBlockCount > 0)
                {
                    allocatedFromIndexBlocks = new int[allocatedFromIndexBlockCount];
                    var position = ReleasedBlockCount - allocatedFromIndexBlockCount;

                    blockStream.Read(position, allocatedFromIndexBlocks);
                    blockStream.Write(position, new int[allocatedFromIndexBlockCount]);

                    ReleasedBlockCount -= allocatedFromIndexBlockCount;
                }

                var leftBlocks = blockCount - allocatedFromIndexBlockCount;
                if (leftBlocks > 0)
                {
                    var blocks = storage.Extend(leftBlocks);
                    return allocatedFromIndexBlocks.Concat(blocks).ToArray();
                }
            }

            EraseBlocks(allocatedFromIndexBlocks);
            return allocatedFromIndexBlocks;
        }

        public void Release(int[] blocks)
        {
            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            if (blocks.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(blocks));
            }

            lock(lockObject)
            {
                index.SetSizeInBlocks(Helpers.ModBaseWithCeiling(ReleasedBlockCount + blocks.Length, blockStream.Provider.BlockSize));
                blockStream.Write(ReleasedBlockCount, blocks);
                ReleasedBlockCount += blocks.Length;
            }
        }

        public void Flush()
        {
            lock(lockObject)
            {
                index.Flush();
            }
        }

        private void EraseBlocks(IEnumerable<int> blocks)
        {
            var zeroBuffer = new byte[storage.BlockSize];
            foreach (var blockId in blocks)
            {
                storage.WriteBlock(blockId, zeroBuffer);
            }
        }
    }
}