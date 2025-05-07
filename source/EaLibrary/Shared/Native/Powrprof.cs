using System.Runtime.InteropServices;

namespace Playnite.Native;

public class Powrprof
{
    private const string dllName = "Powrprof.dll";

    [DllImport(dllName, CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
}
