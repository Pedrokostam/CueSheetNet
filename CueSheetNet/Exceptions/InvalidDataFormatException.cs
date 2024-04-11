namespace CueSheetNet.FormatReaders;

/// <summary>
/// Thrown when format is corrupted or otherwise invalid for the format reader
/// </summary>
class InvalidDataFormatException(string msg) : Exception(msg)
{
}
