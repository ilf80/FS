using System;

namespace FS.Core.Api.Directory
{
    public interface IDirectoryEntryInfoOverrides
    {
        int? Size { get; }

        DateTime? Updated { get; }

        string Name { get; }

        DirectoryFlags? Flags { get; }
    }
}
