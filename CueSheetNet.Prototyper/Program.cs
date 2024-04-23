// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using CueSheetNet.FormatReaders;
using CueSheetNet.Logging;
using CueSheetNet.Test;

xd();
void xd()
{
    DebugLogger hybridLogger = new DebugLogger(LogLevel.None);
    CueSheetNet.Logging.Logger.Register(hybridLogger);
    var reader = new CueReader();
    var c = reader.ParseCueSheet(@"./TestItems/Spandau Ballet - True.cue");
    var reader2 = new CueReader2();
    reader2.Read(@"./TestItems/Spandau Ballet - True.cue");
    
}

