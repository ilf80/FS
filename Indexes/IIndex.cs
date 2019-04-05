using FS.Allocattion;
using FS.BlockStorage;
using FS.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS.Indexes
{
    interface IFlushable
    {
        void Flush();
    }

    internal interface IIndex<T> : IBlockChainProvider<T>, IFlushable where T : struct
    {
    }

    internal class Index<T> : IIndex<T> where T : struct
    {
        private readonly IIndexBlockChainProvier provider;
        private readonly IBlockChain<int> blockChain;
        private readonly uint rootBlockIndex;

        public Index(
            IIndexBlockChainProvier provider,
            IBlockChain<int> blockChain)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.blockChain = blockChain ?? throw new ArgumentNullException(nameof(blockChain));
        }

        public int BlockSize => throw new NotImplementedException();

        public int EntrySize => throw new NotImplementedException();

        public int SizeInBlocks => throw new NotImplementedException();

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Read(int index, T[] buffer)
        {
            throw new NotImplementedException();
        }

        public void SetSizeInBlocks(int count)
        {
            throw new NotImplementedException();
        }

        public void Write(int index, T[] buffer)
        {
            throw new NotImplementedException();
        }
    }

    internal interface IIndexBlockChainProvier : IBlockChainProvider<int>, IFlushable
    {
    }

    internal class IndexBlockChainProvier : IIndexBlockChainProvier
    {
        private readonly LinkedList<int[]> indexList = new LinkedList<int[]>();
        private readonly int rootBlockIndex;
        private readonly IAllocationManager2 allocationManager;
        private readonly IBlockStorage2 storage;
        private bool isDirty;

        public IndexBlockChainProvier(
            int rootBlockIndex,
            IAllocationManager2 allocationManager,
            IBlockStorage2 storage)
        {
            this.rootBlockIndex = rootBlockIndex;
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public int BlockSize => Constants.MaxItemsInIndexPage * Constants.IndexEntrySize;

        public int EntrySize => Constants.IndexEntrySize;

        public int SizeInBlocks => this.indexList.Count;

        public void Read(int index, int[] buffer)
        {
            EnsureLoaded();

            Array.Copy(this.indexList.Skip(index).First(), buffer, buffer.Length);
        }

        public void Write(int index, int[] buffer)
        {
            EnsureLoaded();

            Array.Copy(buffer, this.indexList.Skip(index).First(), buffer.Length);
        }

        public void SetSizeInBlocks(int count)
        {
            EnsureLoaded();

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
                    this.indexList.AddLast(new int[Constants.IndexEntrySize]);
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
