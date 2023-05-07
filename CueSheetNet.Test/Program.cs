// See https://aka.ms/new-console-template for more information
using CueSheetNet;
using CueSheetNet.Logging;
using CueSheetNet.Test;
using CueSheetNet.TextParser;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Xml.Linq;

xd();
void xd()
{
    int a = 123;
    Console.WriteLine(a);
    var x = new Decimal[100000000];
    var y = new ArraySegment<decimal>(x, 0, 50000000).Array;
    bool z = x.Length == y.Length;
    Console.WriteLine(a);
}


