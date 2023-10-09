using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace CueSheetNet.FileHandling;
public interface IAudioConverter
{
    string PreConvert(string format);
    void Convert(string input, string output);
    void PostConvert();
}
