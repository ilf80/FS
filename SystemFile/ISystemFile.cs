using System;
using System.Threading.Tasks;

namespace FS.SystemFile
{
    internal interface ISystemFile : IDisposable
    {
        int Length { get; }

        Task<int> Read(int position, byte[] buffer);

        Task Write(int position, byte[] buffer);

        Task SetSize(int totalBytes);

        Task Flush();
    }
}
