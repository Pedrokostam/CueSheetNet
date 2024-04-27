using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CueSheetNet.Collections;
using CueSheetNet.FileHandling;
using CueSheetNet.FormatReaders;
using CueSheetNet.Internal;

namespace CueSheetNet;

/// <summary>
/// Provides properties and instance methods for a data file (including audio) that is specified in a cuesheet.
/// The class monitors the file to detect any changes to it.
/// </summary>
public class CueDataFile : CueItemBase, ICueFile, IEquatable<CueDataFile>
{
    /// <summary>
    /// File types, which contain audio data.
    /// </summary>
    public const FileType AudioTypes = FileType.WAVE | FileType.AIFF | FileType.MP3;

    /// <summary>
    /// File types which contain binary data.
    /// </summary>
    public const FileType DataTypes = FileType.BINARY | FileType.MOTOROLA;

    public FileTrackCollection Tracks { get; }

    /// <summary>
    /// Detects file type from the file's extension. Unknown extension are assumed to be <see cref="FileType.WAVE"/>.
    /// </summary>
    /// <param name="path">File path with extension.</param>
    /// <returns>Detected file type.</returns>
    public static FileType GetFileTypeFromPath(string path)
    {
        string extension = Path.GetExtension(path)[1..].ToLowerInvariant();
        return extension switch
        {
            "mp3" or "bit" => FileType.MP3,
            "aiff" or "aif" or "aifc" => FileType.AIFF,
            "wav" or "wave" => FileType.WAVE,
            "bin" or "mm2" or "iso" or "img" => FileType.BINARY,
            "mot" => FileType.MOTOROLA,
            _ => FileType.WAVE
        };
    }

    /// <summary>
    /// This file's index in the containing CUE sheet.
    /// </summary>
    public int Index { get; internal set; }

    public CueDataFile(CueSheet parent, string filePath, FileType type)
        : base(parent)
    {
        SetFile(filePath, type);
        Tracks = new(this);
    }

    internal CueDataFile ClonePartial(CueSheet newOwner)
    {
        return new(newOwner, SourceFile.FullName, Type);
    }

    /// <summary>
    /// Type of the file's data.
    /// </summary>
    public FileType Type { get; internal set; }

    private FileMetadata? meta;

    /// <summary>
    /// Metadata of this file.
    /// </summary>
    public FileMetadata? Meta
    {
        get
        {
            RefreshIfNeeded();
            return meta;
        }
        private set => meta = value;
    }

    /// <summary>
    /// Signals whether the info about the file needs to be resynced.
    /// </summary>
    private bool NeedsRefresh { get; set; }
    public long FileSize => SourceFile.Exists ? SourceFile.Length : -1;

    private void RefreshIfNeeded()
    {
        if (!NeedsRefresh || ParentSheet.Files.Count <= Index)
            return;
        Debug.WriteLine($"Refreshing file meta: {_sourceFile}");
        if (_sourceFile.Exists)
        {
            FileMetadata? resMeta = FormatReader.ReadMetadata(
                _sourceFile.FullName,
                ParentSheet.GetTracksOfFile_IEnum(Index).Select(x => x.Type)
            );
            Meta = resMeta;
        }
        else
        {
            Meta = null;
        }
        NeedsRefresh = false;
    }

    /// <summary>
    /// Checks whether the file is an audio file.
    /// </summary>
    /// <returns><see langword="true"/> if file's Type is one of the audio type, otherwise <see langword="false"/></returns>
    public bool IsAudio() => (AudioTypes & Type) != FileType.Unknown;

    private FileInfo _sourceFile;

    /// <summary>
    /// Source file in the filesystem.
    /// </summary>
    public FileInfo SourceFile => _sourceFile;

