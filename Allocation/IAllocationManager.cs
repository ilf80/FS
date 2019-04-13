using FS.BlockAccess;

namespace FS.Allocattion
{
    internal interface IAllocationManager : IFlushable
    {
        int ReleasedBlockCount { get; }

        int BlockId { get; }

        int[] Allocate(int blockCount);

        void Release(int[] blocks);
    }
}
