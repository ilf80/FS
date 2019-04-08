namespace FS.BlockAccess.Indexes
{
    internal interface IIndexBlockProvier : IBlockProvider<int>, IFlushable
    {
        int UsedEntryCount { get; }
    }
}
