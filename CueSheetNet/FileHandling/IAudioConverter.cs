using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace CueSheetNet.FileHandling;
public interface IAudioConverter
{
    void PreConvert();
    void Convert(string input, string output);
    void PostConvert();
}
