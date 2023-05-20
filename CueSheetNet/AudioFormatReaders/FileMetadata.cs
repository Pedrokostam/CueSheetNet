using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.AudioFormatReaders;

public readonly record struct FileMetadata
    (long Size, TimeSpan Duration, int SampleRate, int Channels, int BitDepth, bool Lossy, string FormatName)
{
    public CueTime CueDuration =>CueTime.FromTimeSpan(Duration);
    public long RedBookEquivalentSize
    {
        get
        {
            double rawAudioSize = Duration.TotalSeconds
                        * 44100 //cd sample rate in Hz
                        * 2 //cd number of channels
                        * 2; //cd bit depth in bytes
            int waveOverhead = 44; //overhead of Wave Riff format
            long calculated = (long)Math.Round(rawAudioSize,MidpointRounding.AwayFromZero) + waveOverhead;
            //long diff = Math.Abs(calculated - Size);
            //if the difference between current size and calculated one is 1 bytes, it is probably a fractional error
            return calculated;
        }
    }
    public double RedBookCompressionRatio => Math.Round((double)Size / RedBookEquivalentSize,3);


}
