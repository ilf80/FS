using FS.BlockAccess;

namespace FS.Directory
{
    internal interface IFile : IFlushable
    {
        void Read(int position, byte[] buffer);

        void Write(int position, byte[] buffer);

        void SetSize(int size);
    }
}
