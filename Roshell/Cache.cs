using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxConsoleExplorer
{
    public class Cache
    {
        public static Process RobloxProcess { get; set; }

        public static IntPtr BaseAddress = IntPtr.Zero;
        public static IntPtr DataModel = IntPtr.Zero;
    }
}
