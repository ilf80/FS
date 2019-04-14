using FS.Core.Api.Common;

namespace FS.Core.Api.BlockAccess.Indexes
{
    public interface IIndex<in T> : IBlockProvider<T>, ISupportsFlush where T : struct
    {
        int BlockId { get; }
    }
}
