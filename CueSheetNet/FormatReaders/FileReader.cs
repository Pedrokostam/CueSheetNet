using CueSheetNet;
using CueSheetNet.FileReaders;
using CueSheetNet.Logging;
using System.Text;


namespace CueSheetNet.FileReaders;
static public class FileReader
{
    static FileReader()
    {
        BaseAudioFileReaders = new IAudioFileFormatReader[]
        {
            new FlacFormatReader(),
            new WaveFormatReader(),
            new Mp3FormatReader(),
            new OggFormatReader(),
            new WmaFormatReader()
        };
        AudioFileReaders = new List<IAudioFileFormatReader>(BaseAudioFileReaders);
        CdReader = new();
    }
    static readonly IAudioFileFormatReader[] BaseAudioFileReaders;

    static readonly List<IAudioFileFormatReader> AudioFileReaders;
    static readonly CdFormatReader CdReader;
    public static void AddFileReader(IAudioFileFormatReader reader)
    {
        AudioFileReaders.Add(reader);
        Logger.Log(LogLevel.Debug, $"Added {reader.FormatName} file format reader for a total of {AudioFileReaders.Count} readers", nameof(FileReader), "");
    }
    public static void ResetFileReader()
    {
        Logger.Log(LogLevel.Debug, $"Reset file readers list to base state", nameof(FileReader), "");
        AudioFileReaders.Clear();
        AudioFileReaders.AddRange(BaseAudioFileReaders);
    }
    static public FileMetadata? ReadMetadata(string filePath, IEnumerable<TrackType> trackTypes)
    {
        using FileStream stream = File.OpenRead(filePath);
        FileMetadata meta = default;
        bool isStandardAudioFile = trackTypes.All(x => !x.CdSpecification);
        if (isStandardAudioFile)
        {
            foreach (var reader in AudioFileReaders)
            {
                if (reader.ExtensionMatches(filePath))
                {
                    try
                    {
                        bool read = false;
                        if (reader is IAudioBinaryStreamFormatReader streamFormatReader)
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
                    catch (Exception x) when (x is InvalidFileFormatException)
                    {
                        Logger.Log(LogLevel.Warning, "Could not read audio file metadata of \"{File}\". Error: {Error}", filePath, x);
                    }

                }
            }
        }
        else
        {
            if (!CdReader.ExtensionMatches(filePath))
            {
                return null;
            }
            if (CdReader.ReadMetadata(stream, trackTypes, out meta))
            {
                return meta;
            }
            else
            {
                return null;
            }
        }
        return null;
    }
}
