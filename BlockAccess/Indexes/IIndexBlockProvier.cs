namespace FS.Contracts.Indexes
{
    internal interface IIndexBlockProvier : IBlockProvider<int>, IFlushable
    {
        int BlockId { get; }

        int UsedEntryCount { get; }
    }
}
