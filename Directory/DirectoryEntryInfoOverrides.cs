using System;

namespace FS.Directory
{
    internal sealed class DirectoryEntryInfoOverrides : IDirectoryEntryInfoOverrides
    {
        private readonly int? size;
        private readonly DateTime? updated;
        private readonly string name;
        private readonly DirectoryFlags? flags;

        public DirectoryEntryInfoOverrides(int? size = null, DateTime? updated = null, string name = null, DirectoryFlags? flags = null)
        {
            this.size = size;
            this.updated = updated;
            this.name = name;
            this.flags = flags;
        }

        public int? Size => this.size;

        public DateTime? Updated => this.updated;

        public string Name => this.name;

        public DirectoryFlags? Flags => this.flags;
    }
}
