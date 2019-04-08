namespace FS.BlockAccess.Indexes
{
    internal interface IIndex<T> : IBlockProvider<T>, IFlushable where T : struct
    {
        int BlockId { get; }
    }
}
