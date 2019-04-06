using FS.BlockStorage;
using System;
using System.Runtime.InteropServices;

namespace FS.Indexes
{
    internal class Index<T> : IIndex<T> where T : struct
    {
        static readonly int StructSize = Marshal.SizeOf<T>();

        private readonly IIndexBlockChainProvier provider;
        private readonly IBlockChain<int> indexBlockChain;
        private readonly IBlockStorage2 storage;

        public Index(
            IIndexBlockChainProvier provider,
            IBlockChain<int> blockChain,
            IBlockStorage2 storage)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.indexBlockChain = blockChain ?? throw new ArgumentNullException(nameof(blockChain));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public int BlockSize => this.storage.BlockSize;

        public int EntrySize => StructSize;

        public int SizeInBlocks => this.provider.UsedEntryCount;

        public void Flush()
        {
            this.provider.Flush();
        }

        public void Read(int index, T[] buffer)
        {
            ProcessData(index, buffer, false);
        }

        public void Write(int index, T[] buffer)
        {
            ProcessData(index, buffer, true);
        }

        public void SetSizeInBlocks(int count)
        {
            this.provider.SetSizeInBlocks(count / this.provider.BlockSize);
        }

        private int[] GetDataBlocks(int index, int entryCount)
        {
            var blockCount = (entryCount * EntrySize) / BlockSize;
            var blocks = new int[blockCount];
            this.indexBlockChain.Read(index, blocks);
            return blocks;
        }

        private void ProcessData(int index, T[] buffer, bool write)
        {
            var bufferOffset = 0;
            var tempBufferSize = this.storage.BlockSize / EntrySize;
            var tempBuffer = new T[tempBufferSize];
            var blocks = GetDataBlocks(index, buffer.Length);
            foreach (var blockId in blocks)
            {
                if (write)
                {
                    Array.Copy(buffer, bufferOffset, tempBuffer, 0, tempBufferSize);
                    this.storage.WriteBlock(blockId, tempBuffer);
                }
                else
                {
                    this.storage.ReadBlock(blockId, tempBuffer);
                    Array.Copy(tempBuffer, 0, buffer, bufferOffset, tempBufferSize);
                }
                bufferOffset += tempBufferSize;
            }
        }
    }
}
