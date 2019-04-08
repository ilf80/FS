using FS.BlockChain;

namespace FS.Contracts
{
    internal interface IIndexBlockChainProvier : IBlockChainProvider<int>, IFlushable
    {
        int UsedEntryCount { get; }
    }
}
