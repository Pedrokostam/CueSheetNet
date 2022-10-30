// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using System.Diagnostics;
string PATH= @"C:\Users\Pedro\Downloads\CUE\Violator.cue";
var eo = new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true, ReturnSpecialDirectories = false };
var fies = Directory.GetFiles(@"E:\FLACBAZA\RawRips\", "*.cue", eo);
var files = fies;
files = files;
string[] dafak = files.ToArray();
List<CueSheet> l = new(dafak.Length);
var s = Stopwatch.StartNew();
CueReader cr = new();
foreach (var f in dafak)
{
    l.Add(cr.ParseCueSheet(f));
}
var c = new CueSheet(PATH);
c.Date = 1990;
c.AddComment("GENRE Synthpop");
c.AddComment("ExactAudioCopy v0.99pb5");
c.DiscID = "840B0609";
c.Performer = "Depeche Mode";
c.Title = "Violator";
CueTrack track;
CueFile file;

file = c.AddFile("01 - World In My Eyes.flac", "WAVE");
file = c.AddFile("01 - World In My Eyes.flac", "WAVE");
file = c.AddFile("01 - World In My Eyes.flac", "WAVE");
file = c.AddFile("01 - World In My Eyes.flac", "WAVE");
file = c.AddFile("01 - World In My Eyes.flac", "WAVE");
file = c.AddFile("01 - World In My Eyes.flac", "WAVE");
file = c.AddFile("01 - World In My Eyes.flac", "WAVE");
s.Stop();
Console.WriteLine(s.ElapsedMilliseconds);
Console.WriteLine(1d * s.ElapsedMilliseconds / files.Count() * 1000);
