namespace CueSheetNet;

/// <summary>
/// Provides properties and instance methods for an extra file that was mentioned in a CUE sheet.
/// </summary>
public class CueExtraFile : FilePath
{
    public CueExtraFile(string filePath, CueSheet sheet) : this(filePath, sheet,sheet.UseQualifiedPaths)
    {
    }
    internal CueExtraFile(string filePath, CueSheet sheet, bool keepQualified) : base(filePath, sheet, keepQualified)
    {
    }
    internal CueExtraFile(CueExtraFile cueExtraFile, CueSheet sheet):this(cueExtraFile.Path,sheet, sheet.UseQualifiedPaths)
    {
        
    }
    public CueExtraFile Clone(CueSheet sheet)=>new CueExtraFile(this,sheet);
    public override string ToString()
    {
        return $"EXTRA \"{Path}\"";
    }
}
