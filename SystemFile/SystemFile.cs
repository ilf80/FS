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

        public int Length => this.length;

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public async Task SetSize(int totalBytes)
        {
            if (this.length == totalBytes)
            {
                return;
            }

            var desiredBlocksLength = (totalBytes / Constants.BlockSize) + (totalBytes % Constants.BlockSize > 0 ? 1 : 0);
            if (this.length > totalBytes)
            {
                await this.indexManager.Shrink(desiredBlocksLength);
            }
            else
            {
                var currentBlocksLength = (this.length / Constants.BlockSize) + (this.length % Constants.BlockSize > 0 ? 1 : 0);
                await this.indexManager.Increase(desiredBlocksLength - currentBlocksLength);
            }

            this.length = totalBytes;
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
            if (position + buffer.Length >= this.length)
            {
                throw new Exception("Out of bounds");
            }
            //indexManager.Lock();
            try
            {
                var blockCount = (buffer.Length / Constants.BlockSize) + (position % Constants.BlockSize > 0 ? 1 : 0);
                var blockIds = await this.indexManager.GetBlocksForOffset(position / Constants.BlockSize, blockCount);

                var bufferOffset = 0;
                var writeBuffer = new byte[Constants.BlockSize];
                for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
                {
                    if (blockIndex == 0)
                    {
                        await this.storage.ReadBlock(blockIds[0], writeBuffer);

                        var offset = position % Constants.BlockSize;
                        for (int i = offset; i < Constants.BlockSize; i++)
                        {
                            writeBuffer[i] = buffer[bufferOffset++];
                        }
                        await this.storage.WriteBlock(blockIds[0], writeBuffer);
                    }
                    else if (blockIndex == blockCount - 1)
                    {
                        await this.storage.ReadBlock(blockIds[blockIndex], writeBuffer);

                        var bytesCount = buffer.Length - bufferOffset;
                        for (int i = 0; i < bytesCount; i++)
                        {
                            writeBuffer[i] = buffer[bufferOffset++];
                        }
                        await this.storage.WriteBlock(blockIds[blockIndex], writeBuffer);
                    }
                    else
                    {
                        for (int i = 0; i < Constants.BlockSize; i++)
                        {
                            writeBuffer[i] = buffer[bufferOffset++];
                        }

                        await this.storage.WriteBlock(blockIds[blockIndex], writeBuffer);
                    }
                }
            }
            finally
            {
                //indexManager.Release();
            }
        }
    }
}
