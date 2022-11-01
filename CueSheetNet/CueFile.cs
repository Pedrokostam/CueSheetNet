using CueSheetNET.FileIO;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet;

public class CueFile : CueItemBase, IEquatable<CueFile>
{
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
        set => _Type = value.ToUpperInvariant();
    }
    public CueTime? Duration { get; private set; }
    public bool ValidFile { get; private set; }
    private FileInfo _file;

    public FileInfo File => _file;
    [MemberNotNull(nameof(_file))]
    public void SetFile(string value)
    {
        string absPath = Path.Combine(ParentSheet.FileInfo?.DirectoryName ?? ".", value);
        _file = new FileInfo(absPath);
        if (_file?.Exists ?? false)
        {
            try
            {
                Duration = AudioFileReader.ParseDuration(_file.FullName);
                ValidFile = true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        else
        {
            Duration = null;
            ValidFile = false;
        }
    }
    public CueIndex[] Indexes => ParentSheet.GetIndexesOfFile(Index);
    public override string ToString()
    {
        return "File " + Index.ToString("D2") + " \"" + File.FullName + "\" " + Type;
    }

    public void RefreshFileInfo()
    {
        string name = File.Name;
        string absPath = Path.Combine(ParentSheet.FileInfo?.DirectoryName ?? ".", name);
        SetFile(absPath);
    }

    public bool Equals(CueFile? other) => Equals(other, StringComparison.CurrentCulture);
    public bool Equals(CueFile? other, StringComparison stringComparison)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other == null) return false;
        if (
               !string.Equals(File.Name, File.Name, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Type, other.Type)
            || Index != other.Index
           )
            return false;
        return true;
    }
    
}

