using FS.BlockStorage;
using FS.Contracts;
using FS.Indexes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FS.SystemFile
{
    internal class SystemFile : ISystemFile
    {
        private readonly IBlockStorage storage;
        private readonly IIndexManager indexManager;
        private int length;

        public SystemFile(
            IBlockStorage storage,
            IIndexManager index,
            int length)
        {
            this.storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            this.indexManager = index ?? throw new System.ArgumentNullException(nameof(index));
            this.length = length;
        }

        public int Length => length;

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public async Task SetSize(int totalBytes)
        {
            if (length == totalBytes)
            {
                return;
            }

            var desiredBlocksLength = (totalBytes / Constants.BlockSize) + 1;
            if (length < totalBytes)
            {
                await indexManager.Shrink(desiredBlocksLength);
            }
            else
            {
                var currentBlocksLength = (length / Constants.BlockSize) + 1;
                await indexManager.Increase(desiredBlocksLength - currentBlocksLength);
            }

            length = totalBytes;
        }

        public Task Flush()
        {
            throw new System.NotImplementedException();
        }

        public async Task<int> Read(int position, byte[] buffer)
        {
            throw new System.NotImplementedException();
        }

        public async Task Write(int position, byte[] buffer)
        {
            if (position + buffer.Length >= length)
            {
                throw new Exception("Out of bounds");
            }
            indexManager.Lock();
            try
            {
                var blockCount = buffer.Length / Constants.BlockSize + 1;
                var blockIds = await indexManager.GetBlocksForOffset(position, blockCount);

                var bufferOffset = 0;
                for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
                {
                    if (blockIndex == 0)
                    {
                        var writeBuffer = new byte[Constants.BlockSize];
                        await storage.ReadBlock(blockIds[0], buffer);

                        var offset = position % Constants.BlockSize;
                        for (int i = offset; i < Constants.BlockSize; i++)
                        {
                            writeBuffer[i] = buffer[bufferOffset++];
                        }
                        await storage.WriteBlock(blockIds[0], writeBuffer);
                    }
                    else if (blockIndex == blockCount - 1)
                    {
                        var writeBuffer = new byte[Constants.BlockSize];
                        await storage.ReadBlock(blockIds[blockIndex], buffer);
                        var bytesCount = buffer.Length - bufferOffset;
                        for (int i = 0; i < bytesCount; i++)
                        {
                            writeBuffer[i] = buffer[bufferOffset++];
                        }
                        await storage.WriteBlock(blockIds[blockIndex], writeBuffer);
                    }
                    else
                    {
                        var writeBuffer = new byte[Constants.BlockSize];
                        for (int i = 0; i < Constants.BlockSize; i++)
                        {
                            writeBuffer[i] = buffer[bufferOffset++];
                        }
                        await storage.WriteBlock(blockIds[blockIndex], writeBuffer);
                    }
                }
            }
            finally
            {
                indexManager.Release();
            }
        }
    }
}
