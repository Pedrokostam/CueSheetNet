using CueSheetNet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
    public override bool Equals(FileSystemInfo? x, FileSystemInfo? y) => MorphPath(x?.FullName) == MorphPath(y?.FullName);
    public override int GetHashCode([DisallowNull] FileSystemInfo obj) => MorphPath(obj.FullName).GetHashCode();
}

public class CuePackage
{
    static readonly PathComparer Comparer = new();
    public CueSheet Sheet { get; }
    public FileInfo[] AdditionalFiles { get; }
    public FileInfo[] AllFiles { get; }
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
        HashSet<FileInfo> compareNames = new(Comparer);
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
                //var ttyu = Sheet.Files.Select(x => x.FileInfo).ToArray();
                //bool zzz=Comparer.Equals(ttyu[0], file);
                bool isAudioFile = Sheet.Files.Select(x => x.FileInfo).Contains(file, Comparer);
                if (!isAudioFile)
                    compareNames.Add(file);
            }
        }
        AdditionalFiles = compareNames.ToArray();
        List<FileInfo> alls = new List<FileInfo>();
        if (Sheet.FileInfo is not null)
            alls.Add(Sheet.FileInfo);
        foreach (CueFile file in Sheet.Files)
        {
            if (file.FileInfo is not null)
                alls.Add(file.FileInfo);
        }
        alls.AddRange(AdditionalFiles);
        AllFiles = alls.ToArray();
    }
    private string ParseTreeFormat(string? treeFormat)
    {
        if (treeFormat == null)
            return Path.GetFileNameWithoutExtension(Sheet.FileInfo!.Name);
        Regex formatter = new(@"%(?<property>\w+)%");
        MatchCollection matches = formatter.Matches(treeFormat);
        foreach (Match match in matches)
        {
            string val = match.Value;
            string groupVal = match.Groups["property"].Value;
            if (groupVal.Equals("old", StringComparison.InvariantCultureIgnoreCase))
            {

                treeFormat = treeFormat.Replace(val, Path.GetFileNameWithoutExtension(Sheet.FileInfo!.Name));
                continue;
            }
            var flags = System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
            System.Reflection.PropertyInfo? prop = Sheet.GetType().GetProperty(groupVal, flags);
            ArgumentNullException.ThrowIfNull(prop?.GetValue(Sheet));
            treeFormat = treeFormat.Replace(val, prop.GetValue(Sheet)!.ToString());
        }
        return string.Join("_", treeFormat.Split(Path.GetInvalidPathChars()));
    }
    public CueSheet MoveFiles(string destination, string? name = null) => CopyFiles(true, destination, name);
    public CueSheet CopyFiles(string destination, string? name = null) => CopyFiles(false, destination, name);
    private CueSheet CopyFiles(bool delete, string destination, string? name = null)
    {
        string path = Path.Combine(destination, ParseTreeFormat(name));
        destination = Path.GetDirectoryName(path)!;
        name = Path.GetFileNameWithoutExtension(path);

        ArgumentException.ThrowIfNullOrEmpty(destination);
        name = Path.GetFileNameWithoutExtension(name ?? Sheet.FileInfo?.Name);
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (Comparer.Equals(new DirectoryInfo(destination), Sheet.FileInfo?.Directory))
        {
            throw new ArgumentException("Cannot copy cue file to the same directory.");
        }

        Directory.CreateDirectory(destination);
        List<(string source, string dest)> lista = new()
        {
        };
        if (Sheet.Files.Count == 1)
        {
            FileInfo audio = Sheet.Files[0].FileInfo;
            string p = Path.ChangeExtension(Path.Combine(destination, name), audio.Extension);
            lista.Add((audio.FullName, p));
            Sheet.ChangeFile(0, p);
        }
        else
        {
            for (int i = 0; i < Sheet.Files.Count; i++)
            {
                FileInfo audio = Sheet.Files[i].FileInfo;
                string p = Path.ChangeExtension(Path.Combine(destination, name), audio.Extension);
                lista.Add((audio.FullName, p));
                Sheet.ChangeFile(i, Path.ChangeExtension(Path.Combine(destination, $"{name} {i}"), Sheet.Files[i].FileInfo.Extension));
            }
        }
        foreach (var file in AdditionalFiles)
        {
            string source = file.FullName;
            string dest = Path.ChangeExtension(Path.Combine(destination, name), file.Extension);
            lista.Add((source, dest));
        }
        try
        {
            foreach (var copee in lista)
            {
                File.Copy(copee.source, copee.dest,true);
            }
        }
        finally
        {
            if (delete)
            {

                // remove any copied file
                foreach (var copee in lista)
                {
                    if (File.Exists(copee.dest))
                    {
                        File.Delete(copee.dest);
                    }
                }
                // if destination directory is empty, delete it
                if (!Directory.EnumerateFiles(destination).Any())
                {
                    Directory.Delete(destination);
                }
            }
        }
        var originalPath = Sheet.FileInfo;
        string cuePath = Path.ChangeExtension(Path.Combine(destination, name), "cue");
        Sheet.Save(cuePath);
        // remove original files
        if (!delete)
            return Sheet;
        originalPath?.Delete();
        foreach (var copee in lista)
        {
            File.Delete(copee.source);
        }
        return Sheet;
    }
}
