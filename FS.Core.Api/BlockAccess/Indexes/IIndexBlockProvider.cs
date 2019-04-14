using FS.Core.Api.Common;

namespace FS.Core.Api.BlockAccess.Indexes
{
    public interface IIndexBlockProvider : IBlockProvider<int>, ISupportsFlush
    {
        int BlockId { get; }

        int UsedEntryCount { get; }
    }
}
