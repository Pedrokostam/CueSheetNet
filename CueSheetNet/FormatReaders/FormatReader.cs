

using Microsoft.Extensions.Logging;

namespace CueSheetNet.FormatReaders;

public static class FormatReader
{
    static ILogger Loggerio => Loggingq.AudioReadingLogger;
    static FormatReader()
    {
        BaseAudioFileReaders = [new FlacFormatReader(), new WaveFormatReader(),];
        AudioFileReaders = [.. BaseAudioFileReaders];
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
        Loggerio.AudioReaderAdded(reader.FormatName);
    }

    public static void ResetFileReader()
    {
        AudioFileReaders.Clear();
        AudioFileReaders.AddRange(BaseAudioFileReaders);
        Loggerio.AudioReadersReset();
    }

    public static FileMetadata? ReadMetadata(string filePath, IEnumerable<TrackType> trackTypes)
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
                            Loggerio.AudioReadingSuccess(filePath, reader.FormatName);
                            return meta;
                        }
                    }
                    catch (Exception x)
                    {
                        Loggerio.AudioReadingFail(filePath, reader.FormatName, x);
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

            return null;
        }
        return null;
    }
}
