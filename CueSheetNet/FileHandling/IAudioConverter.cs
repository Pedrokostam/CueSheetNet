﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileHandling;
internal interface IAudioConverter
{
    void Convert(string input, string output);
    string OutputFormat { get; }
}
