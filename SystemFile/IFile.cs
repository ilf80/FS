using FS.BlockStorage;

namespace FS.SystemFile
{
    internal interface IFile<T> where T: struct
    {
        int BlockCount { get; }

        void SetBlockCont(int blockCount);

        void Read(int position, T[] buffer);

        void Write(int position, T[] buffer);
    }

    internal class File<T> : IFile<T> where T : struct
    {
        private readonly IBlockChain<T> blockChain;
        private readonly IBlockChainProvider<T> blockChainProvider;

        public File(
            IBlockChain<T> blockChain,
            IBlockChainProvider<T> blockChainProvider
            )
        {
            this.blockChain = blockChain ?? throw new System.ArgumentNullException(nameof(blockChain));
            this.blockChainProvider = blockChainProvider ?? throw new System.ArgumentNullException(nameof(blockChainProvider));
        }

        public int BlockCount => throw new System.NotImplementedException();

        public void Read(int position, T[] buffer)
        {
            this.blockChainProvider.Read(position, buffer);
        }

        public void SetBlockCont(int blockCount)
        {
            throw new System.NotImplementedException();
        }

        public void Write(int position, T[] buffer)
        {
            this.blockChain.Write(position, buffer);
        }
    }
}
