using FS.BlockAccess;
using FS.BlockAccess;
using System.Threading.Tasks;

namespace FS.Allocattion
{
    internal interface IAllocationManager : IFlushable
    {
        int ReleasedBlockCount { get; }

        int[] Allocate(int blockCount);

        void Release(int[] blocks);
    }
}
