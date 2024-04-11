namespace CueSheetNet.FormatReaders;

public interface IFileFormatReader
{
    string[] Extensions { get; }
    string FormatName { get; }

    bool ExtensionMatches(string fileName);
}
