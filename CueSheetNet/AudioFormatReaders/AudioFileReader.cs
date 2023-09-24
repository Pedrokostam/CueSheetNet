using CueSheetNet;
using CueSheetNet.AudioFormatReaders;
using CueSheetNet.Logging;
using System.Text;


namespace CueSheetNet.AudioFormatReaders;
static public class AudioFileReader
{
    static AudioFileReader()
    {
        BaseFileReaders = new IFileFormatReader[]
        {
            new FlacFormatReader(),
            new WaveFormatReader(),
            new Mp3FormatReader(),
            new OggFormatReader(),
            new WmaFormatReader()
        };
        FileReaders = new List<IFileFormatReader>(BaseFileReaders);
    }
    static readonly IFileFormatReader[] BaseFileReaders;

    static readonly List<IFileFormatReader> FileReaders;
    public static void AddFileReader(IFileFormatReader reader)
    {
        FileReaders.Add(reader);
        Logger.Log(LogLevel.Debug, $"Added {reader.FormatName} file format reader for a total of {FileReaders.Count} readers", nameof(AudioFileReader), "");
    }
    public static void ResetFileReader()
    {
        Logger.Log(LogLevel.Debug, $"Reset file readers list to base state", nameof(AudioFileReader), "");
        FileReaders.Clear();
        FileReaders.AddRange(BaseFileReaders);
    }
    static public FileMetadata? ReadMetadata(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        FileMetadata meta = default;
        foreach (var reader in FileReaders)
        {
            if (reader.ExtensionMatches(filePath))
            {
                try
                {
                    bool read = false;
                    if (reader is IStreamFormatReader streamFormatReader)
                    {
                        read = streamFormatReader.ReadMetadata(stream, out meta);
                    }
                    else
                    {
                        read = reader.ReadMetadata(filePath, out meta);
                    }
                    if (read)
                    {
                        Logger.Log(LogLevel.Debug, "Read metadata on {File} using {Reader.FormatName} reader", filePath, reader);
                        return meta;
                    }
                }
                catch (Exception x) when (x is FileFormatRecognitionException
                                          || x is InvalidFileFormatException)
                {
                    Logger.Log(LogLevel.Warning, "Could not read audio file metadata of \"{File}\". Error: {Error}", filePath, x);

                }

            }
        }
        Logger.Log(LogLevel.Warning, "Could not read audio file metadata of \"{File}\" - no matching exception", filePath);
        return null;
    }

}
