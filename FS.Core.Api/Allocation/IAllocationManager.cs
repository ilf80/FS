using FS.Core.Api.Common;

namespace FS.Core.Api.Allocation
{
    public interface IAllocationManager : ISupportsFlush
    {
        int ReleasedBlockCount { get; }

        int BlockId { get; }

        int[] Allocate(int blockCount);

        void Release(int[] blocks);
    }
}
