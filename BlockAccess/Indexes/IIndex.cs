using FS.Contracts;

namespace FS.BlockAccess.Indexes
{
    internal interface IIndex<T> : IBlockProvider<T>, ISupportsFlush where T : struct
    {
        int BlockId { get; }
    }
}
