using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RobloxConsoleExplorer
{
    public class Native
    {
        public static uint MEM_COMMIT = 0x1000;
        public static uint MEM_RESERVE = 0x2000;
        public static uint PAGE_READWRITE = 0x04;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flAllocationType,
            uint flProtect
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flNewProtect,
            out uint lpflOldProtect
        );

        [DllImport("ntdll.dll")]
        public static extern int NtWriteVirtualMemory(
            IntPtr ProcessHandle,
            IntPtr BaseAddress,
            byte[] Buffer,
            uint NumberOfBytesToWrite,
            IntPtr NumberOfBytesWritten // peut être null
        );



        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        public static void SuspendProcess(int processId)
        {
            Process process = Process.GetProcessById(processId);

            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(0x0002, false, (uint)thread.Id);

                if (pOpenThread != IntPtr.Zero)
                {
                    SuspendThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
            }
        }

        public static void ResumeProcess(int processId)
        {
            Process process = Process.GetProcessById(processId);

            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(0x0002, false, (uint)thread.Id);
                if (pOpenThread != IntPtr.Zero)
                {
                    ResumeThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
            }
        }
    }
}
