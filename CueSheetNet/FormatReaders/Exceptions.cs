
namespace CueSheetNet.FileReaders;

/// <summary>
/// Thrown when attempting to read a format, for which nor FormatReader is present
/// </summary>
class UnsupportedFileFormatException : Exception
{
    public UnsupportedFileFormatException(string msg) : base(msg)
    { }
}

/// <summary>
/// Thrown when format is corrupted or otherwise invalid for the format reader
/// </summary>
class InvalidFileFormatException : Exception
{
    public InvalidFileFormatException(string msg) : base(msg)
    { }
}
