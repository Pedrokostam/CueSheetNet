namespace CueSheetNet.FormatReaders;

public interface IBinaryFileFormatReader : IFileFormatReader
{
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    bool ReadMetadata(string path, IEnumerable<TrackType> trackTypes, out FileMetadata metadata);
}
