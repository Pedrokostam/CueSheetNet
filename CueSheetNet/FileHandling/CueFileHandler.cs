﻿using CueSheetNet.Logging;
using CueSheetNet.NameParsing;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;

namespace CueSheetNet.FileHandling;

/// <summary>
/// Class which takes care of file operations related to the CueSheet passed to the constructor.
/// </summary>
public static partial class CuePackage
{
    /// <summary>
    /// Finds all files related to the CueSheet
    /// </summary>
    public static IEnumerable<ICueFile> GetAssociatedFiles(CueSheet sheet)
    {
        //If there are no files or directory of last file is null, return - no files
        if (sheet.LastFile is null || sheet.LastFile?.SourceFile?.DirectoryName is null)
        {
            return Enumerable.Empty<ICueFile>();
        }
        // All variations of base name of cuesheet
        HashSet<string> matchStrings = GetMatchStringHashset(sheet);

        //Where the sheet is located
        DirectoryInfo? sheetDir = sheet.SourceFile?.Directory;
        //Where the last audio file is located
        DirectoryInfo? audioDir = sheet.LastFile.SourceFile.Directory;
        IEnumerable<FileInfo> siblingFiles = audioDir?.EnumerateFiles() ?? Enumerable.Empty<FileInfo>();

        // If audio dir and sheet dir are different, concatenate file sequences
        if (!PathComparer.Instance.Equals(sheetDir, audioDir)
            && sheetDir?.EnumerateFiles() is IEnumerable<FileSystemInfo> audioSiblings)
        {
            siblingFiles = siblingFiles.Concat(sheetDir.EnumerateFiles());
        }

        // Case-insensitive hashset to get only unique filepaths. Should work with case-sensitive filesystems,
        // since what is added is directly enumerated file.
        HashSet<CueExtraFile> compareNames = new(PathComparer.Instance);
        foreach (FileInfo file in siblingFiles)
        {
            if (file.Extension.Equals(".cue", StringComparison.OrdinalIgnoreCase)) continue;
            string name = Path.GetFileNameWithoutExtension(file.Name);
            if (matchStrings.Contains(name))
            {
                //var ttyu = Sheet.Files.Select(x => x.FileInfo).ToArray();
                //bool zzz=Comparer.Equals(ttyu[0], file);
                bool isAudioFile = sheet.Files.Select(x => x.SourceFile).Contains(file, PathComparer.Instance);
                if (isAudioFile)
                    continue; //Audio files are already associated with the sheet
                compareNames.Add(new(file));
            }
        }
        return compareNames.Order(PathComparer.Instance);//.Select((file, index) => new IndexedFile(FileType.Additional, index, (FileInfo)file)).ToArray();
    }

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
            PathStringNormalization.NormalizeString(baseName),
            noSpaceName,
            PathStringNormalization.NormalizeString(noSpaceName),
            underscoreName,
            PathStringNormalization.NormalizeString(underscoreName)
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
                name = Path.GetFileNameWithoutExtension(Sheet.LastFile.SourceFile.Name);
            else
                name = $"{Sheet.Performer} - {Sheet.Title}";
        }
        else
        {
            name = Path.GetFileNameWithoutExtension(Sheet.SourceFile.Name);
        }
        return name;
    }

    [GeneratedRegex(@"%(?<property>[\w\s]+)%")]
    private static partial Regex PropertyParser();


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

    private static void SaveModifiedCueSheet(CueSheet sheet, string filename, DirectoryInfo immediateParentDir, CueWriter writer)
    {
        string sheetPath = Path.Join(immediateParentDir.FullName, filename);
        sheetPath = Path.ChangeExtension(sheetPath, "cue");
        writer.SaveCueSheet(sheet, sheetPath);//If we can't save the sheet there, IOException happens and we stop without needing to reverse anything.
        Logger.LogInformation("Saved {CueSheet} to {Destination}", sheet, sheetPath);
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
    private static List<TransFile> GetTransFiles(CueSheet sheet, string filename, bool preserveSubfolders)
    {
        DirectoryInfo? cueFolder = preserveSubfolders ? sheet.SourceFile?.Directory : null;
        List<TransFile> transFiles = new();
        IEnumerable<TransFile> transAudios = GetAudioTransFiles(sheet, filename, cueFolder);
        int fileIndex = 0;
        foreach (TransFile transAudio in transAudios)
        {
            sheet.ChangeFile(fileIndex, transAudio.NewNameWithExtension);
            transFiles.Add(transAudio);
            fileIndex++;
        }
        IEnumerable<TransFile> trandAdditionals = GetAdditionalTransFiles(sheet.AssociatedFiles, filename, cueFolder);
        transFiles.AddRange(trandAdditionals);
        return transFiles;
    }

    private static IEnumerable<TransFile> GetAdditionalTransFiles(IEnumerable<ICueFile> files, string baseFilename, DirectoryInfo? relativeBase)
    {
        SortedDictionary<string, List<TransFile>> ExtGroups = new(StringComparer.OrdinalIgnoreCase);
        foreach (ICueFile file in files)
        {
            TransFile transFile = new(file, relativeBase);
            if (ExtGroups.TryGetValue(file.SourceFile.Extension, out List<TransFile>? extFiles))
            {
                extFiles.Add(transFile);

            }
            else
            {
                ExtGroups[file.SourceFile.Extension] = new List<TransFile> { transFile };
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

    /// <summary>
    /// Gets the number of digits (in base 10) needed to represent the number
    /// </summary>
    /// <param name="maxCount"></param>
    /// <returns>How many digits the number takes in base 10</returns>
    private static int GetNumberOfDigits(int maxCount) => (int)Math.Log10(maxCount) + 1;

    /// <summary>
    /// Converts number to string with the specified width
    /// </summary>
    /// <param name="number"></param>
    /// <param name="digitCount"></param>
    /// <returns></returns>
    private static string GetPaddedNumber(int number, int digitCount)
    {
        return number.ToString().PadRight(digitCount, '0');
        // apparently fewer JIT instruction than creating dynamic formatting string -- x.ToString($"d{w}")
    }

    private static IEnumerable<TransFile> GetAudioTransFiles(CueSheet sheet, string filename, DirectoryInfo? relativeBase)
    {
        // One audio file, no need to change the filename
        if (sheet.Files.Count == 0) { yield break; }
        if (sheet.Files.Count == 1)
        {
            TransFile transFile = new(sheet.Files[0], relativeBase) { NewName = filename };
            yield return transFile;
        }
        else
        {
            int numOfDigits = GetNumberOfDigits(sheet.Files.Count);
            foreach (CueAudioFile cueFile in sheet.Files)
            {
                string audiofilename = $"{filename} - {GetPaddedNumber(cueFile.Index, numOfDigits)}";// add index if file to filename
                var trackOfFile = sheet.GetTracksOfFile(cueFile.Index);
                if (trackOfFile.Length == 1)// If the file has one song, add the song title to filename
                {
                    audiofilename += $" - {trackOfFile[0].Title}";
                }
                TransFile currAudio = new(cueFile, relativeBase) { NewName = audiofilename };
                yield return currAudio;
            }
        }
    }

    /// <summary>
    /// Returns the filename from the path, without the last extension. If the string is null, throws an exception.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Non-null filename</returns>
    private static string GetNotNullName(string? path)
    {
        string? name = Path.GetFileNameWithoutExtension(path);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return name;
    }

    /// <summary>
    /// Returns the directory name of the parent of the path. If the string is null, throws an exception.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Non-null directory name</returns>
    private static string GetNotNullDestination(string? path)
    {
        string destination = Path.GetDirectoryName(path)!;
        ArgumentException.ThrowIfNullOrEmpty(destination);
        return destination;
    }


    public static void RemovePackage(CueSheet sheet)
    {
        IEnumerable<ICueFile> audioFiles = sheet.Files.Select(x => x);
        var allFiles = audioFiles.Concat(sheet.AssociatedFiles).Select(x => x.SourceFile);
        if (sheet.SourceFile is FileInfo main)
        {
            allFiles.Append(main);
        };
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
        sheet.SourceFile?.Delete();
    }

    /// <summary>
    /// Finds all files associated with the sheet (including those not mention in the sheet proper, but sharing its name) and copies them to the new location. Modifies a clone of the CueSheet and returns it.
    /// <para/>The final path is created by using <see cref="Path.Combine"/> on both the parameters
    /// </summary>
    /// <remarks>If the pattern is rooted (e.g. C:\Music\%artist%\%current%) any previous parts will be ignored</remarks>
    /// <param name="activeSheet">CueSheet to processed</param>
    /// <param name="destinationDirectory">Path to destinationDirectory folder</param>
    /// <param name="pattern">Pattern to be added to <paramref name="destinationDirectory"/>. Can contain tags (surrounded by '%') which will be expanded bases on the CueSheet.
    ///     <para>E.G. %title% will be replaced by sheet's title. Speical tag %old'% is replaced by the current filename of the sheet.</para>
    ///     <para>Any extension in the pattern will be ignored and replaced by ".cue"</para>
    ///     <para>Pattern can be a directory structure (slashes are allowed), so this pattern is permitted: %artist%/%year%/%title%/%old%.cue</para>
    /// </param>
    /// <returns>Newly copied cuesheet</returns>
    public static CueSheet CopyPackage(CueSheet activeSheet,
                                       string destinationDirectory,
                                       string? pattern = null,
                                       CueWriterSettings? settings = null,
                                       bool preserveSubfolders = false)
    {
        activeSheet = activeSheet.Clone();
        SaveModifiedCueSheetInNewLocation(destinationDirectory,
                                          pattern,
                                          settings,
                                          activeSheet,
                                          out DirectoryInfo immediateParentDir,
                                          out List<TransFile> transFiles,
                                          preserveSubfolders);

        List<FileInfo> inProgressCopied = new();
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
        return activeSheet;
    }

    /// <summary>
    /// Finds all files associated with the sheet (including those not mentioned in the sheet proper, but sharing its name) and moves them to the new location. Modifies a clone of the CueSheet and returns it.
    /// </summary>
    /// <returns>Newly moved sheet</returns>
    /// <inheritdoc cref="CopyCueFiles(CueSheet, string, string?)"/>
    public static CueSheet MovePackage(CueSheet sheet,
                                       string destinationDirectory,
                                       string? pattern = null,
                                       CueWriterSettings? settings = null,
                                       bool preserveSubfolders = false)
    {
        CueSheet activeSheet = sheet.Clone();
        SaveModifiedCueSheetInNewLocation(destinationDirectory,
                                          pattern,
                                          settings,
                                          activeSheet,
                                          out DirectoryInfo immediateParentDir,
                                          out List<TransFile> transFiles,
                                          preserveSubfolders);

        List<MovedFile> movedFileArchive = new();
        try
        {
            foreach (var item in transFiles)
            {
                // Before moveing copy the contents of file into memory
                var moved = item.Move(immediateParentDir);
                movedFileArchive.Add(moved);
            }
            // All associated file moved
            // Cuesheet already saved
            // We're done here
        }
        catch (Exception)
        {
            // One of the file failed to copy
            // Restoring all already moved files from memory and throwing
            foreach (var item in movedFileArchive)
            {
                item.Undo();
            }
            throw;
        }
        if (sheet.SourceFile is FileInfo x)
        {
            x.Delete();
            Logger.LogInformation("Deleted original file of {File}", x);
        }
        // Sheet of this cuepackage is already updated, no need to change anything
        return activeSheet;
    }

    private static void SaveModifiedCueSheetInNewLocation(string destinationDirectory,
                                                          string? pattern,
                                                          CueWriterSettings? settings,
                                                          CueSheet activeSheet,
                                                          out DirectoryInfo immediateParentDir,
                                                          out List<TransFile> transFiles,
                                                          bool preserveSubfolders)
    {
        // Combine Destination with whatever results from parsing (which may contain more directories)
        string patternParsed = CueTreeFormatter.ParseFormatPattern(activeSheet, pattern);
        string destinationWithPattern = Path.Combine(destinationDirectory, patternParsed);
        //If we can't create the directory, IOException happens and we stop without needing to reverse anything.
        immediateParentDir = GetImmediateParentDir(destinationWithPattern);
        // Now the destinationDirectory is refreshed to the second to last element
        // Even if user specified some extension in the name, it's discarded here.
        string filename = GetNotNullName(destinationWithPattern);
        filename = PathStringNormalization.RemoveInvalidNameCharacters(filename);
        transFiles = GetTransFiles(activeSheet, filename, preserveSubfolders);
        CheckForNameCollisions(immediateParentDir, transFiles);
        CueWriter writer = new(settings ?? new());
        SaveModifiedCueSheet(activeSheet, filename, immediateParentDir, writer);
        // At this point we saved a sheet referencing file that do not exist yet
    }

    public static CueSheet Convert(CueSheet cueSheet, IAudioConverter converter, string destination, string? pattern = null)
    {
        throw new NotImplementedException();
        //converter.Convert();
    }
}
