using FS.Directory;
using System;

namespace FS.Api
{
    public sealed class FileSystemEntry : IFileSystemEntry
    {
        internal FileSystemEntry(IDirectoryEntryInfo info)
        {
            IsDirectory = info.IsDirectory;
            Name = info.Name;
            Size = info.Size;
            Created = info.Created;
            Updated = info.Updated;
        }

        public bool IsDirectory { get; private set; }

        public int Size { get; private set; }

        public DateTime Created { get; private set; }

        public DateTime Updated { get; private set; }

        public string Name { get; private set; }
    }
}
