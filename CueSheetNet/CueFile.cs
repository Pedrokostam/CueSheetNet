using CueSheetNet.FileIO;
using CueSheetNET.FileIO;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet;

public class CueFile : CueItemBase, IEquatable<CueFile>
{
    public const int CdSamplingRate = 44_100;
    public const int CdNumberOfChannels = 2;
    public const int CdBitrate = CdSamplingRate * CdNumberOfChannels * 16;
    public int Index { get; internal set; }
    public CueFile(CueSheet parent, string filePath, string type) : base(parent)
    {
        SetFile(filePath);
        Type = type;
    }
    private string _Type;
    public string Type
    {
        get => _Type;
        [MemberNotNull(nameof(_Type))]
        set => _Type = value.ToUpperInvariant().Trim();
    }
    public FileMetadata? Meta { get; private set; }
    public bool ValidFile { get; private set; }

    private FileInfo _file;

    public FileInfo FileInfo => _file;
    public long FileSize => FileInfo.Exists ? FileInfo.Length : -1;



    [MemberNotNull(nameof(_file))]
    public void SetFile(string value)
    {
        string absPath = Path.Combine(ParentSheet.FileInfo?.DirectoryName ?? ".", value);
        _file = new FileInfo(absPath);
        if (_file?.Exists ?? false)
        {
            Meta = AudioFileReader.ParseDuration(_file.FullName);
            ValidFile = true;
        }
        else
        {
            Meta = null;
            ValidFile = false;
        }
    }
    public CueIndex[] Indexes => ParentSheet.GetIndexesOfFile(Index);

    public override string ToString()
    {
        return "File " + Index.ToString("D2") + " \"" + FileInfo.FullName + "\" " + Type;
    }

    public void RefreshFileInfo()
    {
        string name = FileInfo.Name;
        string absPath = Path.Combine(ParentSheet.FileInfo?.DirectoryName ?? ".", name);
        SetFile(absPath);
    }

    public bool Equals(CueFile? other) => Equals(other, StringComparison.CurrentCulture);

    public bool Equals(CueFile? other, StringComparison stringComparison)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other == null) return false;
        if (
               !string.Equals(FileInfo.Name, FileInfo.Name, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Type, other.Type)
            || Index != other.Index
           )
            return false;
        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CueFile);
    }
}

