using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.AudioFormatReaders;
internal sealed class OggFormatReader : FfprobeFormatReader
{
    private readonly string[] extensions = new string[] { ".OGG", ".OGX", ".SPX" };
    private readonly string formatName = "Ogg";
    //private readonly byte[] OggS = new byte[] { 0x4f, 0x67, 0x67, 0x53 };
    override public string FormatName => formatName;
    override public string[] Extensions => extensions;
    protected override bool Lossy => true;
}
