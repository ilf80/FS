using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS.Utils
{
    internal static class Helpers
    {
        public static int ModBaseWithCeiling(int value, int @base)
        {
            return value / @base + (value % @base == 0 ? 0 : 1);
        }

        public static int ModBaseWithFloor(int value, int @base)
        {
            //return value / @base - (value % @base == 0 && value != 0 ? 1 : 0);
            return value / @base;
        }
    }
}
