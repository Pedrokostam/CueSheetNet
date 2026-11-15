using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Helpers;

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("freebsd")]
internal static partial class FileSystemInfoHelperUnix
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct FileId(long DeviceId, long Inode)
    {
        public override string ToString() => $"{DeviceId}:{Inode}";
    }

    public readonly record struct LastStatus(int ErrorCode)
    {
        public bool IsSuccess => ErrorCode == 0;

        public void Throw()
        {
            if (IsSuccess)
                return;
            // errno is already set by the P/Invoke
            throw new IOException($"stat failed with errno {ErrorCode}",
                new System.ComponentModel.Win32Exception(ErrorCode)); // works on Unix too
        }

        public static LastStatus CreateFromLast() => new(Marshal.GetLastPInvokeError());
        public static LastStatus Success() => new(0);
    }

    // Normalized structure that works for both Linux and macOS
    // (we only care about Dev + Ino)
    [StructLayout(LayoutKind.Sequential)]
    private struct StatBuf
    {
        public long st_dev;     // device ID
        public long st_ino;     // inode number
        // We deliberately ignore the rest of the fields.
        // The layout is not 100% identical across all Unixes,
        // but for the first two 64-bit fields it is stable enough
        // on the platforms .NET supports.
    }

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int stat(string path, out StatBuf buf);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int lstat(string path, out StatBuf buf);

    /// <summary>
    /// Gets a unique file identity (device + inode).
    /// Follows symbolic links (like Windows FILE_ID_INFO normally does).
    /// </summary>
    public static FileId GetFileId(string path)
    {
        var status = TryGetFileId(path, followSymlinks: true, out var id);
        status.Throw();
        return id;
    }

    /// <summary>
    /// Non-throwing version.
    /// </summary>
    public static LastStatus TryGetFileId(string path, bool followSymlinks, out FileId id)
    {
        id = default;
        int result = followSymlinks
            ? stat(path, out StatBuf buf)
            : lstat(path, out buf);

        if (result != 0)
            return LastStatus.CreateFromLast();

        id = new FileId(buf.st_dev, buf.st_ino);
        return LastStatus.Success();
    }
}
