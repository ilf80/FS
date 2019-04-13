namespace FS.BlockAccess
{
    internal interface IBlockStream<T> where T : struct
    {
        IBlockProvider<T> Provider { get; }

        void Write(int position, T[] buffer);

        void Read(int position, T[] buffer);
    }
}
