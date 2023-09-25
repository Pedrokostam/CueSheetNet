using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.AudioFormatReaders;

public readonly record struct FileMetadata
    (long Size, TimeSpan Duration, int SampleRate, int Channels, int BitDepth, bool Lossy, string FormatName)
{
    public CueTime CueDuration => CueTime.FromTimeSpan(Duration);
    public const int RedBookBitrate = 44100 * 2 * 16; // sample_rate * channels * bit_depth
    //Size of equivalent CD in bytes
    public long RedBookEquivalentSize
    {
        get
        {
            double rawAudioSize_bytes = Duration.TotalSeconds * RedBookBitrate / 8;// /8 to get bytes
            int waveOverhead = 44; //overhead of Wave Riff format
            long calculated = (long)Math.Round(rawAudioSize_bytes, MidpointRounding.AwayFromZero) + waveOverhead;
            //long diff = Math.Abs(calculated - Size);
            //if the difference between current size and calculated one is 1 bytes, it is probably a fractional error
            return calculated;
        }
    }
    public double RedBookCompressionRatio => Math.Round((double)Size / RedBookEquivalentSize, 3);


}
