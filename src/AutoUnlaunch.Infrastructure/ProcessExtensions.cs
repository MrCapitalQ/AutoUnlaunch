using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal static partial class ProcessExtensions
{
    public static int GetParentProcessId(this Process process)
    {
        try
        {
            var pi = new ProcessInformation();
            var status = NtQueryInformationProcess(process.Handle, 0, ref pi, Marshal.SizeOf(pi), out var _);
            if (status != 0)
                return 0;

            return pi.InheritedFromUniqueProcessId.ToInt32();
        }
        catch (Exception)
        {
            return 0;
        }
    }

    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationProcess(IntPtr processHandle,
        int processInformationClass,
        ref ProcessInformation processInformation,
        int processInformationLength,
        out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }
}
