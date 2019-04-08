using System.Runtime.InteropServices;

namespace FS.Directory
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct DirectoryItem
    {
        [FieldOffset(0)]
        public DirectoryEntry Entry;

        [FieldOffset(0)]
        public DirectoryHeader Header;
    }
}
