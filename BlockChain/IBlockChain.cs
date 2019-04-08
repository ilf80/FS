namespace FS.BlockChain
{
    internal interface IBlockChain<T> where T : struct
    {
        void Write(int position, T[] buffer);

        void Read(int position, T[] buffer);
    }
}
