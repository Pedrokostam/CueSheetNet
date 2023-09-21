using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using ATL.AudioData;

namespace CueSheetNet.AudioFormatReaders;
internal class Mp3FormatReader : IFileFormatReader
{
    private readonly string formatName = "MP3";
    private readonly string[] extensions = new string[] { ".MP3" };

    public string FormatName => formatName;

    public string[] Extensions => extensions;

    public bool ExtensionMatches(string fileName)
    {
        string ext = Path.GetExtension(fileName);
        return extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
    public bool ReadMetadata(Stream stream, out FileMetadata metadata)
    {

        try
        {
            var tr = new Track(stream, "audio/mp3");
            metadata = new FileMetadata(
                 stream.Length,
                TimeSpan.FromMilliseconds(tr.DurationMs),
                (int)tr.SampleRate,
                tr.ChannelsArrangement.NbChannels,
                tr.BitDepth,
                    tr.CodecFamily == AudioDataIOFactory.CF_LOSSY,
                FormatName
                );
            return true;
        }
        catch (Exception)
        {
            metadata = default;
            return false;
        }
    }
}
