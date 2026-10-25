namespace CueSheetNet;

public interface ICueFile
{
    FileInfo SourceFile { get; }
    long FileSize { get; }
    bool Exists { get; }

    string GetRelativePath();
}

