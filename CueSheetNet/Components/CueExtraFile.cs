namespace CueSheetNet;

public class CueExtraFile : ICueFile
{
    public FileInfo SourceFile { get; set; }
    public bool ValidFile => SourceFile.Exists;
    public CueExtraFile(FileInfo source)
    {
        SourceFile = source;
    }
    public CueExtraFile(string path) : this(new FileInfo(path))
    {
    }
    public static implicit operator FileInfo(CueExtraFile file) => file.SourceFile;
    public static explicit operator CueExtraFile(FileInfo file) => new CueExtraFile(file);

    public long FileSize => SourceFile.Length;
    public bool Exists => SourceFile.Exists;

}

