using System;
using System.Runtime.InteropServices;

namespace Playnite.Native;

public class Shell32
{
    private const string dllName = "Shell32.dll";

    [DllImport(dllName)]
    public extern static int ExtractIconEx(string libName, int iconIndex, IntPtr[] largeIcon, IntPtr[] smallIcon, uint nIcons);
}
