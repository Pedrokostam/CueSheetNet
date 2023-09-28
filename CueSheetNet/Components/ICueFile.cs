namespace CueSheetNet;

public interface ICueFile
{
    FileInfo SourceFile { get; }
    bool ValidFile { get; }
    long FileSize { get; }
    bool Exists { get; }


}

