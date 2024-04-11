namespace CueSheetNet;

public class CueExtraFile(FileInfo source, CueSheet parent) : ICueFile
{
    public CueSheet ParentSheet { get; } = parent;
    public FileInfo SourceFile { get; set; } = source;

    public CueExtraFile(string path, CueSheet sheet) : this(new FileInfo(path), sheet)
    {
    }
    public static implicit operator FileInfo(CueExtraFile file) => file.SourceFile;

    public long FileSize => SourceFile.Length;
    public bool Exists => SourceFile.Exists;

    public string GetRelativePath() => PathHelper.GetRelativePath(SourceFile, ParentSheet.SourceFile);
}

