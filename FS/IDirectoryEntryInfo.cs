using System;

namespace FS
{
    internal interface IDirectoryEntryInfo
    {
        bool IsDirectory { get; }

        int Size { get; }

        DateTime Created { get; }

        DateTime Updated { get; }

        string Name { get; }

        int BlockId { get; }
    }
}