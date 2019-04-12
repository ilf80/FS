using FS.Allocattion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FS.Contracts.Indexes
{
    internal class IndexBlockProvier : IIndexBlockProvier
    {
        private readonly LinkedList<int[]> indexList = new LinkedList<int[]>();
        private readonly int rootBlockIndex;
        private readonly IAllocationManager allocationManager;
        private readonly IBlockStorage storage;
        private bool isDirty;

        public IndexBlockProvier(
            int rootBlockIndex,
            IAllocationManager allocationManager,
            IBlockStorage storage)
        {
            this.rootBlockIndex = rootBlockIndex;
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public int BlockId => this.rootBlockIndex;

        public int BlockSize => Constants.MaxItemsInIndexPage;

        public int EntrySize => Constants.IndexEntrySize;

        public int SizeInBlocks
        {
            get
            {
                EnsureLoaded();
                return this.indexList.Count;
            }
        }

        public int UsedEntryCount
        {
            get
            {
                EnsureLoaded();
                return (this.indexList.Count - 1) * Constants.MaxItemsInIndexPage
                    + (this.indexList.Count > 0 
                    ? this.indexList.Last.Value.Count(x => x != Constants.EmptyBlockIndex)
                    : 0);
            }
        }

        public void Read(int index, int[] buffer)
        {
            EnsureLoaded();

            Array.Copy(this.indexList.Skip(index).First(), buffer, buffer.Length);
        }

        public void Write(int index, int[] buffer)
        {
            EnsureLoaded();

            this.isDirty = true;

            Array.Copy(buffer, this.indexList.Skip(index).First(), buffer.Length);
        }

        public void SetSizeInBlocks(int count)
        {
            EnsureLoaded();

            count = Math.Max(count, 1);

            if (this.indexList.Count == count)
            {
                return;
            }

            this.isDirty = true;

            if (this.indexList.Count < count)
            {
                var blocks = this.allocationManager.Allocate(count - this.indexList.Count);
                foreach(var blockId in blocks)
                {
                    SetNextExtentionBlockIndex(this.indexList.Last.Value, blockId);
                    this.indexList.AddLast(new int[Constants.IndexPageSize]);
                }
            }
            else
            {
                var blocks = new int[this.indexList.Count - count];
                for(var i = 0; i < blocks.Length; i++)
                {
                    this.indexList.RemoveLast();
                    SetNextExtentionBlockIndex(this.indexList.Last.Value, Constants.EmptyBlockIndex);
                }
                this.allocationManager.Release(blocks);
            }
        }

        public void Flush()
        {
            if (!this.isDirty)
            {
                return;
            }

            var blockIndex = this.rootBlockIndex;
            foreach(var indexPage in this.indexList)
            {
                this.storage.WriteBlock(blockIndex, indexPage);
                blockIndex = GetNextExtentionBlockIndex(indexPage);
            }

            this.isDirty = false;
        }

        private void EnsureLoaded()
        {
            if (this.indexList.Count > 0)
            {
                return;
            }
            Load();
        }

        private void Load()
        {
            int[] index;
            for (var extentionBlockIndex = this.rootBlockIndex;
                extentionBlockIndex != Constants.EmptyBlockIndex;
                extentionBlockIndex = GetNextExtentionBlockIndex(index))
            {
                index = new int[Constants.IndexPageSize];
                this.storage.ReadBlock(extentionBlockIndex, index);
                this.indexList.AddLast(index);
            }
        }

        private int GetNextExtentionBlockIndex(int[] index)
        {
            return index[index.Length - 1];
        }

        private void SetNextExtentionBlockIndex(int[] index, int blockIndex)
        {
            index[index.Length - 1] = blockIndex;
        }
    }
}
