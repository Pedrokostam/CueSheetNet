using CueSheetNet;
using CueSheetNet.FileReaders;
using CueSheetNet.Logging;
using System.Text;


namespace CueSheetNet.FileReaders;
static public class FormatReader
{
    static FormatReader()
    {
        BaseAudioFileReaders = new IAudioFileFormatReader[]
        {
            new FlacFormatReader(),
            new WaveFormatReader(),
        };
        AudioFileReaders = new List<IAudioFileFormatReader>(BaseAudioFileReaders);
        CdReader = new();
        BaseFallbackReader = new FfprobeFormatReader();
        FallbackReader = BaseFallbackReader;
    }
    static readonly IAudioFileFormatReader[] BaseAudioFileReaders;

    private static readonly IAudioFileFormatReader BaseFallbackReader;

    public static IAudioFileFormatReader FallbackReader { get; set; }

    static readonly List<IAudioFileFormatReader> AudioFileReaders;
    static readonly CdFormatReader CdReader;
    public static void AddFileReader(IAudioFileFormatReader reader)
    {
        AudioFileReaders.Add(reader);
        Logger.Log(LogLevel.Debug, $"Added {reader.FormatName} file format reader for a total of {AudioFileReaders.Count} readers", nameof(FormatReader), "");
    }
    public static void ResetFileReader()
    {
        Logger.Log(LogLevel.Debug, $"Reset file readers list to base state", nameof(FormatReader), "");
        AudioFileReaders.Clear();
        AudioFileReaders.AddRange(BaseAudioFileReaders);
    }
    static public FileMetadata? ReadMetadata(string filePath, IEnumerable<TrackType> trackTypes)
    {
        FileMetadata meta = default;
        bool isStandardAudioFile = trackTypes.All(x => !x.CdSpecification);
        if (isStandardAudioFile)
        {
            foreach (var reader in AudioFileReaders.Append(FallbackReader))
            {
                if (reader.ExtensionMatches(filePath))
                {
                    try
                    {
                        bool read = false;
                        read = reader.ReadMetadata(filePath, out meta);
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
            if (CdReader.ReadMetadata(filePath, trackTypes, out meta))
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
