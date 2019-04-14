namespace FS.BlockAccess
{
    internal interface IBlockProvider<in T> where T : struct
    {
        int BlockSize { get; }

        int EntrySize { get; }

        int SizeInBlocks { get; }

        void SetSizeInBlocks(int count);

        void Read(int index, T[] buffer);

        void Write(int index, T[] buffer);
    }
}
