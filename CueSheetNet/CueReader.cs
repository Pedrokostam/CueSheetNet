using CueSheetNet.FileIO;
using CueSheetNet.TextParser;
using System.Text;
using System.Text.RegularExpressions;

namespace CueSheetNet;

internal class CueReader
{
    CueSheet Sheet { get; set; }

    readonly List<bool> TrackHasZerothIndex = new();
    static public Encoding GetEncoding(string filePath)
    {
        throw new NotImplementedException();
    }
    internal CueReader(string cuepath)
    {
        if (!File.Exists(cuepath)) throw new FileNotFoundException($"{cuepath} does not exist");
        Sheet = new(cuepath);
    }

    internal CueSheet ParseCueSheet()
    {
        if (!File.Exists(Sheet.FileInfo?.FullName)) throw new FileNotFoundException($"{Sheet.FileInfo?.FullName ?? "Null path"} does not exist");
        var t = File.ReadAllBytes(Sheet.FileInfo.FullName);
        using MemoryStream fs = new(t,false);
        //using FileStream fs = new(Sheet.FileInfo.FullName, FileMode.Open, FileAccess.Read);
        Encoding enc = CueEncodingTester.DetectCueEncoding(fs);
        fs.Seek(0, SeekOrigin.Begin);

        using StreamReader strr = new(fs, enc);
        while (strr.ReadLine()?.Trim() is string line)
        {
            var keyMatch = LineParser.Keyword.Match(line);
            if (!keyMatch.Success) continue;
            if (!Enum.TryParse(keyMatch.Groups["KEY"].Value.ToUpperInvariant(), out CueKeyWords key)) continue;
            switch (key)
            {
                case CueKeyWords.REM:
                    ParseREM(line);
                    break;
                case CueKeyWords.PERFORMER:
                    ParsePerformer(line);
                    break;
                case CueKeyWords.TITLE:
                    ParseTitle(line);
                    break;
                case CueKeyWords.FILE:
                    ParseFile(line);
                    break;
                case CueKeyWords.CDTEXTFILE:
                    ParseCdTextFile(line);
                    break;
                case CueKeyWords.TRACK:
                    ParseTrack(line);
                    break;
                case CueKeyWords.FLAGS:
                    ParseFlags(line);
                    break;
                case CueKeyWords.INDEX:
                    ParseIndex(line);
                    break;
                case CueKeyWords.GAP:
                    ParseGap(line);
                    break;
                case CueKeyWords.ISRC:
                    ParseISRC(line);
                    break;
                case CueKeyWords.CATALOG:
                    ParseCatalog(line);
                    break;
            }
        }
        for (int i = 0; i < TrackHasZerothIndex.Count; i++)
        {
            Sheet.SetTrackHasZerothIndex(i, TrackHasZerothIndex[i]);
        }
        Sheet.RefreshIndices();
        return Sheet;
    }

    private void CheckCueType(string contentSingle)
    {
        bool gaps = contentSingle.Contains("PREGAP", StringComparison.OrdinalIgnoreCase)
                    || contentSingle.Contains("PREGAP", StringComparison.OrdinalIgnoreCase);
        int fileNum = Regex.Matches(contentSingle, @"\s+FILE", RegexOptions.IgnoreCase).Count;
        bool fileAfterIndex0 = Regex.IsMatch(contentSingle, @"FILE(.|\n)*?INDEX\s+0+\s+.*\nFILE", RegexOptions.IgnoreCase);

        if (fileAfterIndex0)
            Sheet.SheetType = CueType.EacStyle;
        else if (fileNum > 1)
            Sheet.SheetType = gaps ? CueType.MultipleFileWithSimulatedGaps : CueType.MultipleFilesWithPrependedGaps;
        else
            Sheet.SheetType = CueType.SingleFile;
    }

