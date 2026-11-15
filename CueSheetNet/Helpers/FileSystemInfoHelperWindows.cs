using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CueSheetNet.Helpers;

[SupportedOSPlatform("windows")]
internal static partial class FileSystemInfoHelperWindows
{
    public readonly record struct LastStatus(int ErrorCode)
    {
        public bool IsSuccess => ErrorCode == 0;
        /// <summary>
        /// Throws the <see cref="Win32Exception"/> associated with its <see cref="ErrorCode"/>, but only if the error code is not 0.
        /// </summary>
        /// <exception cref="Win32Exception"></exception>
        public void Throw()
        {
            if (IsSuccess)
                return;
            throw new Win32Exception(ErrorCode);
        }
        public static LastStatus CreateFromLast() => new LastStatus(Marshal.GetLastWin32Error());
        public static LastStatus Success() => new LastStatus(0);
    }
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct FILE_ID_INFO(ulong VolumeSerialNumber, FILE_ID_128 FileId)
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct FILE_ID_128(ulong Low, ulong High)
    {
    }
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetFileInformationByHandleEx(SafeFileHandle hFile,
                                                    int FileInformationClass,
                                                    out FILE_ID_INFO lpFileInformation,
                                                    uint dwBufferSize);

    const int FileIdInfo = 18;

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial SafeFileHandle CreateFile(string lpFileName,
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
        var status = TryGetFileId(path, out var info);
        status.Throw();
        return info;
    }
    public static LastStatus TryGetFileId(string path, out FILE_ID_INFO info)
    {
        info = default;
        using var handle = CreateFile(
            path,
            0,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);
        if (handle.IsInvalid)
            return LastStatus.CreateFromLast();

        if (!GetFileInformationByHandleEx(
            handle, FileIdInfo, out info,
            (uint)Marshal.SizeOf<FILE_ID_INFO>()))
        {
            return LastStatus.CreateFromLast();
        }
        return LastStatus.Success();
    }
}
