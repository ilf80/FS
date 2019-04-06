using System;

namespace FS.BlockStorage
{
    internal sealed class BlockChain<T> : IBlockChain<T> where T : struct
    {
        private readonly IBlockChainProvider<T> provider;

        public BlockChain(
            IBlockChainProvider<T> provider)
        {
            this.provider = provider ?? throw new System.ArgumentNullException(nameof(provider));
        }

        public void GetOffset(int position, out int blockIndex, out int blockOffset)
        {
            blockIndex = position / this.provider.BlockSize;
            blockOffset = position % this.provider.BlockSize;
        }

        public void Read(int position, T[] buffer)
        {
            ProcessBuffer(position, buffer, false);
        }

        public void Write(int position, T[] buffer)
        {
            ProcessBuffer(position, buffer, true);
        }

        private void ProcessBuffer(int position, T[] buffer, bool write)
        {
            CheckOuOfBounds(position, buffer.Length);

            var blockCount = (position + buffer.Length - 1) / this.provider.BlockSize - position / this.provider.BlockSize + 1;
            var bufferOffset = 0;
            var blockBuffer = new T[this.provider.BlockSize];
            for (var blockIndex = position / this.provider.BlockSize; blockIndex < blockCount; blockIndex++)
            {
                if (blockIndex == 0 || blockIndex == blockCount - 1 || !write)
                {
                    this.provider.Read(blockIndex, blockBuffer);
                }

                if (blockIndex == 0)
                {
                    var offset = position % this.provider.BlockSize;
                    var entryCount = Math.Min(this.provider.BlockSize - offset, buffer.Length);
                    bufferOffset += TransferData(blockBuffer, buffer, offset, bufferOffset, entryCount, write);
                }
                else if (blockIndex == blockCount - 1)
                {
                    var entryCount = buffer.Length - bufferOffset;
                    bufferOffset += TransferData(blockBuffer, buffer, 0, bufferOffset, entryCount, write);
                }
                else
                {
                    bufferOffset += TransferData(blockBuffer, buffer, 0, bufferOffset, this.provider.BlockSize, write);
                }

                if (write)
                {
                    this.provider.Write(blockIndex, blockBuffer);
                }
            }
        }

        private int TransferData(T[] array1, T[] array2, int array1Offset, int array2Offset, int count, bool direct)
        {
            var target = array1;
            var source = array2;
            var targetIndex = array1Offset;
            var sourceIndex = array2Offset;
            if (!direct)
            {
                target = array2;
                source = array1;
                targetIndex = array2Offset;
                sourceIndex = array1Offset;
            }
            for (var i = 0; i < count; i++, targetIndex++, sourceIndex++)
            {
                target[targetIndex] = source[sourceIndex];
            }
            return count;
        }

        private void CheckOuOfBounds(int position, int length)
        {
        }
    }
}