    private void ParseTitle(string line)
    {
        if(!LineParser.TryGetOneValue(line,"TITLE", out string? title))
            return;
        if (Sheet.LastTrack is CueTrack lastTrack)
        {
            lastTrack.Title = title;
        }
        else
        {
            Sheet.Title = title;
        }
    }
    private void ParseFile(string line)
    {
        if (!LineParser.TryGetFilepath(line, out string? path, out string? type))
            return;
        Sheet.AddFile(path, type);
    }
    private void ParseTrack(string line)
    {
        if (!LineParser.TryGetTrack(line, out int trackNumber))
            return;
       Sheet.AddTrack(trackNumber);
    }
    private void ParsePerformer(string line)
    {
        if (!LineParser.TryGetOneValue(line,"PERFORMER", out string? performer))
            return;
        if (Sheet.LastTrack is CueTrack track)
        {
            track.Performer = performer;
        }
        else
        {
            Sheet.Performer = performer;
        }
    }
    private void ParseCdTextFile(string line)
    {
        if (LineParser.TryGetOneValue(line,"CDTEXTFILE", out string? cdt))
            Sheet.SetCdTextFile(cdt);
    }
    private void ParseFlags(string line)
    {
        if (Sheet.LastTrack is not CueTrack track) return;
        LineParser.TryGetFlags(line, out CueTrackFlags flags);
        track.Flags = flags;
    }
    private void ParseISRC(string line)
    {
        if (Sheet.LastTrack is not CueTrack track) return;
        if(!LineParser.TryGetOneValue(line,"ISRC", out string? isrc))
        track.ISRC = isrc;
    }
    private void ParseCatalog(string line)
    {
        if (!LineParser.TryGetOneValue(line, "CATALOG", out string? cat))
            Sheet.Catalog = cat;
    }
    private void ParseIndex(string line)
    {
        Match indMatch = LineParser.Index.Match(line);
        if (!indMatch.Success) return;
        if (Sheet.LastTrack is not CueTrack track) return;
        if (int.TryParse(indMatch.Groups["NUMBER"].Value, out int num)
            && int.TryParse(indMatch.Groups["MINUTES"].Value, out int min)
            && int.TryParse(indMatch.Groups["SECONDS"].Value, out int sec)
            && int.TryParse(indMatch.Groups["FRAME"].Value, out int fr)
            )
        {
            if (Sheet.LastTrack is CueTrack ctr && Sheet.LastFile is CueFile cfl)
            {
                ctr.ParentFile = cfl;
            }

            CueTime time = new(min, sec, fr);
            CueIndexImpl c = Sheet.AddIndex(time);
            (int Start, int End) = Sheet.GetCueIndicesOfTrack(c.Track.Index);
            //If this is the first added index for the track (by default new track do not have 0th index so its starts at 1)
            if (End - Start == 1)
                TrackHasZerothIndex.Add(num == 0);
        }
    }
    private void ParseGap(string line)
    {
        Match gapMatch = LineParser.Gap.Match(line);
        if (!gapMatch.Success) return;
        if (Sheet.LastFile is not CueFile track) return;
        if (int.TryParse(gapMatch.Groups["MINUTES"].Value, out int min)
            && int.TryParse(gapMatch.Groups["SECONDS"].Value, out int sec)
            && int.TryParse(gapMatch.Groups["FRAME"].Value, out int fr))
        {
            if (gapMatch.Groups["GAPTYPE"].Value.Equals("PRE", StringComparison.OrdinalIgnoreCase))
                track.PreGap = new(min, sec, fr);
            else
                track.PostGap = new(min, sec, fr);
        }
    }
    private void ParseREM(string line)
    {
        //(@"\s*REM\s+(?<FIELD>\S+)\s+[""|']?(?<VALUE>.+)[""|']?", opt);//REM COMMENT 'przerwa xD'
        Match remMatch = QuoteFinder.CheckClosedQuotesPresence(line) ? LineParser.Rem.Match(line) : LineParser.RemNQ.Match(line);
        if (!remMatch.Success) return;
        string value = remMatch.Groups["VALUE"].Value;
        string field = remMatch.Groups["FIELD"].Value.ToUpperInvariant();
        if (Sheet.LastTrack is CueTrack track)
        {
            switch (field)
            {
                case "COMPOSER":
                    track.Composer = value;
                    break;
                case "COMMENT":
                    track.Comment = value;
                    break;
                default:
                    break;
            }
        }
        else
        {
            switch (field)
            {
                case "COMPOSER":
                    Sheet.Composer = value;
                    break;
                case "DATE":
                    Sheet.Date = int.TryParse(value, out int d) ? d : null;
                    break;
                case "DISCID":
                    Sheet.DiscID = value;
                    break;
                case "COMMENT":
                    Sheet.AddComment(value);
                    break;
                default:
                    Sheet.AddComment(field + " " + value);
                    break;
            }
        }
        CueExtensions.Parse(remMatch.Groups["FLAGS"].Value);
    }
}