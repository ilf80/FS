using System;
using FS.Contracts;

namespace FS.Directory
{
    internal interface IFile : IBlockHandle, ISupportsFlush, IDisposable
    {
        int Size { get; }

        void Read(int position, byte[] buffer);

        void Write(int position, byte[] buffer);

        void SetSize(int size);
    }
}
