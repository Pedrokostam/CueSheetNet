namespace CueSheetNet;

public interface ICueFile
{
    FileInfo SourceFile { get; }
    /// <summary>
    /// Files metadata can be accesed
    /// </summary>
    bool ValidFile { get; }
    long FileSize { get; }
    bool Exists { get; }


}

