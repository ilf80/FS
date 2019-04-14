namespace FS.Core.Utils
{
    internal static class Helpers
    {
        public static int ModBaseWithCeiling(int value, int @base)
        {
            return value / @base + (value % @base == 0 ? 0 : 1);
        }

        public static int ModBaseWithFloor(int value, int @base)
        {
            return value / @base;
        }
    }
}
