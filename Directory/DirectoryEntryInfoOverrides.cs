using System;

namespace FS.Directory
{
    internal sealed class DirectoryEntryInfoOverrides : IDirectoryEntryInfoOverrides
    {
        public DirectoryEntryInfoOverrides(int? size = null, DateTime? updated = null, string name = null, DirectoryFlags? flags = null)
        {
            Size = size;
            Updated = updated;
            Name = name;
            Flags = flags;
        }

        public int? Size { get; }

        public DateTime? Updated { get; }

        public string Name { get; }

        public DirectoryFlags? Flags { get; }
    }
}
