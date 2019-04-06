using FS.Contracts;
using FS.Indexes;
using System.Threading.Tasks;

namespace FS.Allocattion
{
    internal interface IAllocationManager
    {
        Task<uint[]> Allocate(int blockCount);

        Task<IOVoid> Release(uint[] blocks);
    }

    internal interface IAllocationManager2 : IFlushable
    {
        int[] Allocate(int blockCount);

        void Release(int[] blocks);
    }
}
