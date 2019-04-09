using System.Runtime.InteropServices;

namespace FS.Directory
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct DirectoryHeader
    {
        [FieldOffset(0)]
        public int NameBlockIndex;

        [FieldOffset(4)]
        public int FirstEmptyItemOffset;

        [FieldOffset(8)]
        public int ItemsCount;

        [FieldOffset(12)]
        public int LastNameOffset;

        [FieldOffset(16)]
        public int ParentDirectoryBlockIndex;
    }
}
