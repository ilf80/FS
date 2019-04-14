using System;
using FS.Core.Api.Directory;

namespace FS.Core.Directory
{
    internal sealed class DirectoryEntryInfo : IDirectoryEntryInfo
    {
        public DirectoryEntryInfo(DirectoryEntryStruct header, string name)
        {
            IsDirectory = header.Flags == DirectoryFlags.Directory;
            Size = header.Size;
            Created = DateTime.FromBinary(header.Created);
            Updated = DateTime.FromBinary(header.Updated);
            Name = name;
            BlockId = header.BlockIndex;
        }

        public bool IsDirectory { get; }

        public int Size { get; }

        public DateTime Created { get; }

        public DateTime Updated { get; }

        public string Name { get; }

        public int BlockId { get; }
    }
}
