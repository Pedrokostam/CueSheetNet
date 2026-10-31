using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CueSheetNet.Helpers;

public static class FileSystemInfoHelperWindows
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct FILE_ID_INFO(ulong VolumeSerialNumber, FILE_ID_128 FileId)
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct FILE_ID_128(ulong Low, ulong High)
    {
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetFileInformationByHandleEx(
    SafeFileHandle hFile,
    int FileInformationClass,
    out FILE_ID_INFO lpFileInformation,
    uint dwBufferSize);

    const int FileIdInfo = 18;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFile(
    string lpFileName,
    uint dwDesiredAccess,
    uint dwShareMode,
    IntPtr lpSecurityAttributes,
    uint dwCreationDisposition,
    uint dwFlagsAndAttributes,
    IntPtr hTemplateFile);

    const uint FILE_SHARE_READ = 1;
    const uint FILE_SHARE_WRITE = 2;
    const uint FILE_SHARE_DELETE = 4;
    const uint OPEN_EXISTING = 3;
    const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    public static FILE_ID_INFO GetFileId(string path)
    {
        using var handle = CreateFile(
            path,
            0,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);

        if (handle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        if (!GetFileInformationByHandleEx(
            handle, FileIdInfo, out var info,
            (uint)Marshal.SizeOf<FILE_ID_INFO>()))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return info;
    }
}
