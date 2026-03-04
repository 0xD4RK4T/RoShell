using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobloxConsoleExplorer
{
    

    public class Roblox
    {
        [DllImport("luau.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RBXCompile(string path, string source);

        [DllImport("luau.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern bool RBXDecompress(string path, byte[] data, UIntPtr dataSize);


        public static (int, int) DumpDataModel(int startOffset)
        {
            int offset = startOffset;

            while (true)
            {
                
                IntPtr datamodel = Mem.Read<IntPtr>(Mem.BaseAddress + offset);
                IntPtr pointer1 = Mem.Read<IntPtr>(Mem.BaseAddress + offset + 8);

                if (datamodel != IntPtr.Zero && pointer1 != IntPtr.Zero)
                {
                    IntPtr pointer2 = Mem.Read<IntPtr>(pointer1 + 0x18);  //go in datamodel
                    if (pointer2 != IntPtr.Zero)
                    {
                        IntPtr pointer3 = Mem.Read<IntPtr>(pointer2 + 0x18); //class descriptor
                        if (pointer3 != IntPtr.Zero)
                        {
                            //get the name
                            IntPtr namePtr = Mem.Read<IntPtr>(pointer3 + 0x8);
                            string name = Mem.ReadString(namePtr, 9);

                            if (name == "DataModel")
                            {
                                //find the fake to real
                                for (int i = 0x100; i < 0x200; i++)
                                {
                                    if (Mem.Read<IntPtr>(datamodel + i) == pointer1)
                                        return (offset, i - 8);
                                }
                            }


                        }

                    }
                }

                offset += 4;
            }
        }

        public static List<IntPtr> FindChildren(IntPtr parent)
        {
            List<IntPtr> children = new List<IntPtr>();

            if (parent == IntPtr.Zero)
                return children;

            // start = *(parent + Offsets.Children)
            IntPtr start = Mem.Read<IntPtr>(parent + Offsets.Children);
            if (start == IntPtr.Zero)
                return children;

            // end = *(start + Offsets.Size)
            IntPtr end = Mem.Read<IntPtr>(start + 0x8);

            // ptr = *(start)
            IntPtr current = Mem.Read<IntPtr>(start + 0x0);

            while (current != end)
            {
                IntPtr instancePtr = Mem.Read<IntPtr>(current);
                if (instancePtr != IntPtr.Zero)
                {
                    children.Add(instancePtr);
                }

                current += 0x10;
            }

            return children;
        }

        public static IntPtr FindFirstChild(IntPtr parent, string name)
        {
            List<IntPtr> children = FindChildren(parent);

            foreach (IntPtr childPtr in children)
            {
                if (GetName(childPtr) == name)
                    return childPtr;
            }

            return IntPtr.Zero;
        }

        public static IntPtr GetLocalPlayer()
        {
            IntPtr playersAd = Roblox.FindFirstChild(Cache.DataModel, "Players");
            return Mem.Read<IntPtr>(playersAd + Offsets.LocalPlayer);
        }

        public static string GetName(IntPtr address)
        {
            IntPtr namePtr = Mem.Read<IntPtr>(address + Offsets.Name);
            return RobloxReadString(namePtr);
        }

        public static string GetClassName(IntPtr address)
        {
            IntPtr classDescriptor = Mem.Read<IntPtr>(address + 0x18);
            IntPtr namePtr = Mem.Read<IntPtr>(classDescriptor + 0x8);
            return RobloxReadString(namePtr);
        }

        public static string RobloxReadString(IntPtr address)
        {
            long strSize = (long)Mem.Read<long>(address + 0x10);
            if (strSize >= 0x10)
                address = Mem.Read<IntPtr>(address);

            string strValue = Mem.ReadString(address, (int)strSize);
            return strValue;
        }

        public static void RobloxWriteString(IntPtr address, string value)
        {
            long strSize = (long)Mem.Read<long>(address + 0x10);
            if (strSize >= 0x10)
                address = Mem.Read<IntPtr>(address);

            Mem.WriteString(address, value);
        }

        public static byte[] GetBytecode(IntPtr address)
        {
            string className = Roblox.GetClassName(address);

            int offset = 0;
            if (className == "LocalScript") offset = Offsets.LocalScriptBytecode;
            else if (className == "ModuleScript") offset = Offsets.ModuleScriptBytecode;
            else return null;

            IntPtr byteCodePointer = Mem.Read<IntPtr>(address + offset);
            long byteCodeSize = (long)Mem.Read<long>(byteCodePointer + 0x20);
            IntPtr realByteCodeAddress = Mem.Read<IntPtr>(byteCodePointer + 0x10);

            byte[] byteCode = Mem.ReadBytes(realByteCodeAddress, (int)byteCodeSize);
            return byteCode;
        }

        public static bool ModifyBytecode(IntPtr address, byte[] newBytecode, bool allocate = true)
        {
            Native.SuspendProcess(Cache.RobloxProcess.Id);
            Thread.Sleep(500);

            string className = GetClassName(address);
            int offset = 0;

            if (className == "ModuleScript") offset = Offsets.ModuleScriptBytecode;
            else if (className == "LocalScript") offset = Offsets.LocalScriptBytecode;
            else return false;

            IntPtr byteCodePtr = Mem.Read<IntPtr>(address + offset);
            if (byteCodePtr == IntPtr.Zero)
                return false;

            IntPtr newByteCodePtr = AllocateScriptMemory(newBytecode);
            if (newByteCodePtr == IntPtr.Zero)
                return false;

            //Cache.driver.WriteBytes((long)byteCodePtr, newBytecode);
            Mem.Write<IntPtr>(byteCodePtr + 0x10, newByteCodePtr);
            Mem.Write<long>(byteCodePtr + 0x20, newBytecode.Length);
            Thread.Sleep(500);
            Native.ResumeProcess(Cache.RobloxProcess.Id);

            return true;
        }

        static IntPtr AllocateScriptMemory(byte[] byteCode)
        {
            int length = byteCode.Length;

            IntPtr newByteCodePtr = Native.VirtualAllocEx(Mem.Handle, IntPtr.Zero, (UIntPtr)length, 0x1000 | 0x2000, 0x04);

            if (newByteCodePtr == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            int status = Native.NtWriteVirtualMemory(Mem.Handle, newByteCodePtr, byteCode, (uint)byteCode.Length, IntPtr.Zero);
            if (status != 0)
            {
                return IntPtr.Zero;
            }

            return newByteCodePtr;
        }

        public static byte[] Compile(string source)
        {
            if (File.Exists("cp.bin"))
                File.Delete("cp.bin");

            RBXCompile("cp.bin", source);

            
            if (File.Exists("cp.bin"))
            {
                byte[] content = File.ReadAllBytes("cp.bin");
                return content;
            }
            else
            {
                return new byte[0];
            }
        }

        public static byte[] Decompress(byte[] source)
        {
            if (File.Exists("dp.bin"))
                File.Delete("dp.bin");

            RBXDecompress("dp.bin", source, (UIntPtr)source.Length);

            
            if (File.Exists("dp.bin"))
            {
                byte[] content = File.ReadAllBytes("dp.bin");
                File.Delete("dp.bin");
                return content;
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
