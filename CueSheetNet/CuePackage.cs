using CueSheetNet;
using CueSheetNet.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace CueSheetNet;

/// <summary>
/// Class which takes care of file operations related to the CueSheet passed to the constructor.
/// </summary>
public partial class CuePackage
{
    private static readonly Dictionary<string,string> CommonSynonyms=new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"ARTIST","Performer" },
        {"YEAR","Date" },
        {"ALBUM","Title" },
    };
    public CueSheet Sheet { get; }
    private FileInfo[] _AdditionalFiles { get; set; }
    public ReadOnlyCollection<FileInfo> AdditionalFiles => _AdditionalFiles.AsReadOnly();

    /// <summary>
    /// Gets all files related to the cuesheet: Sheet file, audio files, additional files.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<FileInfo> GetAllFiles()
    {
        IEnumerable<FileInfo> audiofiles = from file in Sheet.Files
                                           let finfo = file.FileInfo
                                           where finfo != null
                                           select finfo;
        if (Sheet.SourceFile is not null)
            audiofiles = audiofiles.Prepend(Sheet.SourceFile);
        return audiofiles.Concat(AdditionalFiles);
    }
    /// <summary>
    /// Replace compound characters with their components. Don't be to aggresive, e.g. long S will remain as is, not be converted to standard S, ligatures and superscripts remain.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string NormalizeString(string input)
    {
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
    /// <summary>
    /// Finds all files related to the CueSheet
    /// </summary>
    [MemberNotNull(nameof(_AdditionalFiles))]
    public void RefreshFiles()
    {
        //If there are no files or directory of last file is null, return - no files
        if (Sheet.LastFile is null || Sheet.LastFile?.FileInfo?.DirectoryName is null)
        {
            _AdditionalFiles = Array.Empty<FileInfo>();
            return;
        }
        // All variations of base name of cuesheet
        HashSet<string> matchStrings = GetMatchStringHashset();

        //Where the sheet is located
        DirectoryInfo? sheetDir = Sheet.SourceFile?.Directory;
        //Where the last audio file is located
        DirectoryInfo? audioDir = Sheet.LastFile.FileInfo.Directory;
        IEnumerable<FileInfo> siblingFiles = audioDir?.EnumerateFiles() ?? Enumerable.Empty<FileInfo>();

        // If audio dir and sheet dir are different, concatenate file sequences
        if (!PathComparer.Instance.Equals(sheetDir, audioDir)
            && sheetDir?.EnumerateFiles() is IEnumerable<FileSystemInfo> audioSiblings)
        {
            siblingFiles = siblingFiles.Concat(sheetDir.EnumerateFiles());
        }

        // Case-insensitive hashset to get only unique filepaths. Should work with case-sensitive filesystems,
        // since what is added is directly enumerated file.
        HashSet<FileInfo> compareNames = new(PathComparer.Instance);
        foreach (FileInfo file in siblingFiles)
        {
            if (matchStrings.Contains(Path.GetFileNameWithoutExtension(file.Name)))
            {
                //var ttyu = Sheet.Files.Select(x => x.FileInfo).ToArray();
                //bool zzz=Comparer.Equals(ttyu[0], file);
                bool isAudioFile = Sheet.Files.Select(x => x.FileInfo).Contains(file, PathComparer.Instance);
                if (!isAudioFile)
                    compareNames.Add(file);
            }
        }
        _AdditionalFiles = compareNames.Order(PathComparer.Instance).Cast<FileInfo>().ToArray();
    }

    public CuePackage(CueSheet sheet)
    {
        Sheet = sheet;
        RefreshFiles();
    }
    /// <summary>
    /// Gets collection of unique string matches for file searching
    /// </summary>
    /// <returns>Hashset with unique string matches, both normalized and raw. Hashset uses <see cref="StringComparer.InvariantCultureIgnoreCase"/></returns>
    private HashSet<string> GetMatchStringHashset()
    {
        string baseName = GetBaseNameForSearching();
        string noSpaceName = baseName.Replace(" ", "");
        string underscoreName = baseName.Replace(' ', '_');
        HashSet<string> hs = new(StringComparer.InvariantCultureIgnoreCase) {
            baseName,
            NormalizeString(baseName),
            noSpaceName,
            NormalizeString(noSpaceName),
            underscoreName,
            NormalizeString(underscoreName)
        };
        return hs;
    }
    /// <summary>
    /// Gets the filename of source sheet file, or the filename of last audio file, or "{Performer} - {Title}". All without extension
    /// </summary>
    /// <returns></returns>
    private string GetBaseNameForSearching()
    {
        string name;
        if (Sheet.SourceFile is null)
        {
            if (Sheet.LastFile is not null)
                name = Path.GetFileNameWithoutExtension(Sheet.LastFile.FileInfo.Name);
            else
                name = $"{Sheet.Performer} - {Sheet.Title}";
        }
        else
        {
            name = Path.GetFileNameWithoutExtension(Sheet.SourceFile.Name);
        }

        return name;
    }
    /// <summary>
    /// Parse the format for output filepath. E.g. %Artist%/%DATE%/%Album% can result in
    /// ./Artist/2001/Album.
    /// All invalid characters are replaced with '_'.
    /// Property names do not need to match case.
    /// </summary>
    /// <param name="treeFormat">Pattern whcih may contain reference to properies of CueSheet, case insensitive names. To get the original filename, specify %old%
    /// If parameter is null, the original filename will be used
    /// </param>
    /// <returns></returns>
    private string ParseTreeFormat(string? treeFormat)
    {
        if (treeFormat == null)
            return Path.GetFileNameWithoutExtension(Sheet.SourceFile!.Name);
        Regex formatter = PropertyParser();
        MatchCollection matches = formatter.Matches(treeFormat);
        foreach (Match match in matches)
        {
            string val = match.Value;
            string groupVal = match.Groups["property"].Value;
            if (CommonSynonyms.TryGetValue(groupVal, out string? syn))
            {
                groupVal = syn;
            }
            if (groupVal.Equals("old", StringComparison.InvariantCultureIgnoreCase))
            {

                treeFormat = treeFormat.Replace(val, Path.GetFileNameWithoutExtension(Sheet.SourceFile!.Name));
                continue;
            }
            var flags = System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
            System.Reflection.PropertyInfo? prop = Sheet.GetType().GetProperty(groupVal, flags);
            object? value = prop?.GetValue(Sheet) as object;
            if (value is null)
            {
                Logger.LogVerbose("No matching property found for {Property} when parsing tree format {Format}", val, treeFormat);
                continue;
            }
            treeFormat = treeFormat.Replace(val, value.ToString());
        }
        //replace all invalid patrh chars with underscore
        return string.Join("_", treeFormat.Split(Path.GetInvalidPathChars()));
    }
    public CueSheet MoveFiles(string destination, string? name = null) => CopyFiles(true, destination, name);
    public CueSheet CopyFiles(string destination, string? name = null) => CopyFiles(false, destination, name);
    private CueSheet CopyFiles(bool delete, string destination, string? name = null)
    {
        // Combine Destination with whatever results from parsing (which may contain more directories)
        string path = Path.Combine(destination, ParseTreeFormat(name));
        // Now the destination is refreshed to the second to last element
        destination = Path.GetDirectoryName(path)!;
        ArgumentException.ThrowIfNullOrEmpty(destination);
        // Even if user specified some extension in the name, it's discarded here.
        name = Path.GetFileNameWithoutExtension(path);
        ArgumentException.ThrowIfNullOrEmpty(name);

        DirectoryInfo destinationDir = new DirectoryInfo(destination);
        if (PathComparer.Instance.Equals(destinationDir, Sheet.SourceFile?.Directory))
        {
            throw new ArgumentException("Cannot copy cue file to the same directory.");
        }
        destinationDir.Create();
        Einzigartiger ez = new();
        //Skip the cuesheet itself
        ez.AddFiles(GetAllFiles().Skip(1));
        var tfs = ez.GetNumbered(name);
        List<FileInfo> copiedFiles = new();
        try
        {
            foreach (var tf in tfs)
            {
                copiedFiles.Add(tf.Copy(destinationDir));
            }
        }
        catch (Exception e)
        {
            foreach (var item in copiedFiles)
            {
                item.Delete();
            }
            Logger.LogError("Could not copy file to destination {Destination}: {Exception}", destinationDir, e);
            throw;
        }
        if (delete)
        {
            foreach (var item in tfs)
            {
                item.DeleteSource();
                Logger.LogVerbose("Deleted original file \"{Source}\"", item.SourceFile);
            }
        }
        List<(string source, string dest)> lista = new();
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
                File.Copy(copee.source, copee.dest, true);
                Logger.LogVerbose("Copied file from \"{Source}\" to \"{Destination}\"", copee.source, copee.dest);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Error while copying: {Error}", e);
            // remove any copied file
            foreach (var (_, dest) in lista)
            {
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                    Logger.LogVerbose("Deleted file {File}", dest);
                }
            }
            // if destination directory is empty, delete it
            if (!Directory.EnumerateFiles(destination).Any())
            {
                Directory.Delete(destination);
                Logger.LogVerbose("Deleted empty directory {Directory}", destination);
            }
        }
        var originalPath = Sheet.SourceFile;
        string cuePath = Path.ChangeExtension(Path.Combine(destination, name), "cue");
        Sheet.Save(cuePath);
        Logger.LogVerbose("Saved {CueSheet}", Sheet);
        // remove original files
        if (!delete)
            return Sheet;
        originalPath?.Delete();
        foreach (var (source, _) in lista)
        {
            File.Delete(source);
            Logger.LogVerbose("Deleted source file {Source}", source);
        }
        return Sheet;
    }

    [GeneratedRegex(@"%(?<property>\w+)%")]
    private static partial Regex PropertyParser();
}

