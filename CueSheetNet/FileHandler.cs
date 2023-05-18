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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CueSheetNet;

/// <summary>
/// Class which takes care of file operations related to the CueSheet passed to the constructor.
/// </summary>
public static partial class FileHandler
{
    private static readonly Dictionary<string, string> CommonSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        {"ARTIST","Performer" },
        {"ALBUMARTIST","Performer" },
        {"ALBUM ARTIST","Performer" },
        {"YEAR","Date" },
        {"ALBUM","Title" },
        {"ALBUMTITLE","Title" },
        {"ALBUM TITLE","Title" },
        {"ALBUMNAME","Title" },
        {"ALBUM NAME","Title" },
        {"DISC ID","DiscID" },
        {"CURRENT","old" },
    };
    /// <summary>
    /// Finds all files related to the CueSheet
    /// </summary>
    public static IEnumerable<FileInfo> GetAssociatedFiles(CueSheet sheet)
    {
        //If there are no files or directory of last file is null, return - no files
        if (sheet.LastFile is null || sheet.LastFile?.FileInfo?.DirectoryName is null)
        {
            return Enumerable.Empty<FileInfo>();
        }
        // All variations of base name of cuesheet
        HashSet<string> matchStrings = GetMatchStringHashset(sheet);

        //Where the sheet is located
        DirectoryInfo? sheetDir = sheet.SourceFile?.Directory;
        //Where the last audio file is located
        DirectoryInfo? audioDir = sheet.LastFile.FileInfo.Directory;
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
            if (file.Extension.Equals(".cue", StringComparison.OrdinalIgnoreCase)) continue;
            string name = Path.GetFileNameWithoutExtension(file.Name);
            if (matchStrings.Contains(name))
            {
                //var ttyu = Sheet.Files.Select(x => x.FileInfo).ToArray();
                //bool zzz=Comparer.Equals(ttyu[0], file);
                bool isAudioFile = sheet.Files.Select(x => x.FileInfo).Contains(file, PathComparer.Instance);
                if (isAudioFile)
                    continue; //Audio files are already associated with the sheet
                compareNames.Add(file);
            }
        }
        return compareNames.Cast<FileSystemInfo>().Order(PathComparer.Instance).Cast<FileInfo>();//.Select((file, index) => new IndexedFile(FileType.Additional, index, (FileInfo)file)).ToArray();
    }
    /// <summary>
    /// Creates a mover for a clone of the <paramref name="sheet"/>.
    /// The original sheet will not be modified
    /// </summary>
    /// <param name="sheet"></param>
    /// <summary>
    /// Gets collection of unique string matches for file searching
    /// </summary>
    /// <returns>Hashset with unique string matches, both normalized and raw. Hashset uses <see cref="StringComparer.InvariantCultureIgnoreCase"/></returns>
    private static HashSet<string> GetMatchStringHashset(CueSheet sheet)
    {
        string baseName = GetBaseNameForSearching(sheet);
        string noSpaceName = baseName.Replace(" ", "");
        string underscoreName = baseName.Replace(' ', '_');
        HashSet<string> hs = new(StringComparer.InvariantCultureIgnoreCase)
        {
            baseName,
            StringNormalization.NormalizeString( baseName),
            noSpaceName,
            StringNormalization.NormalizeString(noSpaceName),
            underscoreName,
            StringNormalization.NormalizeString(underscoreName)
        };
        return hs;
    }
    /// <summary>
    /// Gets the filename of source sheet file, or the filename of last audio file, or "{Performer} - {Title}". All without extension
    /// </summary>
    /// <returns></returns>
    private static string GetBaseNameForSearching(CueSheet Sheet)
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
    /// <param name="sheet"></param>
    /// <returns>Path stem made accoridng to the treeformat. All invalid path chars removed.</returns>
    /// <param name="treeFormat">Pattern which may contain reference to properies of CueSheet, case insensitive names. To get the original filename, specify %old%
    /// If parameter is null, the original filename will be used
    /// </param>
    private static string ParseTreeFormat(CueSheet sheet, string? treeFormat)
    {
        if (treeFormat == null)
            return Path.GetFileNameWithoutExtension(sheet.SourceFile!.Name);
        Regex formatter = PropertyParser();
        MatchCollection matches = formatter.Matches(treeFormat);
        foreach (Match match in matches.Cast<Match>())
        {
            string val = match.Value;
            string groupVal = match.Groups["property"].Value;
            if (CommonSynonyms.TryGetValue(groupVal, out string? syn))
            {
                groupVal = syn;
            }
            if (groupVal.Equals("old", StringComparison.InvariantCultureIgnoreCase))
            {
                // special tag "old" - reuse current name of the file
                treeFormat = treeFormat.Replace(val, Path.GetFileNameWithoutExtension(sheet.SourceFile!.Name));
                continue;
            }
            // Flag for case ignoring is set
            var flags = System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
            System.Reflection.PropertyInfo? prop = sheet.GetType().GetProperty(groupVal, flags);
            object? value = prop?.GetValue(sheet);
            if (value is null)
            {
                // Invalid tags will be left without changing, still in percent signs
                Logger.LogWarning("No matching property found for {Property} when parsing tree format {Format}.", val, treeFormat);
                continue;
            }
            treeFormat = treeFormat.Replace(val, value.ToString());
        }
        //replace all invalid patrh chars with underscore
        return StringNormalization.RemoveInvalidPathCharacters(treeFormat);
    }
    //public CueSheet MoveFiles(string destination, string? name = null) => CopyFiles(true, destination, name);
    //public CueSheet CopyFiles(string destination, string? name = null) => CopyFiles(false, destination, name);


    /// <summary>
    /// Gets parent directory path of <paramref name="destinationWithPattern"/>, creates the directory on the disk and return its <see cref="DirectoryInfo"/>
    /// </summary>
    /// <param name="destinationWithPattern">Multi-part filepath, where the last part will be removed</param>
    /// <returns><see cref="DirectoryInfo"/> for the parent folder of <paramref name="destinationWithPattern"/></returns>
    /// <exception cref="IOException">If the directory cannot be created</exception>
    private static DirectoryInfo GetImmediateParentDir(string destinationWithPattern)
    {
        string destination = GetNotNullDestination(destinationWithPattern);
        DirectoryInfo immediateParentDir = new(destination);
        immediateParentDir.Create(); //If we can't create the directory, IOException happens and we stop without needing to reverse anything.
        return immediateParentDir;
    }
    private static void SaveModifiedCueSheet(CueSheet sheet, string filename, DirectoryInfo immediateParentDir)
    {
        string sheetPath = Path.Join(immediateParentDir.FullName, filename);
        sheetPath = Path.ChangeExtension(sheetPath, "cue");
        sheet.Save(sheetPath); //If we can't save the sheet there, IOException happens and we stop without needing to reverse anything.
        Logger.LogInformation("Saved {CueSHeet} to {Destination}", sheet, sheetPath);
    }

    /// <summary>
    /// For a given directory check if files matching any of the input transfiles NewName exists. If such a file exists, throw IOException
    /// </summary>
    /// <param name="immediateParentDir"></param>
    /// <param name="transFiles">Their NewNames will be checked in <paramref name="immediateParentDir"/></param>
    /// <exception cref="IOException">If there is a name collision with any of the <paramref name="transFiles"/></exception>
    private static void CheckForNameCollisions(DirectoryInfo immediateParentDir, List<TransFile> transFiles)
    {
        foreach (TransFile item in transFiles)
        {
            if (item.CheckNewNameExists(immediateParentDir))
            {

                throw new IOException($"File with name {item.NewName}{item.Extension} already exists in {immediateParentDir}");
            }
        }
    }
    /// <summary>
    /// Find all audio files of the CueSheet and all additional files. Create new names for them to prevent name collision and returns a list with TransFiles
    /// </summary>
    /// <param name="sheet"></param>
    /// <returns></returns>
    /// <param name="filename">Base anem given all related files, without any invalid characters</param>
    private static List<TransFile> GetTransFiles(CueSheet sheet, string filename)
    {
        List<TransFile> transFiles = new();
        IEnumerable<TransFile> transAudios = GetAudioTransFiles(sheet,filename);
        int fileIndex = 0;
        foreach (TransFile transAudio in transAudios)
        {
            sheet.ChangeFile(fileIndex, transAudio.NewNameWithExtension);
            transFiles.Add(transAudio);
            fileIndex++;
        }
        IEnumerable<TransFile> trandAdditionals = GetAdditionalTransFiles(sheet.AssociatedFiles, filename);
        transFiles.AddRange(trandAdditionals);
        return transFiles;
    }

    private static IEnumerable<TransFile> GetAdditionalTransFiles(IEnumerable<FileInfo> files, string baseFilename)
    {
        SortedDictionary<string, List<TransFile>> ExtGroups = new(StringComparer.OrdinalIgnoreCase);
        foreach (FileInfo file in files)
        {
            TransFile transFile = new(file);
            if (ExtGroups.TryGetValue(file.Extension, out List<TransFile>? extFiles))
            {
                extFiles.Add(transFile);
            }
            else
            {
                ExtGroups[file.Extension] = new List<TransFile> { transFile };
            }
        }
        foreach (var ext in ExtGroups.Keys)
        {
            bool isOne = ExtGroups[ext].Count == 1;
            if (isOne)
            {
                ExtGroups[ext][0].NewName = baseFilename;
            }
            else
            {
                int count = 1;
                int digits = GetNumberOfDigits(ExtGroups[ext].Count);
                foreach (var tf in ExtGroups[ext])
                {
                    string countstr = count.ToString($"d{digits}");
                    tf.NewName = $"{baseFilename} {GetPaddedNumber(count, digits)}";
                    count++;
                }
            }
        }
        IEnumerable<TransFile> additionals = from key in ExtGroups.Keys
                                             orderby key.ToUpperInvariant()
                                             from list in ExtGroups[key]
                                             select list;
        return additionals;
    }

    private static int GetNumberOfDigits(int maxCount) => (int)Math.Log10(maxCount) + 1;
    private static string GetPaddedNumber(int number, int digitCount)
    {
        return number.ToString($"d{digitCount}");
    }

    private static IEnumerable<TransFile> GetAudioTransFiles(CueSheet sheet,string filename)
    {
        // One audio file, no need to change the filename
        if (sheet.Files.Count == 0) { yield break; }
        if (sheet.Files.Count == 1)
        {
            TransFile transFile = new TransFile(sheet.Files[0].FileInfo) { NewName = filename };
            yield return transFile;
        }
        else
        {
            int numOfDigits = GetNumberOfDigits(sheet.Files.Count);
            foreach (CueFile cueFile in sheet.Files)
            {
                string audiofilename = $"{filename} - {GetPaddedNumber(cueFile.Index, numOfDigits)}";// add index if file to filename
                var trackOfFile = sheet.GetTracksOfFile(cueFile.Index);
                if (trackOfFile.Length == 1)// If the file has one song, add the song title to filename
                {
                    audiofilename += $" - {trackOfFile[0].Title}";
                }
                TransFile currAudio = new(cueFile.FileInfo) { NewName = audiofilename };
                yield return currAudio;
            }
        }
    }

    private static string GetNotNullName(string path)
    {
        string? name = Path.GetFileNameWithoutExtension(path);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return name;
    }

    private static string GetNotNullDestination(string path)
    {
        string destination = Path.GetDirectoryName(path)!;
        ArgumentException.ThrowIfNullOrEmpty(destination);
        return destination;
    }

    [GeneratedRegex(@"%(?<property>[\w\s]+)%")]
    private static partial Regex PropertyParser();
    //private static  Regex PropertyParser()=> throw new NotImplementedException();
    public static  void DeleteCueFiles(CueSheet sheet)
    {
        var audioFiles = sheet.Files.Select(x => x.FileInfo);
        var allFiles = audioFiles.Concat(sheet.AssociatedFiles).Append(sheet.SourceFile);
        HashSet<string> h = new(StringComparer.InvariantCulture);
        foreach (var file in allFiles)
        {
            if (!(file?.Exists ?? false))
            {
                continue;
            }
            if (file.DirectoryName is not null)
            {
                h.Add(file.DirectoryName);
            }
            file.Delete();
            Logger.LogInformation("Removed file {File}", file);
        }
    }
    /// <summary>
    /// Finds all files associated with the sheet (including those not mention in the sheet proper, but sharing its name) and copies them to the new location. Modifies a clone of the CueSheet and returns it.
    /// </summary>
    /// <param name="destination">Path to destination folder</param>
    /// <param name="pattern">Pattern to be added to <paramref name="destination"/>. Can contain tags (surrounded by '%') which will be expanded bases on the CueSheet.
    ///     <para>E.G. %title% will be replaced by sheet's title. Speical tag %old'% is replaced by the current filename of the sheet.</para>
    ///     <para>Any extension in the pattern will be ignored and replaced by ".cue"</para>
    ///     <para>Pattern can be a directory structure (slashes are allowed), so this pattern is permitted: %artist%/%year%/%title%/%old%.cue</para>
    /// </param>
    /// <returns>Newly copied cuesheet</returns>
    public static CueSheet CopyCueFiles(CueSheet sheet,string destination, string? pattern = null)
    {
        sheet=sheet.Clone();
        // Combine Destination with whatever results from parsing (which may contain more directories)
        string destinationWithPattern = Path.Combine(destination, ParseTreeFormat(sheet, pattern));
        //If we can't create the directory, IOException happens and we stop without needing to reverse anything.
        DirectoryInfo immediateParentDir = GetImmediateParentDir(destinationWithPattern);
        // Now the destination is refreshed to the second to last element
        // Even if user specified some extension in the name, it's discarded here.
        string filename = GetNotNullName(destinationWithPattern);
        filename = StringNormalization.RemoveInvalidNameCharacters(filename);

        List<TransFile> transFiles = GetTransFiles(sheet, filename);

        CheckForNameCollisions(immediateParentDir, transFiles);

        SaveModifiedCueSheet(sheet, filename, immediateParentDir);
        // At this point we saved a sheet referencing file that do not exist yet

        List<FileInfo> inProgressCopied = new List<FileInfo>();
        try
        {
            foreach (var item in transFiles)
            {
                var copied = item.Copy(immediateParentDir);
                inProgressCopied.Add(copied);
            }
            // All associated file copied
            // Cuesheet already saved
            // We're done here
        }
        catch (Exception)
        {
            // One of the file failed to copy
            // Removing all already copied files and throwing
            foreach (var item in inProgressCopied)
            {
                if (!item.Exists)
                    continue;
                item.Delete();
                Logger.LogWarning("Removed copied file {File}", item);
            }
            throw;
        }
        // Sheet of this cuepackage is already updated, no need to change anything
        return sheet;
    }
    /// <summary>
    /// Finds all files associated with the sheet (including those not mentioned in the sheet proper, but sharing its name) and moves them to the new location. Modifies a clone of the CueSheet and returns it.
    /// </summary>
    /// <returns>Newly moved sheet</returns>
    /// <inheritdoc cref="CopyCueFiles(CueSheet, string, string?)"/>
    public static CueSheet MoveCueFiles(CueSheet sheet,string destination, string? pattern = null)
    {
        sheet=sheet.Clone();
        // Combine Destination with whatever results from parsing (which may contain more directories)
        string destinationWithPattern = Path.Combine(destination, ParseTreeFormat(sheet, pattern));
        //If we can't create the directory, IOException happens and we stop without needing to reverse anything.
        DirectoryInfo immediateParentDir = GetImmediateParentDir(destinationWithPattern);
        // Now the destination is refreshed to the second to last element
        // Even if user specified some extension in the name, it's discarded here.
        string filename = GetNotNullName(destinationWithPattern);
        filename = StringNormalization.RemoveInvalidNameCharacters(filename);

        List<TransFile> transFiles = GetTransFiles(sheet, filename);

        CheckForNameCollisions(immediateParentDir, transFiles);

        SaveModifiedCueSheet(sheet, filename, immediateParentDir);
        // At this point we saved a sheet referencing file that do not exist yetZ

        List<TransFile> movedFileArchive = new();
        try
        {
            foreach (var item in transFiles)
            {
                // Before moveing copy the contents of file into memory
                movedFileArchive.Add(new(item.SourceFile));
                var copied = item.Move(immediateParentDir);
            }
            // All associated file moved
            // Cuesheet already saved
            // We're done here
        }
        catch (Exception)
        {
            // One of the file failed to copy
            // Restoring all already moved files from memory and throwing
            foreach (var item in transFiles)
            {
                item.Restore();
            }
            throw;
        }
        // Sheet of this cuepackage is already updated, no need to change anything
        return sheet;
    }
}
public record class TransFile
{
    private string? newName;

