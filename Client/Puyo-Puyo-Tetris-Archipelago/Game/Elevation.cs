using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Puyo_Puyo_Tetris_Archipelago.Game
{
    public static class Elevation
    {
        // Error thrown when we don't have the right access
        private const int ERROR_ACCESS_DENIED = 5;

        // Permission to read process memory
        private const uint PROCESS_VM_READ = 0x0010;
        // Permission to read process limited data (globally accessible)
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        // OpenProcess forward
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        // CloseHandle forward
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Is client currently elevated
        public static bool IsElevated
        {
            get
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        // Check if failure was specifically accessdenied
        public static bool OpenFailedAccessDenied(int pid)
        {
            IntPtr handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (handle != IntPtr.Zero)
            {
                CloseHandle(handle);
                return false;
            }

            return Marshal.GetLastWin32Error() == ERROR_ACCESS_DENIED;
        }
    }
}
