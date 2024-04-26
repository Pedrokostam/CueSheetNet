using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace CueSheetNet.Benchmark;

[MemoryDiagnoser(true)]
public class ReadingBenchmark
{
    [Params(
        "Normal",
        "Long",
        "Multi"
        )]
    public string Name { get; set; }
    private string GetFilePath()
    {
        return Path.Join(".", "TestItems", $"{Name}.cue");
    }

    [Benchmark]
    public void Read()
    {
        var file = GetFilePath();
        CueSheet.Read(file);
    }

}
