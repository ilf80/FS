namespace FS
{
    internal interface IIndex<in T> : IBlockProvider<T>, ISupportsFlush where T : struct
    {
        int BlockId { get; }
    }
}