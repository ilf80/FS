using System;

namespace FS
{
    internal interface IBlockStorage : IDisposable
    {
        long TotalSize { get; }

        int BlockSize { get; }

        int IndexEntrySize { get; }

        int IndexPageSize { get; }

        int MaxItemsInIndexPage { get; }

        void ReadBlock(int blockIndex, byte[] buffer);

        void WriteBlock(int blockIndex, byte[] buffer);

        void ReadBlock<T>(int blockIndex, T[] buffer) where T : struct;

        void WriteBlock<T>(int blockIndex, T[] buffer) where T : struct;

        int[] Extend(int blockCount);

        void Open(OpenMode mode);
    }
}