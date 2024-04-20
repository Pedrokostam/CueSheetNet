namespace CueSheetNet.FormatReaders;

/// <summary>
/// Thrown when attempting to read a format, for which nor FormatReader is present
/// </summary>
class UnsupportedDataFormatException(string msg) : Exception(msg)
{ }
