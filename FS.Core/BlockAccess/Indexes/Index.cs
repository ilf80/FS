using System;
using System.Runtime.InteropServices;
using FS.Core.Api.Allocation;
using FS.Core.Api.BlockAccess;
using FS.Core.Api.BlockAccess.Indexes;

namespace FS.Core.BlockAccess.Indexes
{
    internal sealed class Index<T> : IIndex<T> where T : struct
    {
        private static readonly int StructSize = Marshal.SizeOf<T>();

        private readonly IIndexBlockProvider provider;
        private readonly IBlockStream<int> indexBlockStream;
        private readonly IBlockStorage storage;
        private readonly IAllocationManager allocationManager;

        public Index(
            IIndexBlockProvider provider,
            IBlockStream<int> blockStream,
            IAllocationManager allocationManager,
            IBlockStorage storage)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            indexBlockStream = blockStream ?? throw new ArgumentNullException(nameof(blockStream));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.allocationManager = allocationManager ?? throw new ArgumentNullException(nameof(allocationManager));
        }

        public int BlockId => provider.BlockId;

        public int BlockSize => storage.BlockSize / EntrySize;

        public int EntrySize => StructSize;

        public int SizeInBlocks => provider.UsedEntryCount;

        public void Flush()
        {
            provider.Flush();
        }

        public void Read(int index, T[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            ProcessData(index, buffer, false);
        }

        public void Write(int index, T[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            ProcessData(index, buffer, true);
        }

        public void SetSizeInBlocks(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            var currentBlockCount = provider.UsedEntryCount;
            if (count == currentBlockCount)
            {
                return;
            }

            if (count > currentBlockCount)
            {
                provider.SetSizeInBlocks(count / provider.BlockSize + (count % provider.BlockSize == 0 ? 0 : 1));

                var allocateBlockCount = count - currentBlockCount;
                var blocks = allocationManager.Allocate(allocateBlockCount);
                indexBlockStream.Write(currentBlockCount, blocks);
            }
            else
            {
                var releaseBlockCount = currentBlockCount - count;
                var blocks = new int[releaseBlockCount];
                indexBlockStream.Read(currentBlockCount - releaseBlockCount, blocks);
                allocationManager.Release(blocks);

                blocks = new int[releaseBlockCount];
                indexBlockStream.Write(currentBlockCount - releaseBlockCount, blocks);

                provider.SetSizeInBlocks(count / provider.BlockSize + count % provider.BlockSize == 0 ? 0 : 1);
            }
        }

        private int[] GetDataBlocks(int index, int entryCount)
        {
            var blockCount = (entryCount * EntrySize) / storage.BlockSize;
            var blocks = new int[blockCount];
            indexBlockStream.Read(index, blocks);
            return blocks;
        }

        private void ProcessData(int index, T[] buffer, bool write)
        {
            var bufferOffset = 0;
            var tempBufferSize = storage.BlockSize / EntrySize;
            var tempBuffer = new T[tempBufferSize];
            var blocks = GetDataBlocks(index, buffer.Length);
            foreach (var blockId in blocks)
            {
                if (write)
                {
                    Array.Copy(buffer, bufferOffset, tempBuffer, 0, tempBufferSize);
                    storage.WriteBlock(blockId, tempBuffer);
                }
                else
                {
                    storage.ReadBlock(blockId, tempBuffer);
                    Array.Copy(tempBuffer, 0, buffer, bufferOffset, tempBufferSize);
                }
                bufferOffset += tempBufferSize;
            }
        }
    }
}
