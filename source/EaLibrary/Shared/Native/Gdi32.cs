using System;
using System.Runtime.InteropServices;

namespace Playnite.Native;

public class Gdi32
{
    private const string dllName = "Gdi32.dll";

    [DllImport(dllName, SetLastError = true)]
    public static extern bool DeleteObject(IntPtr hObject);
}
