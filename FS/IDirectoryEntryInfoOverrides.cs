using System;

namespace FS
{
    internal interface IDirectoryEntryInfoOverrides
    {
        int? Size { get; }

        DateTime? Updated { get; }

        string Name { get; }

        DirectoryFlags? Flags { get; }
    }
}