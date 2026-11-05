// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using CueSheetNet.FormatReaders;
using CueSheetNet.Logging;
using CueSheetNet.Test;
using Microsoft.Extensions.Logging;

xd();
void xd()
{
    DebugLogger hybridLogger = new DebugLogger(CueSheetNet.Logging.LogLevel.All);
    CueSheetNet.Logging.Logger.Register(hybridLogger);
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug)
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss.fff ";
            });
    });

    ILogger clog = loggerFactory.CreateLogger("test");
    CueSheetNet.Loggingq.SetLogger(clog);
    var dir = new DirectoryInfo(@"./TestItems/");
    var files = dir.EnumerateFiles("*.cue").ToList();
    List<string> outp = [];
    for (int i = 0; i < 10; i++)
    {

        foreach (var item in files)
        {
            var reader = CueReader2.CreateFromFile(item);
            var c = reader.Parse();
            outp.Append(new CueWriter().WriteToString(c));
        }
    }

    //foreach (var entry in hybridLogger.LogEntries)
    //{
    //    Console.WriteLine(entry);
    //}
}

