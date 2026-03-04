using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RobloxConsoleExplorer
{
    public class Mem
    {
        public static IntPtr Handle = IntPtr.Zero;
        public static IntPtr BaseAddress = IntPtr.Zero;
        public static int PID = 0;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int fuckidk, bool a2, int processid);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr prc, IntPtr addy, byte[] buffer, int size, out int writen);

        [DllImport("kernel32.dll")]
        private static extern unsafe bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, void* lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr prc, IntPtr addy, byte[] buffer, int size, out int writen);

        public static bool GetProcess(string procName)
        {
            try
            {
                Process proc = Process.GetProcessesByName(procName)[0];
                Handle = OpenProcess(0x0010 | 0x0020 | 0x0008, false, proc.Id);
                PID = proc.Id;
                BaseAddress = proc.MainModule.BaseAddress;

                return true;
            }
            catch
            {
                return false;
            }
        }

        //optimised
        public static unsafe T Read<T>(IntPtr address) where T : unmanaged
        {
            T value = default;
            int size = sizeof(T);
            ReadProcessMemory(Handle, address, &value, size, out _);
            return value;
        }

        public static bool Write<T>(IntPtr addy, T value) where T : struct // ok
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] buff = new byte[size];
            IntPtr pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(value, pointer, false);
            Marshal.Copy(pointer, buff, 0, size);
            Marshal.FreeHGlobal(pointer);
            return WriteProcessMemory(Handle, addy, buff, size, out int ffff) && ffff == size;
        }

        public static string ReadString(IntPtr address, int length)
        {
            byte[] buffer = new byte[length];
            ReadProcessMemory(Handle, address, buffer, buffer.Length, out int bytesRead);
            return Encoding.UTF8.GetString(buffer);
        }

        public static void WriteString(IntPtr address, string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            WriteProcessMemory(Handle, address, buffer, buffer.Length, out int bytesWritten);
        }

        public static byte[] ReadBytes(IntPtr address, int length)
        {
            byte[] buffer = new byte[length];
            ReadProcessMemory(Handle, address, buffer, buffer.Length, out int bytesRead);
            return buffer;
        }

        public static void WriteBytes(IntPtr address, byte[] value)
        {
            WriteProcessMemory(Handle, address, value, value.Length, out int bytesWritten);
        }

    }
}
