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
    var h = FileSystemInfoComparer.Instance.GetHashCode(new FileInfo(@"C:\Users\Pedro\OneDrive\FLACBAZA\RawRips\drive\Marek Grechuta - 40 piosenek I.cue"));
    var res = CueReader.CreateFromFile(@"C:\Users\Pedro\OneDrive\FLACBAZA\RawRips\drive\Marek Grechuta - 40 piosenek I.cue").Parse();
    var q = res.AdditionalFiles;
    var s = new CueWriter(null).WriteToString(res);
    Console.WriteLine(s);
    Console.WriteLine("----------------------------------------");
    foreach (var item in q)
    {
        Console.WriteLine(item.AbsolutePath);
    }
    Console.WriteLine("----------------------------------------");
    foreach (var item in res.Files)
    {
        Console.WriteLine(item.AbsolutePath);
        Console.WriteLine(item.Meta);
        Console.WriteLine();
    }
    //var dir = new DirectoryInfo(@"./TestItems/");
    //var files = dir.EnumerateFiles("*.cue").ToList();
    //List<string> outp = [];
    //for (int i = 0; i < 10; i++)
    //{

    //    foreach (var item in files)
    //    {
    //        var reader = CueReader.CreateFromFile(item);
    //        var c = reader.Parse();
    //        outp.Append(new CueWriter().WriteToString(c));
    //    }
    //}
}

