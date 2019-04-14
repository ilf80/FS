using System;
using FS.Core.Api.Common;

namespace FS.Core.Api.Directory
{
    public interface IFile : IBlockHandle, ISupportsFlush, IDisposable
    {
        int Size { get; }

        void Read(int position, byte[] buffer);

        void Write(int position, byte[] buffer);

        void SetSize(int size);
    }
}
