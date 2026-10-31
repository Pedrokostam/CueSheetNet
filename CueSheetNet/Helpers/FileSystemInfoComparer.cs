using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.Helpers;

public class FileSystemInfoComparer : EqualityComparer<FileSystemInfo>, IEqualityComparer<string>
{
    public override bool Equals(FileSystemInfo? x, FileSystemInfo? y)
    {
        if (!Type.Equals(x?.GetType(), y?.GetType()))
        {
            return false;
        }
        return this.Equals(x?.FullName, y?.FullName);
    }

    public bool Equals(string? x, string? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        if (System.OperatingSystem.IsWindows())
        {
            var fidX = FileSystemInfoHelperWindows.GetFileId(x);
            var fidY = FileSystemInfoHelperWindows.GetFileId(y);
            return fidX.Equals(fidY);
        }
        if (System.OperatingSystem.IsLinux())
        {
            throw new NotImplementedException();
        }
        throw new PlatformNotSupportedException();
    }

    public override int GetHashCode([DisallowNull] FileSystemInfo obj)
    {
        if (System.OperatingSystem.IsWindows())
        {
            return FileSystemInfoHelperWindows.GetFileId(obj.FullName).GetHashCode();
        }

        if (System.OperatingSystem.IsLinux())
        {
            throw new NotImplementedException();
        }
        throw new PlatformNotSupportedException();
    }

    public int GetHashCode([DisallowNull] string obj)
    {
        if (System.OperatingSystem.IsWindows())
        {
            return FileSystemInfoHelperWindows.GetFileId(obj).GetHashCode();
        }

        if (System.OperatingSystem.IsLinux())
        {
            throw new NotImplementedException();
        }
        throw new PlatformNotSupportedException();
    }
}
