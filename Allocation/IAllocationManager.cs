using FS.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FS.Allocattion
{
    internal interface IAllocationManager
    {
        Task<uint[]> Allocate(int blockCount);

        Task<IOVoid> Release(uint[] blocks);
    }

    internal interface IAllocationManager2
    {
        int[] Allocate(int blockCount);

        void Release(int[] blocks);
    }

    internal class AllocationManager2 : IAllocationManager2
    {
        private int block = 2;

        public int[] Allocate(int blockCount)
        {
            return Enumerable.Range(0, blockCount).Select(x => Interlocked.Increment(ref this.block)).ToArray();
        }

        public void Release(int[] blocks)
        {
        }
    }
}
