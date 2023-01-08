using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;


internal class PathComparer : EqualityComparer<FileSystemInfo>
{
    [return: NotNullIfNotNull(nameof(p))]
    static string? MorphPath(string? p)
    {
        if (p is null) return null;
        return p.ToUpperInvariant().TrimEnd('/').TrimEnd('\\');
    }
    public override bool Equals(FileSystemInfo? x, FileSystemInfo? y) => MorphPath(x?.FullName) != MorphPath(y?.FullName);
    public override int GetHashCode([DisallowNull] FileSystemInfo obj) => MorphPath(obj.FullName).GetHashCode();
}

public class CuePackage
{
    static readonly PathComparer Comparer = new();
    public CueSheet Sheet { get; }
    public FileInfo[] AdditionalFiles { get; }

    public static string NormalizeString(string input)
    {
        //Replace compound characters with their components. Don't be to aggresive, e.g. long S will remain as is, not be converted to standard S, ligatures and superscripts remain.
        string norm = input.Normalize(NormalizationForm.FormD);
        StringBuilder sbd = new StringBuilder();
        //
        foreach (char c in norm)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sbd.Append(c);
        }
        return sbd.ToString().Normalize(NormalizationForm.FormC);
    }

    public CuePackage(CueSheet sheet)
    {
        Sheet = sheet;
        if (Sheet.LastFile is null || Sheet.LastFile.FileInfo.DirectoryName is null)
        {
            AdditionalFiles = Array.Empty<FileInfo>();
            return;
        }
        DirectoryInfo? sheetDir = Sheet.FileInfo?.DirectoryName is not null ? new(Sheet.FileInfo.DirectoryName) : null;
        DirectoryInfo overdir = new(Sheet.LastFile.FileInfo.DirectoryName);
        IEnumerable<FileInfo> files = overdir.EnumerateFiles();
        if (sheetDir is not null && Comparer.Equals(overdir, sheetDir))
        {
            files = files.Concat(sheetDir.EnumerateFiles());
        }
        List<FileInfo> compareNames = new();
        string name;
        if (Sheet.FileInfo is null)
        {
            if (Sheet.LastFile is not null)
                name = Path.GetFileNameWithoutExtension(Sheet.LastFile.FileInfo.Name);
            else
                name = $"{Sheet.Performer} - {Sheet.Title}";
        }
        else
            name = Path.GetFileNameWithoutExtension(Sheet.FileInfo.Name);
        string noSpaceName = name.Replace(" ", "");
        string underscoreName = name.Replace(' ', '_');
        HashSet<string> hs = new(StringComparer.InvariantCultureIgnoreCase) {
            name,
            NormalizeString(name),
            noSpaceName,
            NormalizeString(noSpaceName),
            underscoreName,
            NormalizeString(underscoreName)
        };
        foreach (FileInfo file in files)
        {
            if (hs.Contains(Path.GetFileNameWithoutExtension(file.Name)))
            {
                compareNames.Add(file);
            }
        }
        AdditionalFiles = compareNames.ToArray();


    }
}
