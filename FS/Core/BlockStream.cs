﻿using System;

namespace FS.Core
{
    internal sealed class BlockStream<T> : IBlockStream<T> where T : struct
    {
        public BlockStream(IBlockProvider<T> provider)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IBlockProvider<T> Provider { get; }

        public void Read(int position, T[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            }

            CheckOuOfBounds(position, buffer.Length);

            ProcessBuffer(position, buffer, false);
        }

        public void Write(int position, T[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));
            }

            CheckOuOfBounds(position, buffer.Length);

            ProcessBuffer(position, buffer, true);
        }

        private void ProcessBuffer(int position, T[] buffer, bool write)
        {
            var blockCount = (position + buffer.Length - 1) / Provider.BlockSize - position / Provider.BlockSize + 1;
            var bufferOffset = 0;
            var blockBuffer = new T[Provider.BlockSize];
            var blockIndex = Helpers.ModBaseWithFloor(position, Provider.BlockSize);
            for (var iterationIndex = 0; iterationIndex < blockCount; iterationIndex++, blockIndex++)
            {
                if (iterationIndex == 0 || iterationIndex == blockCount - 1 || !write)
                {
                    Provider.Read(blockIndex, blockBuffer);
                }

                if (iterationIndex == 0)
                {
                    var offset = position % Provider.BlockSize;
                    var entryCount = Math.Min(Provider.BlockSize - offset, buffer.Length);
                    bufferOffset += TransferData(blockBuffer, buffer, offset, bufferOffset, entryCount, write);
                }
                else if (iterationIndex == blockCount - 1)
                {
                    var entryCount = buffer.Length - bufferOffset;
                    bufferOffset += TransferData(blockBuffer, buffer, 0, bufferOffset, entryCount, write);
                }
                else
                {
                    bufferOffset += TransferData(blockBuffer, buffer, 0, bufferOffset, Provider.BlockSize, write);
                }

                if (write)
                {
                    Provider.Write(blockIndex, blockBuffer);
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

            Array.Copy(source, sourceIndex, target, targetIndex, count);
            return count;
        }

        private void CheckOuOfBounds(int position, int length)
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException($"position={position} cannot be negative.");
            }

            if (Provider.SizeInBlocks == 0)
            {
                throw new InvalidOperationException("BlockProvider is empty");
            }

            var blockIndex = Helpers.ModBaseWithFloor(position, Provider.BlockSize);
            if (blockIndex >= Provider.SizeInBlocks)
            {
                throw new InvalidOperationException($"Addressed position {position} with buffer.length {length} are out of Provider blocks space");
            }
        }
    }
}