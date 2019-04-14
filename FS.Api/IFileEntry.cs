using System;

namespace FS.Api
{
    public interface IFileEntry : IDisposable
    {
        int Size { get; }

        void Read(int position, byte[] buffer);

        void Write(int position, byte[] buffer);

        void SetSize(int size);

        void Flush();
    }
}
