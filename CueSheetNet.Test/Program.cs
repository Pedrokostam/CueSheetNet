// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using System.Diagnostics;

var eo = new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true, ReturnSpecialDirectories = false };
var fies = Directory.GetFiles(@"E:\FLACBAZA\RawRips\", "*.cue", eo);
var files = fies.Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies).Concat(fies);
files = files.Concat(files);
string[] dafak = files.ToArray();
List<CueSheet> l = new(dafak.Length);
var s = Stopwatch.StartNew();
CueReader cr = new();
foreach (var f in dafak)
{
    l.Add(cr.ParseCueSheet(f));
}

s.Stop();
Console.WriteLine(s.ElapsedMilliseconds);
Console.WriteLine(1d * s.ElapsedMilliseconds / files.Count() * 1000);
