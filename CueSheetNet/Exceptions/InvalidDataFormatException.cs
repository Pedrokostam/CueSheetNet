namespace CueSheetNet.FileReaders;

/// <summary>
/// Thrown when format is corrupted or otherwise invalid for the format reader
/// </summary>
class InvalidDataFormatException : Exception
{
    public InvalidDataFormatException(string msg) : base(msg)
    { }
}
