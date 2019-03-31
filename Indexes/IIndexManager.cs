using System.Threading.Tasks;

namespace FS.Indexes
{
    internal interface IIndexManager
    {
        Task Increase(int blockCount);

        Task Shrink(int totalBlockCount);

        Task<uint[]> GetBlocksForOffset(int offset, int count);

        void Lock();

        void Release();
    }
}
