using BenchmarkDotNet.Attributes;
using CueSheetNet.TextParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
/*
 
‘ ’ 
 
“ ”

„ ” 
 
 
 */
namespace CueSheetNet.Benchmark
{
    [MemoryDiagnoser(false)]
    public class Benchmark2
    {

        public int X { get; set; } =132217;
        public DateTime Y { get; set; } = DateTime.Now;
        public string Z { get; set; } = "04mcvyure";
        [Benchmark(Baseline =true)]
        public string Format() => string.Format("{0}-{1}+{2}", X, Y, Z);
        [Benchmark]
        public string Interplated() => $"{X}-{Y}+{Z}";
        [Benchmark]
        public string InterplatedTstr() => $"{X.ToString()}-{Y.ToString()}+{Z.ToString()}";
    }
}
