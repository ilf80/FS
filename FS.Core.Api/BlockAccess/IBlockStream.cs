namespace FS.Core.Api.BlockAccess
{
    public interface IBlockStream<in T> where T : struct
    {
        IBlockProvider<T> Provider { get; }

        void Write(int position, T[] buffer);

        void Read(int position, T[] buffer);
    }
}
