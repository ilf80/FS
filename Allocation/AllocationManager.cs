﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FS.BlockAccess;
using FS.BlockAccess.Indexes;
using FS.Utils;

namespace FS.Allocation
{
    internal sealed class AllocationManager : IAllocationManager
    {
        private readonly IIndex<int> index;
        private readonly IBlockStream<int> blockStream;
        private readonly IBlockStorage storage;
        private readonly object lockObject = new object();

        public AllocationManager(
            Func<IAllocationManager, IIndex<int>> indexFactory,
            IBlockStorage storage,
            int freeSpaceBlocksCount)
        {
            if (indexFactory == null) throw new ArgumentNullException(nameof(indexFactory));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

            index = indexFactory(this);
            blockStream = new BlockStream<int>(index);
            ReleasedBlockCount = freeSpaceBlocksCount;
        }

        public int ReleasedBlockCount { get; private set; }

        public int BlockId => index.BlockId;

        public int[] Allocate(int blockCount)
        {
            if (blockCount <= 0) throw new ArgumentOutOfRangeException(nameof(blockCount));

            var allocatedFromIndexBlocks = new int[0];

            Monitor.Enter(lockObject);
            try
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
            finally
            {
                Monitor.Exit(lockObject);
            }

            EraseBlocks(allocatedFromIndexBlocks);
            return allocatedFromIndexBlocks;
        }

        public void Release(int[] blocks)
        {
            Monitor.Enter(lockObject);
            try
            {
                index.SetSizeInBlocks(Helpers.ModBaseWithCeiling(ReleasedBlockCount + blocks.Length, blockStream.Provider.BlockSize));
                blockStream.Write(ReleasedBlockCount, blocks);
                ReleasedBlockCount += blocks.Length;
            }
            finally
            {
                Monitor.Exit(lockObject);
            }
        }

        public void Flush()
        {
            Monitor.Enter(lockObject);
            try
            {
                index.Flush();
            }
            finally
            {
                Monitor.Exit(lockObject);
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
