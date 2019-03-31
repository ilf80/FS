using System;

namespace FS.Utils
{
    public static class Guard
    {
        public static void AgrumentIsNotNull<T>(T arg, string paramName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException($"Argument {paramName} is null");
            }
        }
    }
}
