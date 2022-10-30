// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using CueSheetNet.Benchmark;

var r = new Benchmark();
r.Setup();
BenchmarkRunner.Run<Benchmark2>();