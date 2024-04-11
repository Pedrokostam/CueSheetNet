namespace CueSheetNet.FormatReaders;

public interface IBinaryStreamFormatReader : IBinaryFileFormatReader
{
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="stream">Stream with byte data. Method should not take ownership of the stream.</param>
    /// <returns>Duration in seconds</returns>
    bool ReadMetadata(Stream stream, IEnumerable<TrackType> trackTypes, out FileMetadata metadata);
}
