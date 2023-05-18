using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileIO;
internal class Mp3FormatReader : IFileFormatReader
{
    private readonly string formatName="MP3";
    private readonly string[] extensions = new string[] {".MP3" };

    public string FormatName => formatName;

    public string[] Extensions => throw new NotImplementedException();

    public bool ExtensionMatches(string fileName) => throw new NotImplementedException();
    public bool ReadMetadata(Stream stream, out FileMetadata metadata) => throw new NotImplementedException();
}
