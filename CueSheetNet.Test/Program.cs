// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using CueSheetNet.Logging;
using CueSheetNet.Test;
using CueSheetNet.TextParser;
using System.Diagnostics;
using System.Drawing;
using System.Text;
IEnumerable<T> Repeat<T>(T[] em, int count)
{
	for (int i = 0; i < count; i++)
	{
		foreach (var item in em)
		{
			yield return item;
		}
	}
}

int j = 1; //frames
decimal t = j * (decimal)CueTime.MillisecondsPerFrame * TimeSpan.TicksPerMillisecond;
long tt = (long)t;

var q = tt / (decimal)CueTime.MillisecondsPerFrame / TimeSpan.TicksPerMillisecond; 
var q2 = tt / CueTime.MillisecondsPerFrame / TimeSpan.TicksPerMillisecond; 



for (int i = -5000; i < 5000000; i++)
{
	CueTime ct = new(i);
	TimeSpan ts = ct.ToTimeSpan();
	var p = ct + ts;
	CueTime back = new CueTime(ts);
	Debug.Assert(ct == back, $"{ct.TotalFrames:d25}\r\n{back.TotalFrames:d25}");
	//Debug.WriteLineIf(ct != back, $"------\r\n{ct.TotalFrames:d25}\r\n{back.TotalFrames:d25}");
}




return;

string PATH= @"C:\Users\Pedro\Downloads\CUE\Violator.cue";
var eo = new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true, ReturnSpecialDirectories = false };
var fies = Directory.GetFiles(@"E:\FLACBAZA\RawRips\", "*.cue", eo);
string[] dafak = Repeat(fies, 100).ToArray();
List<CueSheet> l = new(dafak.Length);
HybridLogger hb = new();
Logger.Register(hb);
CueReader cr = new();
var s = Stopwatch.StartNew();
foreach (var f in dafak)
{
	var ccc = cr.ParseCueSheet(f);
    l.Add(ccc);
}
s.Stop();
Console.WriteLine(s.ElapsedTicks *1.0 / dafak.Length);
return;
CueWriter cueWriter = new() { InnerQuotationReplacement=InnerQuotation.Guillemets, IndentationDepth=2 };
var tttttttt = cueWriter.WriteToString(l[0]);
tttttttt = cueWriter.WriteToString(l[0]);
tttttttt = cueWriter.WriteToString(l[0]);
Stopwatch ff = Stopwatch.StartNew();
tttttttt = cueWriter.WriteToString(l[0]);
ff.Stop();
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
