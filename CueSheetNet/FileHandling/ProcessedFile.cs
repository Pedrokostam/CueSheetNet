namespace CueSheetNet.FileHandling;

public record class ProcessedFile(string OriginalPath, string ProcessedPath, FileType Type)
{
}
