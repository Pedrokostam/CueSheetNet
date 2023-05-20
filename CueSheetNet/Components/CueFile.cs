using CueSheetNet.AudioFormatReaders;
using CueSheetNet.FileHandling;
using CueSheetNet.Internal;
using CueSheetNet.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CueSheetNet;

public class CueFile : CueItemBase, IEquatable<CueFile>
{
    private FileSystemWatcher? watcher;
    public const int CdSamplingRate = 44_100;
    public const int CdNumberOfChannels = 2;
    public const int CdBitrate = CdSamplingRate * CdNumberOfChannels * 16;
    public int Index { get; internal set; }
    public CueFile(CueSheet parent, string filePath, string type) : base(parent)
    {
        SetFile(filePath);
        Type = type;
    }
    internal CueFile ClonePartial(CueSheet newOwner)
    {
        return new(newOwner, FileInfo.FullName, Type);
    }
    private string _Type;
    public string Type
    {
        get => _Type;
        [MemberNotNull(nameof(_Type))]
        set => _Type = value.ToUpperInvariant().Trim();
    }
    public FileMetadata? Meta
    {
        get
        {
            RefreshIfNeeded();
            return meta;
        }
        private set => meta = value;
    }
    public bool ValidFile
    {
        get
        {
            RefreshIfNeeded();
            return validFile;
        }
        private set => validFile = value;
    }
    private bool NeedsRefresh { get; set; }

    private FileInfo _file;
    private bool validFile;
    private FileMetadata? meta;
    private string? normalizedPath;

    private void RefreshIfNeeded()
    {
        if (!NeedsRefresh) return;
        Debug.WriteLine($"Refreshing file meta: {_file}");
        if (_file.Exists)
        {
            Meta = AudioFileReader.ParseDuration(_file.FullName);
            ValidFile = true;
        }
        else
        {
            Meta = null;
            ValidFile = false;
        }
        NeedsRefresh = false;
    }
    public FileInfo FileInfo => _file;
    public long FileSize => FileInfo.Exists ? FileInfo.Length : -1;


    [MemberNotNull(nameof(_file))]
    public void SetFile(string value)
    {
        string absPath = Path.Combine(ParentSheet.SourceFile?.DirectoryName ?? ".", value);
        if (absPath == _file?.FullName)
        {
            Debug.WriteLine($"Skipped setting to the same file {_file}");

            return;
        }
        Debug.WriteLine($"Setting file to {absPath}");
        CreateWatcher(absPath);
        _file = new FileInfo(absPath);
        NeedsRefresh = true;
        RefreshIfNeeded();

    }

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
        if (e.Name != FileInfo.Name)
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
        ValidFile = false;
        NeedsRefresh = true;
        Meta = null;
        Debug.WriteLine($"{e.Name} deleted");
    }

    public CueIndex[] CueIndexes => ParentSheet.GetIndexesOfFile(Index);

    public override string ToString()
    {
        return "File " + Index.ToString("D2") + " \"" + FileInfo.FullName + "\" " + Type;
    }
    public void RefreshFileInfo()
    {
        string name = FileInfo.Name;
        string absPath = Path.Combine(ParentSheet.SourceFile?.DirectoryName ?? ".", name);
        SetFile(absPath);
    }

    public bool Equals(CueFile? other) => Equals(other, StringComparison.CurrentCulture);

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

    public bool Equals(CueFile? other, StringComparison stringComparison)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other == null) return false;
        if (NormalizedPath != other.NormalizedPath) return false;
        if (Type != other.Type) return false;
        if (Index != other.Index) return false;
        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CueFile);
    }

    public override int GetHashCode() => HashCode.Combine(NormalizedPath, Index);
}

