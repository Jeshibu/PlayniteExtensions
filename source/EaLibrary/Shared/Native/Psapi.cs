using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Playnite.Native;

public class Psapi
{
    private const string dllName = "Psapi.dll";

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetMappedFileName(IntPtr hProcess, IntPtr lpv, StringBuilder lpFilename, int nSize);
}
