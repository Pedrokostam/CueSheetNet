namespace CueSheetNet;

/// <summary>
/// Provides properties and instance methods for an extra file that was mentioned in a CUE sheet.
/// </summary>
public class CueExtraFile(FileInfo source, CueSheet parent) : ICueFile
{
    /// <inheritdoc cref="Internal.CueItemBase.ParentSheet"/>
    public CueSheet ParentSheet { get; } = parent;

    /// <inheritdoc cref="CueDataFile.SourceFile"/>
    public FileInfo SourceFile { get; set; } = source;

    public CueExtraFile(string path, CueSheet sheet)
        : this(new FileInfo(path), sheet) { }

    public static implicit operator FileInfo(CueExtraFile file) => file.SourceFile;

    /// <inheritdoc cref="CueDataFile.FileSize"/>
    public long FileSize => SourceFile.Length;

    /// <inheritdoc cref="CueDataFile.Exists"/>
    public bool Exists => SourceFile.Exists;

    /// <inheritdoc cref="CueDataFile.GetRelativePath"/>
    public string GetRelativePath() =>
        PathHelper.GetRelativePath(SourceFile, ParentSheet.SourceFile);
}
