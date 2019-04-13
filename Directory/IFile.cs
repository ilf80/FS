using FS.BlockAccess;
using System;

namespace FS.Directory
{
    internal interface IFile : IBlockHandle, IFlushable, IDisposable
    {
        int Size { get; }

        void Read(int position, byte[] buffer);

        void Write(int position, byte[] buffer);

        void SetSize(int size);
    }
}
