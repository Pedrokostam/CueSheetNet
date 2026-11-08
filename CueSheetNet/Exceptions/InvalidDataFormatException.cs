using CueSheetNet.Exceptions;

namespace CueSheetNet.FormatReaders;

/// <summary>
/// Thrown when format is corrupted or otherwise invalid for the format reader
/// </summary>
class InvalidDataFormatException(string message) : CueException(message)
{ }
