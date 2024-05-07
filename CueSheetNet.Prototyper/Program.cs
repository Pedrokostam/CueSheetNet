// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using CueSheetNet;
using CueSheetNet.FormatReaders;
using CueSheetNet.Logging;
using CueSheetNet.Test;

xd();
void xd()
{
    DebugLogger hybridLogger = new DebugLogger(LogLevel.None);
    CueSheetNet.Logging.Logger.Register(hybridLogger);
    Stopwatch sw = new Stopwatch();
    //var reader = new CueReader();
    sw.Start();
    //var c = reader.ParseCueSheet(@"C:\Users\Pedro\Documents\Github\CueSheetNet\CueSheetNet.Prototyper\TestItems\MultiFile.cue");
    sw.Stop();
    Console.WriteLine(sw.ElapsedTicks);
    sw.Restart();
    var reader2 = new CueReader2();
    var sheet = reader2.Read(@"C:\Users\Pedro\Documents\Github\CueSheetNet\CueSheetNet.Prototyper\TestItems\MultiFile.cue");
    sw.Stop();
    Console.WriteLine(sw.ElapsedTicks);
    var s = new CueWriter().WriteToString(sheet);
}

