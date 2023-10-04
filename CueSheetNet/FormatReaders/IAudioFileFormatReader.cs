using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileReaders;
public interface IFileFormatReader
{
    string[] Extensions { get; }
    string FormatName { get; }

    bool ExtensionMatches(string fileName);
}

public interface IAudioFileFormatReader: IFileFormatReader
{

    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    bool ReadMetadata(string path, out FileMetadata metadata);
}
public interface IAudioBinaryStreamFormatReader : IAudioFileFormatReader
{
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="stream">Stream with byte data. Method should not take ownership of the stream.</param>
    /// <returns>Duration in seconds</returns>
    bool ReadMetadata(Stream stream, out FileMetadata metadata);
}
public interface IBinaryFileFormatReader:IFileFormatReader
{
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    bool ReadMetadata(string path,IEnumerable<TrackType> trackTypes, out FileMetadata metadata);
}
public interface IBinaryStreamFormatReader : IBinaryFileFormatReader
{
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="stream">Stream with byte data. Method should not take ownership of the stream.</param>
    /// <returns>Duration in seconds</returns>
    bool ReadMetadata(Stream stream, IEnumerable<TrackType> trackTypes, out FileMetadata metadata);
}
