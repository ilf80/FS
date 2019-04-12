using FS.Contracts;
using FS.Contracts;
using System.Threading.Tasks;

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
