using ATL;
using ATL.AudioData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.AudioFormatReaders;
internal class OggFormatReader : IFileFormatReader
{
    private readonly string[] extensions = new string[] { ".OGG", ".OGX", ".SPX" };
    private readonly string formatName = "Ogg";
    //private readonly byte[] OggS = new byte[] { 0x4f, 0x67, 0x67, 0x53 };
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
            var tr = new Track(stream, "audio/ogg");
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
