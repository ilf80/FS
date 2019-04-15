using System.Runtime.InteropServices;

namespace FS.Core
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct DirectoryEntryStruct
    {
        [FieldOffset(0)] public DirectoryFlags Flags;

        [FieldOffset(4)] public int Size;

        [FieldOffset(8)] public long Created;

        [FieldOffset(16)] public long Updated;

        [FieldOffset(24)] public int NameOffset;

        [FieldOffset(28)] public int BlockIndex;
    }
}