    public byte[]? Backup { get; private set; }
    public FileInfo SourceFile { get; }

    public string Extension => SourceFile.Extension;
    /// <summary>
    /// NewName of the file. No extension. If set to null, the old name will be used
    /// </summary>
    [AllowNull]
    public string NewName
    {
        get
        {
            if (newName == null)
                return Path.GetFileNameWithoutExtension(SourceFile.Name);
            return newName;
        }
        set
        {
            newName = value;
        }
    }
    public string NewNameWithExtension
    {
        get
        {
            if (newName == null)
                return Path.GetFileName(SourceFile.Name);
            return newName + Extension;
        }
    }
    public TransFile(FileInfo source)
    {
        SourceFile = source;
    }
    public FileInfo Copy(DirectoryInfo destination)
    {
        string dest = Path.Combine(destination.FullName, NewNameWithExtension);
        FileInfo res = SourceFile.CopyTo(dest);
        Logger.LogInformation("Copied file {File} from {Source}", res, SourceFile);
        return res;
    }
    public FileInfo Move(DirectoryInfo destination)
    {
        Backup = File.ReadAllBytes(SourceFile.FullName);
        string dest = Path.Combine(destination.FullName, NewNameWithExtension);
        SourceFile.MoveTo(dest);
        FileInfo res = new(dest);
        Logger.LogInformation("Moved file {File} from {Source}", res, SourceFile);
        return res;
    }
    //public FileInfo Rename()
    //{
    //    if (newName == Path.GetFileNameWithoutExtension(SourceFile.Name))
    //    {
    //        return SourceFile;
    //    }
    //    string dest = Path.Combine(SourceFile.DirectoryName!, $"{NewName}{Extension}");
    //    SourceFile.MoveTo(dest);
    //    Logger.LogInformation("Moved file {File} from {Source}", res, SourceFile);
    //    return SourceFile;
    //}


    /// <summary>
    /// Check if the file with NewName exists in a given folder
    /// </summary>
    /// <param name="directory"></param>
    /// <returns>True if such file exists, False if not</returns>
    public bool CheckNewNameExists(DirectoryInfo directory)
    {
        string checker = Path.Join(directory.FullName, NewNameWithExtension);
        return Path.Exists(checker);
    }
    internal void DeleteSource()
    {
        SourceFile.Delete();
        Logger.LogInformation("Deleted file {File}", SourceFile);
    }

    public bool Restore()
    {
        if (Backup is null) return false;
        if (SourceFile.Exists) return false;
        File.WriteAllBytes(SourceFile.FullName, Backup);
        Logger.LogInformation("Restored moved file {File}", SourceFile);
        return true;
    }
}