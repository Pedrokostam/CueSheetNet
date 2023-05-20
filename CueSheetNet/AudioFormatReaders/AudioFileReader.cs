using CueSheetNet;
using CueSheetNet.AudioFormatReaders;
using CueSheetNet.Logging;
using System.Text;


namespace CueSheetNet.AudioFormatReaders;
static public class AudioFileReader
{
    static AudioFileReader()
    {
        var flac = new FlacFormatReader();
        var wave = new WaveFormatReader();
        FileReaders.Add(flac);
        FileReaders.Add(wave);
    }
    static readonly List<IFileFormatReader> FileReaders = new();
    public static void AddFileReader(IFileFormatReader reader)
    {
        FileReaders.Add(reader);
        Logger.Log(LogLevel.Debug, $"Added {reader.FormatName} file format reader for a total of {FileReaders.Count} readers", nameof(AudioFileReader), "");
    }
    public static void ResetFileReader()
    {
        Logger.Log(LogLevel.Debug, $"Reset file readers list to base state", nameof(AudioFileReader), "");
        FileReaders.Clear();
    }
    static public FileMetadata? ParseDuration(string filePath)
    {
        using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
        FileMetadata meta = default;
        foreach (var reader in FileReaders)
        {
            if (reader.ExtensionMatches(filePath))
            {
                try
                {
                    if (reader.ReadMetadata(fs, out meta))
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
