using FS.BlockAccess;

namespace FS.Directory
{
    internal interface IFile : IFlushable
    {
        int Size { get; }
        void Read(int position, byte[] buffer);

        void Write(int position, byte[] buffer);

        void SetSize(int size);
    }
}
