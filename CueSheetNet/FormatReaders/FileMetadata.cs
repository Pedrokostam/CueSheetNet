using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileReaders;

public readonly record struct FileMetadata
    (TimeSpan Duration,
    bool Binary,
     int SampleRate,
     int Channels,
     int BitDepth,
     string FormatName)
{
    public CueTime CueDuration => CueTime.FromTimeSpan(Duration);
}
