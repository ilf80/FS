using System;

namespace FS.Directory
{
    internal sealed class DirectoryEntryInfoOverrides : IDirectoryEntryInfoOverrides
    {
        private readonly int? size;
        private readonly DateTime? updated;
        private readonly string name;

        public DirectoryEntryInfoOverrides(int? size, DateTime? updated, string name)
        {
            this.size = size;
            this.updated = updated;
            this.name = name;
        }

        public int? Size => this.size;

        public DateTime? Updated => this.updated;

        public string Name => this.name;
    }
}
