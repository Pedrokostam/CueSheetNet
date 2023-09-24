
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.AudioFormatReaders;

internal sealed class WmaFormatReader : FfprobeFormatReader
{
    private readonly string[] extensions = new string[] { ".WMA", ".ASF" };
    private readonly string formatName = "Windows Media Audio";
    override public string FormatName => formatName;
    override public string[] Extensions => extensions;
    protected override bool Lossy =>true;
}


