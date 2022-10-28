using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CueSheetNet.Benchmark;

[MemoryDiagnoser(false)]
public class Benchmark
{
    public IEnumerable<object> Args()
    {
        yield return @"REM COMMENT  ""óóóóóóó""";
        yield return @"REM COMMENT  ""óóóóóóó"" WAVBE";
        yield return @"REM COMMENT  'óóóóóóó'";
        yield return @"REM COMMENT  'óóóóóóó' WAVBE";
        yield return @"REM COMMENT  “óóóóóóó”";
        yield return @"REM COMMENT  “óóóóóóó” WAVBE";
        yield return @"REM COMMENT  ‘óóóóóóó’";
        yield return @"REM COMMENT  ‘óóóóóóó’ WAVBE";
        yield return @"REM COMMENT  „óóóóóóó“";
        yield return @"REM COMMENT  „óóóóóóó“ WAVBE";
        yield return @"REM COMMENT  «óóóóóóó»";
        yield return @"REM COMMENT  «óóóóóóó» WAVBE";
        yield return @"REM COMMENT  ‹óóóóóóó›";
        yield return @"REM COMMENT  ‹óóóóóóó› WAVBE";
        yield return @"REM COMMENT óóóóóóó WAVBE";
        yield return @"REM COMMENT óóóóóóó";
        yield return @"REM COMMENT  ""óóóóóóóóóóóóóóóóóóóóóóóóóóóóó""";
        yield return @"REM COMMENT  ""óóóóóóóóóóóóóóóóóóóóóóóóóóóóó"" WAVBE";
        yield return @"REM COMMENT  'óóóóóóóóóóóóóóóóóóóóóóóóóóóóó'";
        yield return @"REM COMMENT  'óóóóóóóóóóóóóóóóóóóóóóóóóóóóó' WAVBE";
        yield return @"REM COMMENT  “óóóóóóóóóóóóóóóóóóóóóóóóóóóóó”";
        yield return @"REM COMMENT  “óóóóóóóóóóóóóóóóóóóóóóóóóóóóó” WAVBE";
        yield return @"REM COMMENT  ‘óóóóóóóóóóóóóóóóóóóóóóóóóóóóó’";
        yield return @"REM COMMENT  ‘óóóóóóóóóóóóóóóóóóóóóóóóóóóóó’ WAVBE";
        yield return @"REM COMMENT  „óóóóóóóóóóóóóóóóóóóóóóóóóóóóó“";
        yield return @"REM COMMENT  „óóóóóóóóóóóóóóóóóóóóóóóóóóóóó“ WAVBE";
        yield return @"REM COMMENT  «óóóóóóóóóóóóóóóóóóóóóóóóóóóóó»";
        yield return @"REM COMMENT  «óóóóóóóóóóóóóóóóóóóóóóóóóóóóó» WAVBE";
        yield return @"REM COMMENT  ‹óóóóóóóóóóóóóóóóóóóóóóóóóóóóó›";
        yield return @"REM COMMENT  ‹óóóóóóóóóóóóóóóóóóóóóóóóóóóóó› WAVBE";
        yield return @"REM COMMENT óóóóóóóóóóóóóóóóóóóóóóóóóóóóó WAVBE";
        yield return @"REM COMMENT óóóóóóóóóóóóóóóóóóóóóóóóóóóóó";
    }
    static private readonly char[] Openings = new char[]
      {
        '"',
        '\'',
        '“',
        '‘',
        '„',
        '«',
        '‹',
      };
    static private readonly char[] Endings = new char[]
      {
        '"',
        '\'',
        '“',
        '’',
        '“',
        '»',
        '›',
      };
    private static bool CompareOpening(char o)
    {
        for (int i = 0; i < Openings.Length; i++)
        {
            if (o == Openings[i]) return true;
        }
        return false;
    }
    private static int CompareOpeningI(char o)
    {
        for (int i = 0; i < Openings.Length; i++)
        {
            if (o == Openings[i]) return i;
        }
        return -1;
    }
    Regex QuotesSimple;
    Regex QuotesAdvanced;
    [GlobalSetup]
    public void Setup()
    {

        QuotesSimple = new(@"[""'“‘„«‹](.*)[""'”’“»›]", RegexOptions.Compiled);
        QuotesAdvanced = new(@"(""(.*)""|'(.*)'|“(.*)”|‘(.*)’|„(.*)“|«(.*)»)|‹(.*)›", RegexOptions.Compiled);

    }




    private static char GetMatchingQuotationMark(char q)
    {
        return q switch
        {
            '"' => '"',
            '\'' => '\'',
            '“' or '”' => '”',
            '„' => '“',
            '«' => '»',
            '‘' => '’',
            '‹' => '›',
            _ => q
        };
    }
    [Benchmark]
    [ArgumentsSource(nameof(Args))]
    public string StringIteratingImplSought(string s)
    {
        int length = s.Length;
        for (int j = 0; j < length; j++)
        {
            if (CompareOpening(s[j]))
            {
                char sought = GetMatchingQuotationMark(s[j]);
                for (int k = length - 1; k > j; k--)
                {
                    if (s[k] == sought)
                    {
                        return s.Substring(j + 1, k - j - 1);
                    }
                }
                return "";
            }
        }
        return "";
    }
    [Benchmark]
    [ArgumentsSource(nameof(Args))]
    public (int S, int E) StringIteratingImplSoughtT(string s)
    {
        int length = s.Length;
        for (int j = 0; j < length; j++)
        {
            if (CompareOpening(s[j]))
            {
                char sought = GetMatchingQuotationMark(s[j]);
                for (int k = length - 1; k > j; k--)
                {
                    if (s[k] == sought)
                    {
                        return (j + 1, k - 1);
                    }
                }
                return (0, s.Length - 1);
            }
        }
        return (0, s.Length - 1);
    }
    [Benchmark]
    [ArgumentsSource(nameof(Args))]
    public Range StringIteratingImplSoughtR(string s)
    {
        int length = s.Length;
        for (int j = 0; j < length; j++)
        {
            if (CompareOpening(s[j]))
            {
                char sought = GetMatchingQuotationMark(s[j]);
                for (int k = length - 1; k > j; k--)
                {
                    if (s[k] == sought)
                    {
                        return new(j + 1, k - j - 1);
                    }
                }
                return new(0, s.Length);
            }
        }
        return new(0, s.Length);
    }
    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Args))]
    public void RegexSimple(string s)
    {
        var res = QuotesSimple.Match(s).Groups[^1].Value;
    }
}
