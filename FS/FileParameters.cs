using System;

namespace FS
{
    internal sealed class FileParameters : IFileParameters
    {
        public FileParameters(int blockId, int parentDirectoryBlockId, int size)
        {
            if (blockId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockId));
            }

            if (parentDirectoryBlockId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parentDirectoryBlockId));
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            BlockId = blockId;
            ParentDirectoryBlockId = parentDirectoryBlockId;
            Size = size;
        }

        public int BlockId { get; }
        public int ParentDirectoryBlockId { get; }
        public int Size { get; }
    }
}