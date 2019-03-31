using System;
using System.Runtime.InteropServices;

namespace FS.Indexes
{
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    internal struct IndexTime
    {
        [FieldOffset(0)]
        public ulong Ticks;
    }

    [Flags]
    internal enum IndexFlags : uint
    {

    }


    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
    internal struct IndexDescriptor
    {
        [FieldOffset(0)]
        public IndexFlags Flags;

        [FieldOffset(4)]
        public uint Size;

        [FieldOffset(8)]
        public IndexTime Created;

        [FieldOffset(16)]
        public IndexTime Updated;

        [FieldOffset(24)]
        public uint Reserved;

        [FieldOffset(28)]
        public uint BlockIndex;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 40)]
    internal struct TopLevelBlockIndexes
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 40)]
        public uint[] Indexes;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 512)]
    internal struct ListItemBlockIndexes
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 128)]
        public uint[] Indexes;
    }
}
