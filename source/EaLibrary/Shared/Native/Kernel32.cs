using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Playnite.Native;

public class Kernel32
{
    private const string dllName = "Kernel32.dll";

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Auto)]
    public extern static uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

    [DllImport(dllName, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public extern static bool CloseHandle(IntPtr hObject);

    [DllImport(dllName, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CreateFile(
        [MarshalAs(UnmanagedType.LPTStr)] string filename,
        [MarshalAs(UnmanagedType.U4)] uint access,
        [MarshalAs(UnmanagedType.U4)] FileShare share,
        IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport(dllName, SetLastError = true)]
    public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport(dllName, SetLastError = true)]
    public static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);

    [DllImport(dllName, SetLastError = true)]
    public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport(dllName, SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, ENUMRESNAMEPROC lpEnumFunc, IntPtr lParam);

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

    [DllImport(dllName, SetLastError = true)]
    public static extern IntPtr LockResource(IntPtr hResData);

    [DllImport(dllName, SetLastError = true)]
    public static extern IntPtr GetCurrentProcess();

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool CreateProcess(
       string lpApplicationName,
       string lpCommandLine,
       ref SECURITY_ATTRIBUTES lpProcessAttributes,
       ref SECURITY_ATTRIBUTES lpThreadAttributes,
       bool bInheritHandles,
       uint dwCreationFlags,
       IntPtr lpEnvironment,
       string lpCurrentDirectory,
       [In] ref STARTUPINFO lpStartupInfo,
       out PROCESS_INFORMATION lpProcessInformation);

    [DllImport(dllName, SetLastError = true)]
    public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

    [DllImport(dllName, CharSet = CharSet.Auto)]
    public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static extern uint GetFileAttributesW(string lpFileName);

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint GetCompressedFileSizeW(
        [In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
        [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

    [DllImport(dllName, SetLastError = true, PreserveSig = true, CharSet = CharSet.Unicode)]
    public static extern int GetDiskFreeSpaceW(
        [In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
        out uint lpSectorsPerCluster,
        out uint lpBytesPerSector,
        out uint lpNumberOfFreeClusters,
        out uint lpTotalNumberOfClusters);

    [DllImport(dllName, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public extern static bool GetVolumeInformationW(
        string rootPathName,
        StringBuilder volumeNameBuffer,
        int volumeNameSize,
        out uint volumeSerialNumber,
        out uint maximumComponentLength,
        out uint fileSystemFlags,
        StringBuilder fileSystemNameBuffer,
        int nFileSystemNameSize);

    //public static extern bool GetVolumeInformationW(
    //    [In] string lpRootPathName,                                                 //[in, optional] LPCWSTR lpRootPathName,
    //    [Out, MarshalAs(UnmanagedType.LPWStr)] out string lpVolumeNameBuffer,       //[out, optional] LPWSTR lpVolumeNameBuffer,
    //    uint nVolumeNameSize,                                                       //[in] DWORD nVolumeNameSize,
    //    out uint lpVolumeSerialNumber,                                              //[out, optional] LPDWORD lpVolumeSerialNumber,
    //    out uint lpMaximumComponentLength,                                          //[out, optional] LPDWORD lpMaximumComponentLength,
    //    out uint lpFileSystemFlags,                                                 //[out, optional] LPDWORD lpFileSystemFlags,
    //    [Out, MarshalAs(UnmanagedType.LPWStr)] out string lpFileSystemNameBuffer,   //[out, optional] LPWSTR lpFileSystemNameBuffer,
    //    uint nFileSystemNameSize);                                                  //[in] DWORD nFileSystemNameSize
}
