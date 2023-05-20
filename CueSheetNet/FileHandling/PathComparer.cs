using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace CueSheetNet.FileHandling;


internal partial class PathComparer : EqualityComparer<FileSystemInfo>, IComparer<FileSystemInfo>
{
    [GeneratedRegex(@"[\\/]")]
    private static partial Regex Morpher();
    [GeneratedRegex(@"[\\/]+$")]
    private static partial Regex Trimmer();
    private static readonly string Separator = Path.DirectorySeparatorChar.ToString();
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
        string norm = Morpher().Replace(path, Separator)
            .ToUpperInvariant();
        return Trimmer().Replace(norm, string.Empty);
    }
    public override bool Equals(FileSystemInfo? x, FileSystemInfo? y) => NormalizePath(x?.FullName) == NormalizePath(y?.FullName);
    public override int GetHashCode([DisallowNull] FileSystemInfo obj) => NormalizePath(obj.FullName).GetHashCode();
    public int Compare(FileSystemInfo? x, FileSystemInfo? y) => string.Compare(NormalizePath(x?.FullName), NormalizePath(y?.FullName));
}
