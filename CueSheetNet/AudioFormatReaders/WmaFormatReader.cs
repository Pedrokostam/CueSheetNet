
using ATL;
using ATL.AudioData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.AudioFormatReaders;
internal class WmaFormatReader : IFileFormatReader
{
    private readonly string[] extensions = new string[] { ".WMA", ".ASF" };
    private readonly string formatName = "Windows Media Audio";
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
            var tr = new Track(stream, "audio/x-ms-wma");
            metadata = new FileMetadata(
                0,
                TimeSpan.FromMilliseconds(tr.DurationMs),
                (int)tr.SampleRate,
                tr.ChannelsArrangement.NbChannels,
                tr.BitDepth,
                tr.CodecFamily == AudioDataIOFactory.CF_LOSSY,
                FormatName
                ) ;
            return true;
        }
        catch (Exception)
        {
            metadata = default;
            return false;
        }
    }
}

