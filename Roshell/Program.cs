using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobloxConsoleExplorer
{
    internal class Program
    {
        public static bool init = false;
        static void Main(string[] args)
        {
            Console.Title = "RoShell";
            while (true)
            {
                Command();
            }
        }

        static void Command()
        {
            Console.ForegroundColor = ConsoleColor.Green;

            if (init)
            {
                Console.Write("roshell");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(":");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(Explorer.ActualEmplacement);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("$ ");

                Mem.GetProcess("RobloxPlayerBeta");
                Cache.RobloxProcess = Process.GetProcessById(Mem.PID);

                Cache.DataModel = Mem.Read<IntPtr>(Mem.BaseAddress + Offsets.FakeDataModel);
                Cache.DataModel = Mem.Read<IntPtr>(Cache.DataModel + Offsets.FakeToRealDataModel);

                Console.Title = "roshell:" + Explorer.ActualEmplacement;
            }
            else
            {
                
                Console.Write("roshell");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("$ ");

                Console.Title = "roshell";
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            string cmd = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.White;

            InterpretCommand(cmd);

        }

        public static void InterpretCommand(string cmd)
        {

            List<string> partsList = cmd.Split(' ').ToList();
            for (int i = 0; i < partsList.Count; i++)
                partsList[i] = partsList[i].Replace("%", " ");
            string[] partsArr = partsList.ToArray();

            try
            {
                switch (cmd)
                {
                    case "cls": ClearConsole(); return;
                    case "init": Initialise(); return;
                }

                if (!init)
                {
                    Console.WriteLine("Not initialized. Type 'init'");
                    return;
                }
                    

                switch (cmd)
                {
                    case string s when s.StartsWith("ls"): ListFiles(partsArr); return;
                    case string s when s.StartsWith("cd"): ChangeDirectory(partsArr); break;
                    case string s when s.StartsWith("savebtc"): SaveBytecode(partsArr); break;
                    case string s when s.StartsWith("writebtc"): WriteBytecode(partsArr); break;
                    case string s when s.StartsWith("hkwritebtc"): HookWriteBytecode(partsArr); break;
                    case string s when s.StartsWith("viewscript"): ViewScript(partsArr); break;
                    case string s when s.StartsWith("savescript"): SaveScript(partsArr); break;
                    case string s when s.StartsWith("compile"): Compile(partsArr); break;
                    case string s when s.StartsWith("setvalue"): SetValue(partsArr); break;
                    case string s when s.StartsWith("getvalue"): GetValue(partsArr); break;
                    case string s when s.StartsWith("rpm"): Rpm(partsArr); break;
                    case string s when s.StartsWith("wpm"): Wpm(partsArr); break;


                    default:
                        Console.WriteLine("Unknown command");
                        break;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
            }

            /*if (cmd.StartsWith("prop "))
            {
                string path = cmd.Substring(5);

                IntPtr interpreted = Explorer.InterpretPath(path);

                if (interpreted == IntPtr.Zero)
                {
                    Console.WriteLine("Instance not found");
                    return;
                }

                string className = Roblox.GetClassName(interpreted);
                string name = Roblox.GetName(interpreted);
                Console.WriteLine("Address : " + interpreted);
                Console.WriteLine("Name : " + name);
                Console.WriteLine("Classname : " + className);
            }*/
        }

        static string DecompileBytecode(byte[] bytecode)
        {
            HttpClient client = new HttpClient();
            var content = new ByteArrayContent(bytecode);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

            var response = client.PostAsync("http://api.plusgiant5.com/konstant/decompile", content).Result;
            if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStringAsync().Result;
            return null;
        }

        static void ClearConsole()
        {
            Console.Clear();
            return;
        }

        static void Initialise()
        {
            Console.WriteLine("Initializing...");
            if (Explorer.Init())
            {
                Console.WriteLine("Success");
                init = true;
                Explorer.ActualAddress = Cache.DataModel;
            }
            else
            {
                Console.WriteLine("Failed");
                init = false;
            }
        }

        static void ListFiles(string[] parts)
        {
            IntPtr address = IntPtr.Zero;

            if (parts.Length == 1)
                address = Explorer.ActualAddress;
            else if (parts.Length == 2)
                address = Explorer.InterpretPath(parts[1]);
            else
            {
                Console.WriteLine("Incorrect command.\n Usage : ls [instance path (optional)]");
            }

            List<IntPtr> children = Roblox.FindChildren(address);
            foreach (IntPtr child in children)
            {
                string className = Roblox.GetClassName(child);
                string name = Roblox.GetName(child);
                string addressS = child.ToString("X");
                Console.Write(addressS);
                for (int i = 0; i < 20 - addressS.Length; i++) Console.Write(" ");
                Console.Write(className);
                for (int i = 0; i < 30 - className.Length; i++) Console.Write(" ");
                Console.WriteLine(name);
                //Console.WriteLine($"{name} <{className}> <0x{}>");
            }

        }

        static void ChangeDirectory(string[] parts)
        {
            if (parts.Length != 2)
            {
                Console.WriteLine("Incorrect command\nUsage : cd [instance name/path]");
                return;
            }

            if (parts[1] != "..")
            {
                IntPtr interpreted = Explorer.InterpretPath(parts[1]);

                if (interpreted == IntPtr.Zero)
                {
                    Console.WriteLine("Instance not found");
                    return;
                }

                Explorer.ActualAddress = interpreted;
                Explorer.ActualEmplacement = Explorer.GetActualEmplacement();
            }
            else
            {
                IntPtr parent = Mem.Read<IntPtr>(Explorer.ActualAddress + Offsets.Parent);
                if (parent == IntPtr.Zero)
                {
                    Console.WriteLine("This instance doesnt have parent");
                    return;
                }

                Explorer.ActualAddress = parent;
                Explorer.ActualEmplacement = Explorer.GetActualEmplacement();
            }
        }

        static void SaveBytecode(string[] parts)
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : savebtc [instance name/path] [file path]");
                return;
            }


            string toSave = parts[1];
            string saveFile = parts[2];

            IntPtr interpreted = Explorer.InterpretPath(toSave);

            if (interpreted == IntPtr.Zero)
            {
                Console.WriteLine("Instance not found");
                return;
            }

            byte[] byteCode = Roblox.GetBytecode(interpreted);
            File.WriteAllBytes(saveFile, byteCode);
            Console.WriteLine("Bytecode saved in " + saveFile + " (length : " + byteCode.Length + ")");
        }

        static void HookWriteBytecode(string[] parts)
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : hkwritebtc [file path] [instance name/path]");
                return;
            }

            string instance = parts[2];
            string file = parts[1];
            byte[] byteCode = File.ReadAllBytes(file);

            Console.WriteLine("Waiting for script...");
            IntPtr interpreted = Explorer.InterpretPath(instance);
            while (interpreted == IntPtr.Zero)
            {
                interpreted = Explorer.InterpretPath(instance);
                Thread.Sleep(1);
            }

            
            bool modified = Roblox.ModifyBytecode(interpreted, byteCode);

            Console.WriteLine("Bytecode wrote successfuly in (length : " + byteCode.Length + ")");
        }

        static void WriteBytecode(string[] parts)
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : writebtc [file path] [instance name/path]");
                return;
            }

            string instance = parts[2];
            string file = parts[1];

            IntPtr interpreted = Explorer.InterpretPath(instance);

            if (interpreted == IntPtr.Zero)
            {
                Console.WriteLine("Instance not found");
                return;
            }

            byte[] byteCode = File.ReadAllBytes(file);
            bool modified = Roblox.ModifyBytecode(interpreted, byteCode);

            Console.WriteLine("Bytecode wrote successfuly in (length : " + byteCode.Length + ")");
        }

        static void ViewScript(string[] parts)
        {
            if (parts.Length != 2)
            {
                Console.WriteLine("Incorrect command\nUsage : viewscript [instance name/path]");
                return;
            }
            
            string instance = parts[1];
            IntPtr interpreted = Explorer.InterpretPath(instance);

            byte[] byteCode = Roblox.GetBytecode(interpreted);
            if (byteCode == null || byteCode.Length == 0)
            {
                Console.WriteLine("Cant get the bytecode");
                return;
            }

            byte[] decompressed = Roblox.Decompress(byteCode);
            if (decompressed == null || decompressed.Length == 0)
            {
                Console.WriteLine("Cant decompress the bytecode");
                return;
            }

            string decompiled = DecompileBytecode(decompressed);
            if (decompiled == null)
            {
                Console.WriteLine("Failed decompiling the bytecode");
                return;
            }
            //Ugc\Workspace\ATVs\Lawn Mower

            Console.WriteLine(decompiled);
        }

        static void SaveScript(string[] parts)
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : savescript [instance name/path] [file path]");
                return;
            }

            string instance = parts[1];
            string path = parts[2];

            IntPtr interpreted = Explorer.InterpretPath(instance);

            byte[] byteCode = Roblox.GetBytecode(interpreted);
            if (byteCode == null || byteCode.Length == 0)
            {
                Console.WriteLine("Cant get the bytecode");
                return;
            }

            byte[] decompressed = Roblox.Decompress(byteCode);
            if (decompressed == null || decompressed.Length == 0)
            {
                Console.WriteLine("Cant decompress the bytecode");
                return;
            }

            string decompiled = DecompileBytecode(decompressed);
            if (decompiled == null)
            {
                Console.WriteLine("Failed decompiling the bytecode");
                return;
            }

            File.WriteAllText(path, decompiled);
            //Ugc\Workspace\ATVs\Lawn Mower

            Console.WriteLine("Successfuly saved the script in " + path);
        }

        static void Compile(string[] parts)
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : compile [luau file path] [output file path]");
                return;
            }
            byte[] compiled = Roblox.Compile(File.ReadAllText(parts[1]));
            File.WriteAllBytes(parts[2], compiled);

            Console.WriteLine("Script successfuly compiled in " + parts[2] + " Length : " + compiled.Length.ToString());

        }

        static void SetValue(string[] parts)
        {
            //setvalue [path] [type] [value]
            if (parts.Length != 4)
            {
                Console.WriteLine("Incorrect command\nUsage : setvalue [path] [type] [value]");
                return;
            }

            IntPtr interpreted = Explorer.InterpretPath(parts[1]);

            if (interpreted == IntPtr.Zero)
            {
                Console.WriteLine("Instance not found");
                return;
            }

            string className = Roblox.GetClassName(interpreted);
            if (!className.Contains("Value"))
            {
                Console.WriteLine("Instance isnt a Value");
                return;
            }
            //get value type
            if (parts[2] == "int")
            {
                int value = Convert.ToInt32(parts[3]);
                Mem.Write<int>(interpreted + 0xD0, value);
            }
            else if (parts[2] == "bool")
            {
                bool value = Convert.ToBoolean(parts[3]);
                Mem.Write<bool>(interpreted + 0xD0, value);
            }
            else if (parts[2] == "float")
            {
                float value = Convert.ToSingle(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                Mem.Write<float>(interpreted + 0xD0, value);
            }
            else if (parts[2] == "double")
            {
                double value = Convert.ToDouble(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                Mem.Write<double>(interpreted + 0xD0, value);
            }
            else if (parts[2] == "string")
            {
                Mem.WriteString(interpreted + 0xD0, parts[3]);
            }
            else if (parts[2] == "long")
            {
                long value = Convert.ToInt64(parts[3]);
                Mem.Write<long>(interpreted + 0xD0, value);
            }
        }

        static void GetValue(string[] parts)
        {
            //getvalue [path] [type]
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : getvalue [path] [type]");
                return;
            }

            IntPtr interpreted = Explorer.InterpretPath(parts[1]);

            if (interpreted == IntPtr.Zero)
            {
                Console.WriteLine("Instance not found");
                return;
            }

            string className = Roblox.GetClassName(interpreted);
            if (!className.Contains("Value"))
            {
                Console.WriteLine("Instance isnt a Value");
                return;
            }

            object value = new object();

            switch (parts[2])
            {
                case "int": value = Mem.Read<int>(interpreted + 0xD0); break;
                case "bool": value = Mem.Read<bool>(interpreted + 0xD0); break;
                case "float": value = Mem.Read<float>(interpreted + 0xD0); break;
                case "double": value = BitConverter.ToDouble(Mem.ReadBytes(interpreted + 0xD0, 8), 0); break;
                case "string": value = Roblox.RobloxReadString(interpreted + 0xD0); break;
                case "long": value = Mem.Read<long>(interpreted + 0xD0); break;

                default:
                    Console.WriteLine("Unknown variable type");
                    return;

            }

            Console.WriteLine("Value as (" + parts[2] + ") = " + value.ToString());
        }

        static void Rpm(string[] parts)
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : rpm [offset (hex)] [length]");
                return;
            }

            //convert the offset string into offset integer
            int offset = Convert.ToInt32(parts[1], 16);
            int length = Convert.ToInt32(parts[2]);
            byte[] bytes = Mem.ReadBytes(Explorer.ActualAddress + offset, length);
            foreach (byte b in bytes)
                Console.Write(b.ToString("X"));
            Console.Write('\n');
        }

        static void Wpm(string[] parts)
        {
            if (parts.Length != 3)
            {
                Console.WriteLine("Incorrect command\nUsage : wpm [offset (hex)] [bytes (hex)]");
                return;
            }

            //convert the offset string into offset integer
            int offset = Convert.ToInt32(parts[1], 16);

            

            //convert bytes string to byte array
            string byteArrayStr = parts[2];

            if (byteArrayStr.Length % 2 != 0)
            {
                Console.WriteLine("Hex string must have even length");
                return;
            }

            List<byte> bytesList = new List<byte>();
            for (int i = 0; i < parts[2].Length; i+=2)
            {
                string byteStr = byteArrayStr[i].ToString() + byteArrayStr[i + 1].ToString();
                bytesList.Add(Convert.ToByte(byteStr, 16));
            }

            IntPtr address = Explorer.ActualAddress + offset;
            Mem.WriteBytes(address, bytesList.ToArray());

            Console.WriteLine($"Successfuly wrote {bytesList.Count} bytes to 0x{address.ToString("X")}");
        }
    }
}
