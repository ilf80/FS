using System;
using System.Collections.Generic;
using System.Linq;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.BlockAccess.Indexes;
using FS.Core.Contracts;

namespace FS.Core.BlockAccess.Indexes
{
    internal sealed class IndexBlockProvider : IIndexBlockProvider
    {
        private readonly LinkedList<int[]> indexList = new LinkedList<int[]>();
        private readonly IAllocationManager allocationManager;
        private readonly IBlockStorage storage;
        private bool isDirty;

        public IndexBlockProvider(
            int rootBlockIndex,
            IAllocationManager allocationManager,
            IBlockStorage storage)
        {
            if (rootBlockIndex < 0) throw new ArgumentOutOfRangeException(nameof(rootBlockIndex));

            BlockId = rootBlockIndex;
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public int BlockId { get; }

        public int BlockSize => Constants.MaxItemsInIndexPage;

        public int EntrySize => Constants.IndexEntrySize;

        public int SizeInBlocks
        {
            get
            {
                EnsureLoaded();
                return indexList.Count;
            }
        }

        public int UsedEntryCount
        {
            get
            {
                EnsureLoaded();
                return (indexList.Count - 1) * Constants.MaxItemsInIndexPage
                    + (indexList.Count > 0 
                    ? indexList.Last.Value.Count(x => x != Constants.EmptyBlockIndex)
                    : 0);
            }
        }

        public void Read(int index, int[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            EnsureLoaded();

            Array.Copy(indexList.Skip(index).First(), buffer, buffer.Length);
        }

        public void Write(int index, int[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            EnsureLoaded();

            isDirty = true;

            Array.Copy(buffer, indexList.Skip(index).First(), buffer.Length);
        }

        public void SetSizeInBlocks(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            EnsureLoaded();

            count = Math.Max(count, 1);

            if (indexList.Count == count)
            {
                return;
            }

            isDirty = true;

            if (indexList.Count < count)
            {
                var blocks = allocationManager.Allocate(count - indexList.Count);
                foreach(var blockId in blocks)
                {
                    SetNextExtensionBlockIndex(indexList.Last.Value, blockId);
                    indexList.AddLast(new int[Constants.IndexPageSize]);
                }
            }
            else
            {
                var blocks = new int[indexList.Count - count];
                for(var i = 0; i < blocks.Length; i++)
                {
                    indexList.RemoveLast();
                    SetNextExtensionBlockIndex(indexList.Last.Value, Constants.EmptyBlockIndex);
                }
                allocationManager.Release(blocks);
            }
        }

        public void Flush()
        {
            if (!isDirty)
            {
                return;
            }

            var blockIndex = BlockId;
            foreach(var indexPage in indexList)
            {
                storage.WriteBlock(blockIndex, indexPage);
                blockIndex = GetNextExtensionBlockIndex(indexPage);
            }

            isDirty = false;
        }

        private void EnsureLoaded()
        {
            if (indexList.Count > 0)
            {
                return;
            }
            Load();
        }

        private void Load()
        {
            int[] index;
            for (var extensionBlockIndex = BlockId;
                extensionBlockIndex != Constants.EmptyBlockIndex;
                extensionBlockIndex = GetNextExtensionBlockIndex(index))
            {
                index = new int[Constants.IndexPageSize];
                storage.ReadBlock(extensionBlockIndex, index);
                indexList.AddLast(index);
            }
        }

        private static int GetNextExtensionBlockIndex(IReadOnlyList<int> index)
        {
            return index[index.Count - 1];
        }

        private static void SetNextExtensionBlockIndex(IList<int> index, int blockIndex)
        {
            index[index.Count - 1] = blockIndex;
        }
    }
}
