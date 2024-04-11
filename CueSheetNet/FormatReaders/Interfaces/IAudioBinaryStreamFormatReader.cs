namespace CueSheetNet.FormatReaders;

public interface IAudioBinaryStreamFormatReader : IAudioFileFormatReader
{
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="stream">Stream with byte data. Method should not take ownership of the stream.</param>
    /// <returns>Duration in seconds</returns>
    bool ReadMetadata(Stream stream, out FileMetadata metadata);
}
