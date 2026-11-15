using CueSheetNet.FileHandling;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.Helpers;

public class FileSystemInfoComparer : IEqualityComparer<FileSystemInfo>, IEqualityComparer<FilePath>, IEqualityComparer<string>, IComparer<FileSystemInfo>, IComparer<FilePath>, IComparer<string>
{
    public static bool IsSystemCaseSensitive
    {
        get
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
            {
                return false;
            }
            return true;
        }
    }
    public static StringComparer DefaultSystemComparer => IsSystemCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    static ILogger Loggerio => Loggingq.EditingLogger;
    public static FileSystemInfoComparer Instance { get; } = new();

    public bool Equals(FileSystemInfo? x, FileSystemInfo? y) => this.Equals(x?.FullName, y?.FullName);
    public bool Equals(FilePath? x, FilePath? y) => this.Equals(x?.Path, y?.Path);
    [TraceExecutionTime]
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
            var statX = FileSystemInfoHelperWindows.TryGetFileId(x, out var infoX);
            var statY = FileSystemInfoHelperWindows.TryGetFileId(y, out var infoY);
            if (!statX.IsSuccess && !statY.IsSuccess)
            {
                // both failed, compare by paths
                var normX = NormalizePath(x, preserveTrailingSeparator: true);
                var normY = NormalizePath(y, preserveTrailingSeparator: true);
                return DefaultSystemComparer.Equals(normX, normY);
            }
            if (statX.IsSuccess && statY.IsSuccess)
            {
                // both exist
                return infoX.Equals(infoY);
            }
            // only one of them exists - the paths cannot point to the same entry
            return false;
        }
        if (System.OperatingSystem.IsLinux())
        {
            var statX = FileSystemInfoHelperUnix.TryGetFileId(x,false, out var infoX);
            var statY = FileSystemInfoHelperUnix.TryGetFileId(y,false, out var infoY);
            if (!statX.IsSuccess && !statY.IsSuccess)
            {
                // both failed, compare by paths
                var normX = NormalizePath(x, preserveTrailingSeparator: true);
                var normY = NormalizePath(y, preserveTrailingSeparator: true);
                return DefaultSystemComparer.Equals(normX, normY);
            }
            if (statX.IsSuccess && statY.IsSuccess)
            {
                // both exist
                return infoX.Equals(infoY);
            }
            // only one of them exists - the paths cannot point to the same entry
            return false;
        }
        throw new PlatformNotSupportedException();
    }

    public int GetHashCode([DisallowNull] FileSystemInfo obj) => GetHashCode(obj.FullName);
    public int GetHashCode([DisallowNull] FilePath obj) => GetHashCode(obj.Path);
    [TraceExecutionTime]
    public int GetHashCode([DisallowNull] string obj) => DefaultSystemComparer.GetHashCode(NormalizePath(obj, preserveTrailingSeparator: true));

    private static string DummyPath
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return @"C:/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/";
            }
            return "/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/";
        }
    }
    public static string NormalizePath(FilePath filePath) => NormalizePath(filePath.Path, null, false);
    public static string NormalizePath(DirectoryInfo dirInfo) => NormalizePath(dirInfo.FullName, null, true);
    public static string NormalizePath(FileInfo fileInfo) => NormalizePath(fileInfo.FullName, null, false);
    public static string NormalizePath(string path, string? basePath = null, bool preserveTrailingSeparator = false)
    {
        if(path is null)
        {
            return null!;
        }
        bool qualified = Path.IsPathFullyQualified(path);
        basePath ??= DummyPath;
        string norm;
        if (qualified)
        {
            norm = Path.GetFullPath(path);
        }
        else
        {
            var s = Path.GetFullPath(path, basePath);
            norm = Path.GetRelativePath(basePath, s);
        }
        if (preserveTrailingSeparator
            && (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)))
        {
            return norm + Path.DirectorySeparatorChar;
        }
        return norm;
    }

    public int Compare(string? x, string? y) => DefaultSystemComparer.Compare(NormalizePath(x!), NormalizePath(y!));

    public int Compare(FilePath? x, FilePath? y) => Compare(x?.Path, y?.Path);
    
    public int Compare(FileSystemInfo? x, FileSystemInfo? y) => Compare(x?.FullName, y?.FullName);
}
