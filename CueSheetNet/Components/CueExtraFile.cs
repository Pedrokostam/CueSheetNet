using CueSheetNet.Components;

namespace CueSheetNet;

/// <summary>
/// Provides properties and instance methods for an extra file that was mentioned in a CUE sheet.
/// </summary>
public class CueExtraFile(Sheet sheet, string filePath, bool keepQualified = false) : FilePath(filePath, sheet,keepQualified)
{
}
