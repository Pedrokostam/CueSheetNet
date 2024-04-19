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
    var s = new string[]
    {
        @"E:\Nowy folder\audiobook220805\iluzjo030_Iluzjonista.mp3",
        @"E:\Torrent\vmware\Various Artists - Cyberpunk 2077 Radio, Vol. 2 (Original Soundtrack) (2020)\01. Ponpon Shit.flac",
        @"E:\JAWALE\_Soundtracks\Scrapland (EMU).zophar\epic.ogg",
        @"E:\RIPPER COMPARISON\EAC\KAT - Bastard.wav",
        @"C:\Program Files\Microsoft Office\root\Office16\Media\DefaultHold.wma",
    };
    foreach (var item in s)
    {
       Console.WriteLine(Path.GetFileName(item));
       Console.WriteLine();
    }
}

