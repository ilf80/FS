using FS.Contracts;
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
}
