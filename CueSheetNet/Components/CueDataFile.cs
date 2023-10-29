using CueSheetNet.FileHandling;
using CueSheetNet.FileReaders;
using CueSheetNet.Internal;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet;


public class CueDataFile : CueItemBase, ICueFile, IEquatable<CueDataFile>
{
    public const FileType AudioTypes = FileType.WAVE | FileType.AIFF | FileType.MP3;
    public const FileType DataTypes = FileType.BINARY | FileType.MOTOROLA;
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
    public int Index { get; internal set; }
    public CueDataFile(CueSheet parent, string filePath, FileType type) : base(parent)
    {
        SetFile(filePath, type);
    }
    internal CueDataFile ClonePartial(CueSheet newOwner)
    {
        return new(newOwner, SourceFile.FullName, Type);
    }
    public FileType Type { get; internal set; }
    private FileMetadata? meta;
    public FileMetadata? Meta
    {
        get
        {
            RefreshIfNeeded();
            return meta;
        }
        private set => meta = value;
    }

    private bool NeedsRefresh { get; set; }
    public long FileSize => SourceFile.Exists ? SourceFile.Length : -1;

    private void RefreshIfNeeded()
    {
        if (!NeedsRefresh || ParentSheet.Files.Count <= Index) return;
        Debug.WriteLine($"Refreshing file meta: {_file}");
        if (_file.Exists)
        {
            FileMetadata? resMeta = FormatReader.ReadMetadata(_file.FullName,
                                           ParentSheet
                                           .GetTracksOfFile_IEnum(Index)
                                           .Select(x => x.Type));
            Meta = resMeta;
        }
        else
        {
            Meta = null;
        }
        NeedsRefresh = false;
    }

    public bool IsAudio() => (AudioTypes & Type) != FileType.Unknown;

    private FileInfo _file;
    public FileInfo SourceFile => _file;


    [MemberNotNull(nameof(_file))]
    public void SetFile(string value, FileType? newType = null)
    {
        string absPath = Path.Combine(ParentSheet.SourceFile?.DirectoryName ?? ".", value);
        if (string.Equals(absPath, _file?.FullName, StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine($"Skipped setting to the same file {_file}");
            _file??=new FileInfo(absPath);
            return;
        }
        Debug.WriteLine($"Setting file to {absPath}");
        CreateWatcher(absPath);
        _file = new FileInfo(absPath);
        Type = newType ?? Type;
        NeedsRefresh = true;
        RefreshIfNeeded();
    }

    public CueIndex[] CueIndexes => ParentSheet.GetIndexesOfFile(Index);

    public override string ToString()
    {
        return "File " + Index.ToString("D2") + " \"" + SourceFile.FullName + "\" " + Type;
    }
    public void RefreshFileInfo()
    {
        string name = SourceFile.Name;
        string absPath = Path.Combine(ParentSheet.SourceFile?.DirectoryName ?? ".", name);
        SetFile(absPath);
    }

    private string? normalizedPath;
    public string NormalizedPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                normalizedPath = PathComparer.NormalizePath(_file);
            }
            return normalizedPath;
        }
    }

    public bool Equals(CueDataFile? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        if (!string.Equals(GetRelativePath(), other.GetRelativePath(), StringComparison.OrdinalIgnoreCase)) return false;
        if (Type != other.Type) return false;
        if (Index != other.Index) return false;
        return true;
    }

    public string GetRelativePath()
    {
        string cueBase = ParentSheet.SourceFile?.DirectoryName ?? ".";
        return Path.GetRelativePath(cueBase, NormalizedPath);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CueDataFile);
    }
    static public implicit operator FileInfo(CueDataFile file) => file.SourceFile;
    public override int GetHashCode() => HashCode.Combine(NormalizedPath, Index);
    public bool Exists => _file.Exists;

    #region Watcher
    private FileSystemWatcher? watcher;
    private void CreateWatcher(string absPath)
    {
        string? parentDir = Path.GetDirectoryName(absPath);
        watcher?.Dispose();
        watcher = null;
        if (!Path.Exists(parentDir))
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

