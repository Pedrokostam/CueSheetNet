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
    [ReturnValueValidator()]
    public class Benchmark2
    {
        [Params(
            "tttt isadfjnojsadnfosdjfhnnsdsdf",
            "tttt i",
             "ttkfkfkfkfkfktt isadfjnojsadnfosdjfhnnsdsdf",
            "ttkfkfkfkfkfktt i"
            )]
        public string XXX { get; set; }
        [Benchmark(Baseline =true)]
        public string MATH() => GetKeywor2d(XXX);
        [Benchmark]
        public string MATHSpan() => GetKeywor3d(XXX);
        [Benchmark]
        public string MATHSpanStart() => GetKeywor4d(XXX);
        public string GetKeyword(string s, int startIndex = 0, int maxSearchLength = 20)
        {
            maxSearchLength = (maxSearchLength + startIndex) > s.Length ? s.Length : maxSearchLength;
            int charStart = 0;
            bool hadChars = false;
            for (int i = startIndex; i < maxSearchLength; i++)
            {
                if (char.IsWhiteSpace(s[i]) && hadChars)
                {
                    return s[charStart..(i+1)];
                }
                else
                {
                    if (!hadChars)
                    {
                        charStart = i + 1;
                    }
                    hadChars = true;
                }
            }
            return string.Empty;
        }

        public string GetKeywor2d(string s,int startIndex = 0, int maxSearchLength = 20)
        {
            maxSearchLength = Math.Clamp(startIndex + maxSearchLength, 0, s.Length);
            int charStart = 0;
            bool hadChars = false;
            for (int i = startIndex; i < maxSearchLength; i++)
            {
                if (char.IsWhiteSpace(s[i]) && hadChars)
                {
                    return s[charStart..i];
                }
                else
                {
                    if (!hadChars)
                    {
                        charStart = i ;
                    }
                    hadChars = true;
                }
            }
            return string.Empty;
        }
        public string GetKeywor3d(string s, int startIndex = 0, int maxSearchLength = 20)
        {
            maxSearchLength = Math.Clamp(startIndex + maxSearchLength, 0, s.Length);
            ReadOnlySpan<char> spanish = s.AsSpan(startIndex, maxSearchLength).Trim();
            for (int i = 0; i < spanish.Length; i++)
            {
                if (char.IsWhiteSpace(spanish[i]))
                    return spanish[..i].ToString();
            }
            return String.Empty;
        }
        public string GetKeywor4d(string s, int startIndex = 0, int maxSearchLength = 20)
        {
            maxSearchLength = Math.Clamp(startIndex + maxSearchLength, 0, s.Length);
            ReadOnlySpan<char> spanish = s.AsSpan(startIndex, maxSearchLength).TrimStart();
            for (int i = 0; i < spanish.Length; i++)
            {
                if (char.IsWhiteSpace(spanish[i]))
                    return spanish[..i].ToString();
            }
            return String.Empty;
        }

    }
}
