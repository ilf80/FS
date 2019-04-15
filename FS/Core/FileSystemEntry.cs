using System;

namespace FS.Core
{
    internal sealed class FileSystemEntry : IFileSystemEntry
    {
        public FileSystemEntry(IDirectoryEntryInfo info)
        {
            IsDirectory = info.IsDirectory;
            Name = info.Name;
            Size = info.Size;
            Created = info.Created;
            Updated = info.Updated;
        }

        public bool IsDirectory { get; }

        public int Size { get; }

        public DateTime Created { get; }

        public DateTime Updated { get; }

        public string Name { get; }
    }
}