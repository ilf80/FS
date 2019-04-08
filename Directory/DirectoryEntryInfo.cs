using System;

namespace FS.Directory
{
    internal sealed class DirectoryEntryInfo : IDirectoryEntryInfo
    {
        public DirectoryEntryInfo(DirectoryEntry header, string name)
        {
            IsDirectory = header.Flags == DirectoryFlags.Directory;
            Size = header.Size;
            Created = DateTime.FromBinary(header.Created);
            Updated = DateTime.FromBinary(header.Updated);
            Name = name;
            BlockId = header.BlockIndex;
        }

        public bool IsDirectory { get; private set; }

        public int Size { get; private set; }

        public DateTime Created { get; private set; }

        public DateTime Updated { get; private set; }

        public string Name { get; private set; }

        public int BlockId { get; private set; }
    }
}
