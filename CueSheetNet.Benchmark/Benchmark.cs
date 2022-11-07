using System.Diagnostics;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace CueSheetNet.Benchmark;

public record struct TS(string A, string B, string C, string D, string F, string E, string G) { }
public record C(string A, string B, string CC, string D, string F, string E, string G) { }
[MemoryDiagnoser(false)]
public class Benchmark
{
    public static string A { get; set; } = "141521171618542";
    public TS TTT { get; set; } = new TS(A, A, A, A, A, A, A);
    public C CCC { get; set; } = new C(A, A, A, A, A, A, A);

    private static string T(string a, string b, string c, string D, string E, string F, string G)
    {
        return a + b + c+D+E+F+G;
    }
    private static string T(TS ts)
    {
        return ts.A + ts.B + ts.C +ts.F+ts.E+ts.F+ts.G;
    }
    private static string C(C ts)
    {
        return ts.A + ts.B + ts.CC + ts.F + ts.E + ts.F + ts.G;
    }

    [Benchmark(Baseline = true)]
    public string JoinSep() => T(A, A, A,A,A,A,A);
    [Benchmark]
    public string JoinStr() => T(new(A, A, A,A,A,A,A));
    [Benchmark]
    public string JoinTTT() => T(TTT);
    [Benchmark]
    public string JoinStrC() => C(new(A, A, A, A, A, A, A));
    [Benchmark]
    public string JoinTTTC() => C(CCC);
}
