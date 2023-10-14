using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CueSheetNet.FileHandling;


internal partial class PathComparer : EqualityComparer<FileSystemInfo>, IComparer<FileSystemInfo>, IComparer<FileInfo>, IComparer<ICueFile>, IEqualityComparer<ICueFile>
{
    [GeneratedRegex(@"[\\/]")]
    private static partial Regex Morpher();
    [GeneratedRegex(@"[\\/]+$")]
    private static partial Regex Trimmer();
    private static readonly string _separator = Path.DirectorySeparatorChar.ToString();
    static public readonly PathComparer Instance = new();

    [return: NotNullIfNotNull(nameof(file))]
    public static string? NormalizePath(FileSystemInfo? file) => NormalizePath(file?.FullName);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(path))]
    public static string? NormalizePath(string? path)
    {
        if (path is null) return null;
        //path = PathStringNormalization.NormalizeString(path);
        string norm = Morpher().Replace(path, _separator);
        return Trimmer().Replace(norm, string.Empty);
    }
    public override bool Equals(FileSystemInfo? x, FileSystemInfo? y)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(
            NormalizePath(x?.FullName),
            NormalizePath(y?.FullName));
    }

    public override int GetHashCode([DisallowNull] FileSystemInfo obj)
    {
        return NormalizePath(obj.FullName)
            .GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public int Compare(FileSystemInfo? x, FileSystemInfo? y)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(
            NormalizePath(x?.FullName),
            NormalizePath(y?.FullName));
    }

    public int Compare(FileInfo? x, FileInfo? y)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(
            NormalizePath(x?.FullName),
            NormalizePath(y?.FullName));
    }

    public int Compare(ICueFile? x, ICueFile? y)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(
            NormalizePath(x?.SourceFile.FullName),
            NormalizePath(y?.SourceFile.FullName));
    }

    public bool Equals(ICueFile? x, ICueFile? y)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(
            NormalizePath(x?.SourceFile.FullName),
            NormalizePath(y?.SourceFile.FullName));
    }

    public int GetHashCode([DisallowNull] ICueFile obj)
    {
        return NormalizePath(obj.SourceFile.FullName)
            .GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
