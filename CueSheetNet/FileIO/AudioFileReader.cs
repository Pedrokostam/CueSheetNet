using CueSheetNet;
using CueSheetNet.FileIO;
using CueSheetNet.Logging;
using System.Text;

namespace CueSheetNET.FileIO
{
    static public class AudioFileReader
    {
        static AudioFileReader()
        {
            Logger.Log(LogLevel.Debug, "Static constructor called", nameof(AudioFileReader), "");
            var flac = new FlacFormatDetector();
            var wave = new WaveFormatDetector();
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
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            FileMetadata meta = default;
            foreach (var reader in FileReaders)
            {
                if (reader.ExtensionMatches(filePath))
                {
                    try
                    {
                        if (reader.ReadMetadata(fs, out meta))
                        {
                            Logger.Log(LogLevel.Debug, "Detected encoding", filePath, reader.FormatName);
                            return meta;
                        }
                    }
                    catch (FileFormatRecognitionException ffre)
                    {
                        Logger.Log(LogLevel.Error, ffre.Message, filePath, reader.FormatName);
                    }
                    catch (InvalidFileFormatException iffe)
                    {
                        Logger.Log(LogLevel.Error, iffe.Message, filePath, reader.FormatName);
                    }
                }
            }
            Logger.Log(LogLevel.Warning, "Could not read audio file metadata", filePath, "");
            return null;
        }

    }
}
