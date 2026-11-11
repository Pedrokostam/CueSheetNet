// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using CueSheetNet.FormatReaders;
using CueSheetNet.Helpers;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

xd();
void xd()
{
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss.fff ";
            });
    });

    ILogger clog = loggerFactory.CreateLogger("test");
    CueSheetNet.Loggingq.SetLogger(clog);
    var cue = CueReader.CreateFromFile(@"./TestItems/Jethro Tull - Aqualung.cue").Parse();
    Console.WriteLine(CueWriter.Create().WriteToString(cue));
}

