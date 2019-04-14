using System;
using FS.Api;

namespace FS.Core.Api.BlockAccess
{
    public interface IBlockStorage : IDisposable
    {
        long TotalSize { get; }

        int BlockSize { get; }

        void ReadBlock(int blockIndex, byte[] buffer);

        void WriteBlock(int blockIndex, byte[] buffer);

        void ReadBlock<T>(int blockIndex, T[] buffer) where T : struct;

        void WriteBlock<T>(int blockIndex, T[] buffer) where T : struct;

        int[] Extend(int blockCount);

        void Open(OpenMode mode);
    }
}
