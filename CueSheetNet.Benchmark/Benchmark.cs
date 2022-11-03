using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace CueSheetNet.Benchmark;

[MemoryDiagnoser(false)]
[SimpleJob()]
public class Benchmark
{
    public IEnumerable<string[]> Args()
    {
        //yield return ArgsNoWord().ToArray();
        //yield return ArgsNoEnd().ToArray();
        //yield return ArgsQuotesWord().ToArray();
        yield return ArgsQuotesEnd().ToArray();
    }

    public IEnumerable<string> ArgsNoWord()
    {
        yield return @"6NW CoMMENT      UUUUUUU      WAVE";
        yield return @"8NW COMMENT UUUUUUU WAVE";
        yield return @"DNW A   UUUUUUU      WAVE";
        yield return @"FNW A UUUUUUU WAVE";
    }

    public IEnumerable<string> ArgsNoEnd()
    {
        yield return @"5NE   COMMENT    UUUUUUU";
        yield return @"7NE  COMMeNT UUUUUUU";
        yield return @"CNE A      UUUUUUU";
        yield return @"ENE A  UUUUUUU";
    }

    public IEnumerable<string> ArgsQuotesWord()
    {
        yield return @"2QW COMMENT      ""UUUUUUU""      WAVE";
        yield return @"4QW COmMENT ""UUUUUUU"" WAVE";
        yield return @"0QW A  ""UUUUUUU""      WAVE";
        yield return @"BQW A ""UUUUUUU"" WAVE";
    }

    public IEnumerable<string> ArgsQuotesEnd()
    {
        yield return @"1QE  COMMENT        ""UUUUUUU""";
        yield return @"3QE  COMMENT ""UUUUUUU""";
        yield return @"9QE A  ""UUUUUUU""";
        yield return @"AQE A ""UUUUUUU""";
    }

    private static readonly char[] Openings = new char[]
      {
        '"',
        '\'',
        '“',
        '‘',
        '„',
        '«',
        '‹',
      };

    private static readonly char[] Endings = new char[]
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
    static  Regex QuotesFinderI = new(@"[""\“„«‘‹]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static Regex QuotesFinder = new(@"[""\“„«‘‹]", RegexOptions.Compiled);

    static Regex QuotesQAdvanced = new(@"...\s+(?<KEY>\w+)\s+(?<VAL>""(.*)""|'(.*)'|“(.*)”|‘(.*)’|„(.*)“|«(.*)»|‹(.*)›)\s+(?<END>\S+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static Regex QuotesUAdvanced = new(@"...\s+(?<KEY>\w+)\s+(?<VAL>\S+)\s+(?<END>\S+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    [GlobalSetup]
    public void Setup()
    {
    }
    [Benchmark]
    [ArgumentsSource(nameof(Args))]
    public void BEST(string[] ss)
    {
        for (int x = 0; x < ss.Length; x++)
        {
            string s = ss[x];
            CheckQuotesIteratingFromMiddleImpl(s);
            return;
        }
    }
    [Benchmark]
    [ArgumentsSource(nameof(Args))]
    public void CheckQuotesIteratingFromMiddle(string[] ss)
    {
        for (int x = 0; x < ss.Length; x++)
        {
            string s = ss[x];
            CheckQuotesIteratingFromMiddleImpl(s);
            return;
        }
    }

    private static bool CheckQuotesIteratingFromMiddleImpl(string s)
    {
        int length = s.Length;
        int middle = length / 2;
        for (int toRight = 4; toRight < middle; toRight++)
        {
            if (CompareOpening(s[toRight]))
            { return true; }
        }
        for (int toLeft = length - 1; toLeft >= middle; toLeft--)
        {
            if (CompareOpening(s[toLeft]))
            { return true; }
        }
        return false;
    }

    [Benchmark()]
    [ArgumentsSource(nameof(Args))]
    public void CheckQuotesIteratingFromEnd(string[] ss)
    {
        for (int x = 0; x < ss.Length; x++)
        {
            string s = ss[x];
            CheckQuotesIteratingFromEndImpl(s);
            return;
        }
    }

    private static bool CheckQuotesIteratingFromEndImpl(string s)
    {
        for (int toLeft = s.Length - 1; toLeft >= 4; toLeft--)
        {
            if (CompareOpening(s[toLeft]))
            { return true; }
        }
        return false;
    }

    [Benchmark]
    [ArgumentsSource(nameof(Args))]
    public void CheckQuotesIterating(string[] ss)
    {
        for (int x = 0; x < ss.Length; x++)
        {
            string s = ss[x];
            CheckQuotesIteratingImpl(s);
            return;
        }
    }

    private static bool CheckQuotesIteratingImpl(string s)
    {
        int length = s.Length;
        for (int j = 4; j < length; j++)
        {
            if (CompareOpening(s[j]))
            { return true; }
        }
        return false;
    }

    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Args))]
    public void CheckQuotesFind(string[] ss)
    {
        for (int x = 0; x < ss.Length; x++)
        {
            string s = ss[x];
            QuotesFinder.IsMatch(s);
        }
    }
    [Benchmark()]
    [ArgumentsSource(nameof(Args))]
    public void CheckQuotesFindHybrid(string[] ss)
    {
        for (int x = 0; x < ss.Length; x++)
        {
            string s = ss[x];
            if (CompareOpening(s[^1]))
                return;
            QuotesFinder.IsMatch(s);
        }
    }
    [Benchmark()]
    [ArgumentsSource(nameof(Args))]
    public void CheckQuotesFindInsensitive(string[] ss)
    {
        for (int x = 0; x < ss.Length; x++)
        {
            string s = ss[x];
            QuotesFinderI.IsMatch(s);
        }
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
    //[Benchmark]
    //[ArgumentsSource(nameof(Args))]
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
    //[Benchmark]
    //[ArgumentsSource(nameof(Args))]
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
}
