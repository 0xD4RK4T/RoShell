using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxConsoleExplorer
{
    public class Explorer
    {
        public static string ActualEmplacement = "Ugc";
        public static IntPtr ActualAddress = Cache.DataModel;
        public static bool Init()
        {
            try
            {
                if (!Mem.GetProcess("RobloxPlayerBeta"))
                    return false;

                Cache.RobloxProcess = Process.GetProcessById(Mem.PID);

                (int a, int b) = Roblox.DumpDataModel(0x7000000);
                Offsets.FakeDataModel = a;
                Offsets.FakeToRealDataModel = b;
                Cache.DataModel = Mem.Read<IntPtr>(Mem.BaseAddress + a);
                Cache.DataModel = Mem.Read<IntPtr>(Cache.DataModel + b);

                ActualEmplacement = "Ugc";
                return true;
            }
            catch
            {
                return false;
            }
            
        }

        public static IntPtr InterpretPath(string path)
        {
            if (!path.StartsWith("Ugc"))
                path = ActualEmplacement + "\\" + path;

            if (path == "Ugc")
                return Cache.DataModel;

            IntPtr startAddress = IntPtr.Zero;

            string[] allNames = path.Split('\\');

            foreach (string name in allNames)
            {
                if (name == "Ugc")
                {
                    startAddress = Cache.DataModel;
                    continue;
                }

                startAddress = Roblox.FindFirstChild(startAddress, name);
                if (startAddress == IntPtr.Zero) return IntPtr.Zero;

            }

            return startAddress;
        }

        public static string GetActualEmplacement()
        {
            string path = "";
            string name = "";

            IntPtr address = ActualAddress;
            name = Roblox.GetName(ActualAddress);

            if (name == "Ugc")
                return "Ugc";

            while (name != "Ugc")
            {
                name = Roblox.GetName(address);
                address = Mem.Read<IntPtr>(address + Offsets.Parent);
                path = name + "\\" + path;
            }

            return path.Remove(path.Length - 1, 1);
        }

        public static List<string> ListFiles(string path = "null")
        {
            if (path == "null") path = ActualEmplacement;
            List<string> files = new List<string>();

            IntPtr interpretedPath = InterpretPath(path);

            if (interpretedPath ==  IntPtr.Zero)
                return files;

            List<IntPtr> children = Roblox.FindChildren(interpretedPath);
            foreach (IntPtr child in children)
                files.Add(Roblox.GetName(child));

            return files;
        }
    }
}
