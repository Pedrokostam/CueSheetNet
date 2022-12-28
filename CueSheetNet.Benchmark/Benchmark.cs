using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CueSheetNet.Benchmark;

[MemoryDiagnoser(true)]
public class Benchmark
{
    public static string[] Text =
    {
        "hapukle",
        "kerfufle",
        "kerfuba",
        "dlfjkdgvnkjdfnvkjdfnv",
        "osdjkfnfgoerjf",
        "jsdfbnvkerfubasdfvsdf",
        "jsdfbnvhapuklesdf",
        "jsdfbnvkerfuflesdfvsdf",
        "sdf",
        "s",
        "sjkdnmfjklsdnfjklnsdjklfnsdkjlfjnmsdjklfnsdkjlfnjskldnf",
        "iqwjeqo328"
    };
    public string[] Input { get; set; }
    [GlobalSetup]
    public void Init()
    {
        int x = 50000;
        int l = x * Text.Length;
        Input = new string[l];
        for (int i = 0; i < l; i++)
        {
            Input[i] = Text[i % Text.Length];
        }

    }
    public static IEnumerable<string> Patterns => new string[] {
        "^kerfuba$",
        "^.?.?.?.?.?.?kerfuba.?.?.?.?.?$",
        "^.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?kerfuba.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?.?$",
        "^sdf$",
        "^.?.?.?sdf$",
        "^sdf.?.?.?$",
    };
    [ParamsSource(nameof(Patterns))]
    public string Pattern { get; set; } = "^kerfuba$";

    [Benchmark]
    public List<Match> Natural()
    {
        List<Match> Results = new List<Match>();
        foreach (var item in Input)
        {
            var m = Regex.Match(item, Pattern, RegexOptions.IgnoreCase);
            if (m.Success)
                Results.Add(m);
        }
        return Results;
    }
    [Benchmark]
    public List<Match> Compiled()
    {
        var r = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        List<Match> Results = new List<Match>();
        foreach (var item in Input)
        {
            var m = r.Match(item);
            if (m.Success)
                Results.Add(m);
        }
        return Results;
    }
    [Benchmark]
    public List<Match> Compiled_Parallel()
    {
        var r = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        ConcurrentBag<Match> Results = new();
        Parallel.ForEach(Input, (x) =>
        {
            var m = r.Match(x);
            if (m.Success)
                Results.Add(m);
        });
        return Results.AsEnumerable().ToList();
    }
    [Benchmark]
    public List<Match> Natural_Parallel()
    {
        var r = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        ConcurrentBag<Match> Results = new();
        Parallel.ForEach(Input, (x) =>
        {
            var m = Regex.Match(x, Pattern, RegexOptions.IgnoreCase);
            if (m.Success)
                Results.Add(m);
        });
        return Results.AsEnumerable().ToList();
    }
}
