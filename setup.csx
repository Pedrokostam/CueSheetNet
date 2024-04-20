// #r "./CueSheetNet/bin/Debug/net8.0/CueSheetNet.dll"
// using System.Collections.Generic;
// using CueSheetNet;

var testDir = new DirectoryInfo(@"./CueSheetNet.Prototyper/TestItems");
var cueFiles = testDir.GetFiles("*.cue").Select(x=>x.FullName).ToArray();
