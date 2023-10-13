namespace CueSheetNet;

public interface ICueFile:IParentSheet
{
    FileInfo SourceFile { get; }
    long FileSize { get; }
    bool Exists { get; }
    string GetRelativePath();
}

