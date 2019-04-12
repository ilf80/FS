using System;

namespace FS.Directory
{
    internal interface IDirectoryEntryInfoOverrides
    {
        int? Size { get; }

        DateTime? Updated { get; }

        string Name { get; }

        DirectoryFlags? Flags { get; }
    }
}
