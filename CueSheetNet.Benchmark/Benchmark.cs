using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CueSheetNet.Benchmark;

[MemoryDiagnoser(true)]
public class Benchmark
{
    [Params(10,1000,1000000)]
    public int Size { get; set; }
    public IEnumerable<decimal> A { get; set; }= [];
    public IEnumerable<decimal> B()
    {
        for (int i = 0; i < Size; i++)
        {
            yield return (decimal)i;
        }
    }

    [GlobalSetup]
  public  void Setup()
    {
        A = B();
    }
    private List<decimal> ListImpl()
    {
        return A.ToList();
    }
    private decimal[] ArrayImpl()
    {
        return A.ToArray();
    }
    private ImmutableArray<decimal> ImmutableArrayImpl()
    {
        return A.ToImmutableArray();
    }
    private ImmutableList<decimal> ImmutableListImpl()
    {
        return A.ToImmutableList();
    }
    [Benchmark(Baseline =true)]
    public int List() => ListImpl().Count;
    [Benchmark]
    public int Array() => ArrayImpl().Length;
    [Benchmark]
    public int ImmutableArray() => ImmutableArrayImpl().Length;
    [Benchmark]
    public int ImmutableList() => ImmutableListImpl().Count;
}