//public struct NumberedPath
//{
//    public required string Path { get; set; }
//    public int Number { get; set; }
//    public string Extension => System.IO.Path.GetExtension(Path);
//    public string GetNumbered(int total)
//    {
//        var stem = System.IO.Path.GetFileNameWithoutExtension(Path);
//        int digits = (int)Math.Log10(total) + 1;
//        string numbering = Number.ToString($"d{digits}");
//        return $"{stem}_{numbering}{Extension}";
//    }
//    public string GetNonNumbered() => Path;
//}

public record class TransFile
{
    private string? newName;

    public FileInfo SourceFile { get; }
    public string Extension => SourceFile.Extension;
    public string NewName
    {
        get
        {
            if (newName == null)
                return Path.GetFileNameWithoutExtension(SourceFile.Name);
            return newName;
        }
        set => newName = value;
    }
    public void ResetName() => newName = null;
    public TransFile(FileInfo source)
    {
        SourceFile = source;
    }
    public FileInfo Copy(DirectoryInfo destination)
    {
        string dest = Path.Combine(destination.FullName, $"{NewName}{Extension}");
        return SourceFile.CopyTo(dest);
    }

    internal void DeleteSource() => SourceFile.Delete();
}
/// <summary>
/// Makes sure that every filename is unique
/// </summary>
public class Einzigartiger
{
    private SortedDictionary<string, List<TransFile>> ExtGroups { get; } = new();
    public void AddFiles(IEnumerable<FileInfo> files)
    {
        foreach (FileInfo file in files)
        {
            AddFile(file);
        }
    }
    public void AddFile(FileInfo file)
    {
        TransFile tf = new TransFile(file);
        if (ExtGroups.TryGetValue(tf.Extension, out List<TransFile>? groups))
        {
            groups.Add(tf);
        }
        else
        {
            ExtGroups[tf.Extension] = new List<TransFile>() { tf };
        }
    }
    private void Numberise(string newName)
    {
        foreach (var ext in ExtGroups.Keys)
        {
            bool isOne = ExtGroups[ext].Count == 1;
            if (isOne)
            {
                var t = ExtGroups[ext];
                ExtGroups[ext][0].NewName = newName;
            }
            else
            {
                int count = 1;
                int digits = (int)Math.Log10(ExtGroups[ext].Count) + 1;
                foreach (var tf in ExtGroups[ext])
                {
                    string countstr = count.ToString($"d{digits}");
                    tf.NewName = $"{newName}_{countstr}";
                    count++;
                }
            }
        }
    }
    private IEnumerable<TransFile> Enumerate()
    {
        foreach (var ext in ExtGroups)
        {
            foreach (var item in ext.Value)
            {
                yield return item;
            }
        }
    }
    public IEnumerable<TransFile> GetNumbered(string newName)
    {
        Numberise(newName);
        return Enumerate();
    }
}

public class Copier
{
    public Copier(CuePackage package, string namingPattern, DirectoryInfo destination)
    {
        Package = package;
        NamingPattern = namingPattern;
        Destination = destination;
        OriginalFiles = Package.GetAllFiles().ToImmutableArray();
    }

    public CuePackage Package { get; }
    public string NamingPattern { get; }
    public DirectoryInfo Destination { get; }
    public ImmutableArray<FileInfo> OriginalFiles { get; }

}