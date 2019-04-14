using FS.BlockAccess;
using FS.Contracts;

namespace FS.Allocation
{
    internal interface IAllocationManager : ISupportsFlush
    {
        int ReleasedBlockCount { get; }

        int BlockId { get; }

        int[] Allocate(int blockCount);

        void Release(int[] blocks);
    }
}
