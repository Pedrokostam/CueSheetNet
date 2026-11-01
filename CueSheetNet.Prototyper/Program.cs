// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using CueSheetNet.FormatReaders;
using CueSheetNet.Logging;
using CueSheetNet.Test;

xd();
void xd()
{
    DebugLogger hybridLogger = new DebugLogger(LogLevel.All);
    CueSheetNet.Logging.Logger.Register(hybridLogger);
    var dir = new DirectoryInfo(@"./TestItems/");
    var files = dir.EnumerateFiles("*.cue").ToList();
    List<string> outp = [];
    for (int i = 0; i < 10000; i++)
    {

        foreach (var item in files)
        {
            var reader = new CueReader();
            var c = reader.ParseCueSheet(item.FullName);
            outp.Append(new CueWriter().WriteToString(c));
        }
    }

    foreach (var entry in hybridLogger.LogEntries)
    {
        Console.WriteLine(entry);
    }
}

