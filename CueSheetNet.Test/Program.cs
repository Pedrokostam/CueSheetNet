// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using System.Diagnostics;

var eo=new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories=true,ReturnSpecialDirectories=false };
var fies = Directory.GetFiles(@"E:\FLACBAZA\RawRips\", "*.cue", eo);
List<CueSheet> l = new();
var s = Stopwatch.StartNew();
foreach (var f in fies)
{
l.Add( CueSheetNet.CueSheet.ParseCueSheet(f));
}
l.Clear();
foreach (var f in fies)
{
    l.Add(CueSheetNet.CueSheet.ParseCueSheet(f));
}
l.Clear();
foreach (var f in fies)
{
    l.Add(CueSheetNet.CueSheet.ParseCueSheet(f));
}
l.Clear();
foreach (var f in fies)
{
    l.Add(CueSheetNet.CueSheet.ParseCueSheet(f));
}
s.Stop();
Console.WriteLine(s.ElapsedMilliseconds);
Console.WriteLine(fies.Length) ;
