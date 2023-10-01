using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileReaders;
internal sealed class Mp3FormatReader : FfprobeFormatReader
{
    private readonly string[] extensions = new string[] { ".MP3" };
    private readonly string formatName = "MP3";
    override public string FormatName => formatName;
    override public string[] Extensions => extensions;
    protected override bool Lossy => true;

}
