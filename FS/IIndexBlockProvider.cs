namespace FS
{
    internal interface IIndexBlockProvider : IBlockProvider<int>, ISupportsFlush
    {
        int BlockId { get; }

        int UsedEntryCount { get; }
    }
}