using System;
using System.Runtime.InteropServices;

namespace Poweroff
{
    class Program
    {
        #region Imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);
        [DllImport("ntdll.dll")]
        private static extern int NtShutdownSystem(uint ShutdownAction);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
        #endregion

        #region Constants for privileges
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        #endregion

        #region Structures for token manipulation
        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }
        #endregion

        #region Function to enable shutdown privilege
        private static void EnableShutdownPrivilege()
        {
            IntPtr hToken = IntPtr.Zero;
            TOKEN_PRIVILEGES tkp = new TOKEN_PRIVILEGES();
            LUID luid = new LUID();

            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                    throw new Exception("Failed to open process token.");

                if (!LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out luid))
                    throw new Exception("Failed to lookup privilege value.");

                tkp.PrivilegeCount = 1;
                tkp.Privileges.Luid = luid;
                tkp.Privileges.Attributes = SE_PRIVILEGE_ENABLED;

                if (!AdjustTokenPrivileges(hToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero))
                    throw new Exception("Failed to adjust token privileges.");
            }
            finally
            {
                if (hToken != IntPtr.Zero)
                    CloseHandle(hToken);
            }
        }
        #endregion

        static void Main(string[] args)
        {
            if (args.Length == 0 || args == null)
            {
                Console.WriteLine("Poweroff by Ilonic\n-p poweroff\n-r reboot");
                return;
            }

            if (args[0] == "-p")
            {
                NtShutdownSystem(0);
            }
            else if (args[0] == "-r")
            {
                NtShutdownSystem(1);
            }
            else
            {
                Console.WriteLine("Invalid argument. Use -p for poweroff or -r for reboot.");
                return;
            }
        }
    }
}