    /// <summary>
    /// Sets the <see cref="SourceFile"/> to a new path, optionally detecting its type automatically.
    /// </summary>
    /// <remarks>
    /// Also starts monitoring the new file, to detect changes made to it.
    /// </remarks>
    /// <param name="newPath">Path of the new file.</param>
    /// <param name="newType">Optional, file type. If set to <see langword="null"/>, detects the type from the extension.</param>
    [MemberNotNull(nameof(_sourceFile))]
    public void SetFile(string newPath, FileType? newType = null)
    {
        string absPath = Path.Combine(ParentSheet.SourceFile?.DirectoryName ?? ".", newPath);
        if (string.Equals(absPath, _sourceFile?.FullName, StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine($"Skipped setting to the same file {_sourceFile}");
            _sourceFile ??= new FileInfo(absPath);
            return;
        }
        Debug.WriteLine($"Setting file to {absPath}");
        CreateWatcher(absPath);
        _sourceFile = new FileInfo(absPath);
        Type = newType ?? Type;
        NeedsRefresh = true;
        RefreshIfNeeded();
    }

    public bool CheckPathEqual(string otherPath, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
    {
        // TODO sprawdź czy to na pewno git jest!!!
        string referencePath =Path.IsPathRooted(otherPath) ? SourceFile.FullName : GetRelativePath();
        char[] seps = [Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar  ];
        var parts = referencePath.Split(seps,StringSplitOptions.RemoveEmptyEntries);
        var otherParts = otherPath.Split(seps,StringSplitOptions.RemoveEmptyEntries);
        return parts.SequenceEqual(otherParts, StringHelper.GetComparer(stringComparison));
    }

    /// <summary>
    /// Gets all CUE indices of this file.
    /// </summary>
    public CueIndex[] CueIndexes => ParentSheet.GetIndexesOfFile(Index);

    public override string ToString()
    {
        return "File " + Index.ToString("D2") + " \"" + SourceFile.FullName + "\" " + Type;
    }

    /// <summary>
    /// Forces a refresh of file data.
    /// </summary>
    public void RefreshFileInfo()
    {
        string name = SourceFile.Name;
        string absPath = Path.Combine(ParentSheet.SourceFile?.DirectoryName ?? ".", name);
        SetFile(absPath, Type);
    }

    private string? normalizedPath;
    public string NormalizedPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                normalizedPath = PathComparer.NormalizePath(_sourceFile);
            }
            return normalizedPath!;
        }
    }

    /// <summary>
    /// Gets the path string of relative path, based on the directory that contains the parent CUE sheet.
    /// </summary>
    /// <returns>Relative path from the parent sheet's directory.</returns>
    public string GetRelativePath()
    {
        return PathHelper.GetRelativePath(NormalizedPath, ParentSheet.SourceFile);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The compared features are:
    /// <list type="bullet">
    /// <item>Relative paths.</item>
    /// <item><see cref="Type"/>.</item>
    /// <item><see cref="Index"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="other"></param>
    public bool Equals(CueDataFile? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;
        string thisRelativePath = GetRelativePath();
        string otherRelativePath = other.GetRelativePath();
        if (!string.Equals(thisRelativePath, otherRelativePath, StringComparison.OrdinalIgnoreCase))
            return false;
        if (Type != other.Type)
            return false;
        if (Index != other.Index)
            return false;
        return true;
    }

    /// <inheritdoc cref="CueDataFile.Equals(CueDataFile?)"/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as CueDataFile);
    }

    public static implicit operator FileInfo(CueDataFile file) => file.SourceFile;

    /// <summary>
    /// Calculates the hash of the file. Elements that affect the hash are <see cref="CueDataFile.NormalizedPath"/> and <see cref="CueDataFile.Index"/>.
    /// </summary>
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(NormalizedPath, Index);

    /// <inheritdoc cref="FileInfo.Exists"/>
    public bool Exists => _sourceFile.Exists;

    #region Watcher
    private FileSystemWatcher? watcher;

    private void CreateWatcher(string absPath)
    {
        string? parentDir = Path.GetDirectoryName(absPath);
        watcher?.Dispose();
        watcher = null;
        if (!Directory.Exists(parentDir))
        {
            return;
        }
        watcher = new FileSystemWatcher(parentDir, Path.GetFileName(absPath));
        //watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        watcher.Changed += Watcher_Changed;
        watcher.Deleted += Watcher_Deleted;
        watcher.Renamed += Watcher_Renamed;
        watcher.Created += Watcher_Created;
        watcher.Error += Watcher_Error;
        watcher.EnableRaisingEvents = true;
    }

    private void Watcher_Created(object sender, FileSystemEventArgs e)
    {
        //File was missing but appeared suddenly
        NeedsRefresh = true;
        Debug.WriteLine($"{e.Name} appeared");
    }

    private void Watcher_Error(object sender, ErrorEventArgs e)
    {
        NeedsRefresh = true;
        Debug.WriteLine($"Errored {e.GetException()}");
    }

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        NeedsRefresh = true;
        Debug.WriteLine($"{e.Name} changed");
    }

    private void Watcher_Renamed(object sender, RenamedEventArgs e)
    {
        //If new name is different, treat it as deletion
        if (!string.Equals(e.Name, SourceFile.Name, StringComparison.OrdinalIgnoreCase))
        {
            NeedsRefresh = true;
            Debug.WriteLine($"{e.OldName} renamed to {e.Name}");
        }
        else
        {
            NeedsRefresh = true;
            Debug.WriteLine($"{e.OldName} renamed back to {e.Name}");
        }
    }

    private void Watcher_Deleted(object sender, FileSystemEventArgs e)
    {
        NeedsRefresh = true;
        Meta = null;
        Debug.WriteLine($"{e.Name} deleted");
    }
    #endregion
}
