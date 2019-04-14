using FS.Contracts;

namespace FS.BlockAccess.Indexes
{
    internal interface IIndexBlockProvider : IBlockProvider<int>, ISupportsFlush
    {
        int BlockId { get; }

        int UsedEntryCount { get; }
    }
}
