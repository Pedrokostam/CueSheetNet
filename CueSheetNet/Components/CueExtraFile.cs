namespace CueSheetNet;

public class CueExtraFile : ICueFile
{
    public CueSheet ParentSheet { get; }
    public FileInfo SourceFile { get; set; }
    public CueExtraFile(FileInfo source, CueSheet parent)
    {
        ParentSheet = parent;
        SourceFile = source;
    }
    public CueExtraFile(string path, CueSheet sheet) : this(new FileInfo(path), sheet)
    {
    }
    public static implicit operator FileInfo(CueExtraFile file) => file.SourceFile;

    public long FileSize => SourceFile.Length;
    public bool Exists => SourceFile.Exists;

    public string GetRelativePath() => PathHelper.GetRelativePath(SourceFile, ParentSheet.SourceFile?.DirectoryName);
}

