namespace CueSheetNet;

/// <summary>
/// Provides properties and instance methods for an extra file that was mentioned in a CUE sheet.
/// </summary>
public class CueExtraFile : FilePath
{
    public CueExtraFile(string filePath, CueSheet sheet) : this(filePath, sheet,false)
    {
    }
    internal CueExtraFile(string filePath, CueSheet sheet, bool keepQualified) : base(filePath, sheet, keepQualified)
    {
    }
}
