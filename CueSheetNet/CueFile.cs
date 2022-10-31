using CueSheetNET.FileIO;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet;

public class CueFile : CueItemBase
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
}

