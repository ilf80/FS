using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FS.Contracts;

namespace FS.Allocattion
{
    internal sealed class AllocationManager : IAllocationManager
    {
        private int block = 2;

        public Task<uint[]> Allocate(int blockCount)
        {
            return Task.FromResult(Enumerable.Range(0, blockCount).Select(x => (uint)Interlocked.Increment(ref this.block)).ToArray());
        }

        public Task<IOVoid> Release(uint[] blocks)
        {
            return Task.FromResult(IOVoid.Instance);
        }
    }
}
