using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace CueSheetNet.Benchmark;

[MemoryDiagnoser(true)]
public class Benchmark
{
    public string Path { get; set; } = @"C:\Users\Pedro\Repos\Sony - PlayStation - Cuesheets (10914) (2026-06-15 11-55-46)\Sabrina l'Apprentie Sorciere et l'Horloge Cosmique (France).cue";

    CueReader2 Reader { get; set; } = CueReader2.CreateFromString("");


    [Benchmark(Baseline = true)]
    public void Streamable()
    {
        var c = Reader.FromFile(Path).Parse();
    }
    [Benchmark]
    public void Spannable()
    {
        var c = Reader.FromFile(Path).Parse();
    }
}
