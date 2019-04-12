namespace FS.Contracts
{
    internal interface IBlockProvider<T> where T : struct
    {
        int BlockSize { get; }

        int EntrySize { get; }

        int SizeInBlocks { get; }

        void SetSizeInBlocks(int count);

        void Read(int index, T[] buffer);

        void Write(int index, T[] buffer);
    }
}
