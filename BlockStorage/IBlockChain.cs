namespace FS.BlockStorage
{
    internal interface IBlockChain<T> where T : struct
    {
        void Write(int position, T[] buffer);

        void Read(int position, T[] buffer);
    }

    internal interface IBlockChainProvider<T> where T : struct
    {
        int BlockSize { get; }

        int EntrySize { get; }

        int SizeInBlocks { get; }

        void SetSizeInBlocks(int count);

        void Read(int index, T[] buffer);

        void Write(int index, T[] buffer);
    }
}
