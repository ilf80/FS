using FS.BlockStorage;
using System.Text;
using System.Threading.Tasks;

namespace FS.Indexes
{
    interface IFlushable
    {
        void Flush();
    }

    internal interface IIndex<T> : IBlockChainProvider<T>, IFlushable where T : struct
    {
    }

    internal interface IIndexBlockChainProvier : IBlockChainProvider<int>, IFlushable
    {
        int UsedEntryCount { get; }
    }
}
