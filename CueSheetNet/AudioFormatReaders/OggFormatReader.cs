using CueSheetNET.FileIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileIO;
internal class OggFormatReader : IFileFormatReader
{
    private readonly string[] extensions = new string[] { ".OGG", ".OGX", ".SPX" };
    private readonly string formatName = "Ogg";
    private readonly byte[] OggS = new byte[] { 0x4f, 0x67, 0x67, 0x53 };
    private readonly byte[] WAVE = new byte[] { 0x57, 0x41, 0x56, 0x45 };
    public string FormatName => formatName;
    public string[] Extensions => extensions;
    public bool ExtensionMatches(string fileName)
    {
        string ext = Path.GetExtension(fileName);
        return extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    public bool ReadMetadata(Stream stream, out FileMetadata metadata) => throw new NotImplementedException();
}
