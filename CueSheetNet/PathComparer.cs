using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CueSheetNet;

public static class StringNormalization
{
    [return: NotNullIfNotNull(nameof(input))]
    /// <summary>
    /// Replace compound characters with their components. Don't be to aggresive, e.g. long S will remain as is, not be converted to standard S, ligatures and superscripts remain.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string? NormalizeString(this string? input)
    {
        if (input == null) return null;
        //Replace compound characters with their components. Don't be to aggresive, e.g. long S will remain as is, not be converted to standard S, ligatures and superscripts remain.
        string norm = input.Normalize(NormalizationForm.FormD);
        StringBuilder sbd = new();
        foreach (char c in norm)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sbd.Append(c);
        }
        return sbd.ToString().Normalize(NormalizationForm.FormC);
    }

}

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
        path = path.NormalizeString();
        string norm = Morpher().Replace(path, Separator)
            .ToUpperInvariant();
        return Trimmer().Replace(norm, string.Empty);
    }
    public override bool Equals(FileSystemInfo? x, FileSystemInfo? y) => NormalizePath(x?.FullName) == NormalizePath(y?.FullName);
    public override int GetHashCode([DisallowNull] FileSystemInfo obj) => NormalizePath(obj.FullName).GetHashCode();
    public int Compare(FileSystemInfo? x, FileSystemInfo? y) => string.Compare(NormalizePath(x?.FullName), NormalizePath(y?.FullName));
}
