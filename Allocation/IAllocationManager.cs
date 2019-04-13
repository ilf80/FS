using FS.BlockAccess;
using FS.BlockAccess;
using System;
using System.Threading.Tasks;

namespace FS.Allocattion
{
    internal interface IAllocationManager : IFlushable, IDisposable
    {
        int ReleasedBlockCount { get; }

        int BlockId { get; }

        int[] Allocate(int blockCount);

        void Release(int[] blocks);
    }
}
