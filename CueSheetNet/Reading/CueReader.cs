﻿using CueSheetNet.AudioFormatReaders;
using CueSheetNet.Internal;
using CueSheetNet.Logging;
using CueSheetNet.Reading;
using CueSheetNet.Syntax;
using CueSheetNet.TextParser;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;
namespace CueSheetNet;
public partial class CueReader
{
    public int CurrentLineIndex { get; private set; } = -1;

    public CueSource Source { get; private set; }

    public string? CurrentLine { get; private set; } = "No line read yet";

    CueSheet? Sheet { get; set; }

    /// <summary>
    /// Character used to enclose text when it contains whitespace
    /// </summary>
    public char Quotation { get; set; } = '"';
    public Encoding? Encoding { get; set; }

    readonly List<bool> TrackHasZerothIndex = new();
    private void Reset()
    {
        Sheet = null;
        CurrentLineIndex = -1;
        CurrentLine = "No line read";
        TrackHasZerothIndex.Clear();
    }
    public CueReader()
    {
    }
    public CueSheet ParseCueSheet(string cuePath)
    {
        Reset();
        if (!File.Exists(cuePath))
        {
            //Logger.LogError("Specified file does not exist ({File})", cuePath);
            throw new FileNotFoundException($"{cuePath} does not exist");
        }
        Source = new CueSource(cuePath);
        LogParseSource();
        if (!File.Exists(cuePath)) throw new FileNotFoundException($"{cuePath} does not exist");
        byte[] cueFileBytes = File.ReadAllBytes(cuePath);
        using MemoryStream fs = new(cueFileBytes, false);
        CueSheet cue = ParseCueSheet_Impl(fs);
        cue.SetCuePath(cuePath);
        return cue;
    }
    /// <summary>Parsing CueSheet from: {Source}</summary>
    private void LogParseSource() => Logger.LogDebug("Parsing CueSheet from: {Source}", Source);
    /// <summary>Parsing started</summary>
    private static void LogParseStart() => Logger.LogDebug("Parsing started");
    public CueSheet ParseCueSheet(byte[] cueFileBytes)
    {
        Reset();
        Source = new CueSource(cueFileBytes);
        LogParseSource();
        using MemoryStream fs = new(cueFileBytes, false);
        return ParseCueSheet_Impl(fs);
    }
    public CueSheet ParseCueSheetFromStringContent(string cueContent)
    {
        Reset();
        Source = CueSource.FromStringContent(cueContent);
        LogParseSource();
        LogParseStart();
        using TextReader txt = new StringReader(cueContent);
        return ReadImpl(txt);
    }
    public CueSheet ParseCueSheet(ReadOnlySpan<char> cueContentChars)
    {
        return ParseCueSheetFromStringContent(new string(cueContentChars));
    }

    private CueSheet ParseCueSheet_Impl(Stream fs)
    {
        LogParseStart();
        Sheet = new();
        if (Encoding is null)
        {
            Stopwatch st = Stopwatch.StartNew();
            var tester = new CueEncodingTester(fs, Source);
            Encoding = tester.DetectCueEncoding();
            st.Stop();
            Logger.LogInformation("Detected {Encoding.EncodingName} encoding in {Time}ms", Encoding, st.ElapsedMilliseconds);
        }
        fs.Seek(0, SeekOrigin.Begin);
        using TextReader strr = new StreamReader(fs, Encoding, false);
        return ReadImpl(strr);
    }

    private CueSheet ReadImpl(TextReader txtRead)
    {
        Stopwatch st = Stopwatch.StartNew();
        while (txtRead.ReadLine()?.Trim() is string line)
        {
            CurrentLineIndex++;
            CurrentLine = line;
            string value = GetKeyword(line).ToUpperInvariant();
            if (!Enum.TryParse(value, out Keywords key)) continue;
            switch (key)
            {
                case Keywords.REM:
                    ParseREM(line);
                    break;
                case Keywords.PERFORMER:
                    ParsePerformer(line);
                    break;
                case Keywords.TITLE:
                    ParseTitle(line);
                    break;
                case Keywords.FILE:
                    ParseFile(line);
                    break;
                case Keywords.CDTEXTFILE:
                    ParseCdTextFile(line);
                    break;
                case Keywords.TRACK:
                    ParseTrack(line);
                    break;
                case Keywords.FLAGS:
                    ParseFlags(line);
                    break;
                case Keywords.INDEX:
                    ParseIndex(line);
                    break;
                case Keywords.POSTGAP:
                case Keywords.PREGAP:
                    ParseGap(line, value);
                    break;
                case Keywords.ISRC:
                    ParseISRC(line);
                    break;
                case Keywords.CATALOG:
                    ParseCatalog(line);
                    break;
            }
        }
        for (int i = 0; i < TrackHasZerothIndex.Count; i++)
        {
            Sheet!.SetTrackHasZerothIndex(i, TrackHasZerothIndex[i]);
        }
        Sheet!.RefreshIndices();
        st.Stop();
        Logger.LogInformation("Finished parsing {Source} in {Time}ms", Source, st.ElapsedMilliseconds);
        Sheet.SourceEncoding = Encoding;
        return Sheet;
    }

    private void ParseTitle(string line)
    {
        string? title = GetValue(line, 5);// TITLE_
        if (title == null)
        {
            Logger.LogWarning("Invalid TITLE at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }

        if (Sheet!.LastTrack is CueTrack lastTrack)
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
        (string path, string type) = GetFile(line, 5);// FILE_
        _ = Sheet!.AddFile(path, type);
    }
    private void ParseTrack(string line)
    {
        string num = GetKeyword(line, 6);// TRACK_
        if (!int.TryParse(num, out int number))
        {
            number = Sheet!.LastTrack?.Number + 1 ?? 1;
            Logger.LogWarning("Invalid TRACK number at line {Line number}: \\\"{Line}\\\"\". Substituting {Substitute number:d2}", CurrentLineIndex, CurrentLine, number);
        }
        _ = Sheet!.AddTrack(number);

    }
    private void ParsePerformer(string line)
    {
        string? performer = GetValue(line, 10);// PERFORMER_
        if (performer == null)
        {
            Logger.LogWarning("Invalid PERFORMER at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }

        if (Sheet!.LastTrack is CueTrack track)
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
        string? cdt = GetValue(line, 11); // CDTEXTFILE_
        if (cdt == null)
        {
            Logger.LogWarning("Invalid CDTEXT at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }
        Sheet!.SetCdTextFile(cdt);
    }
    private void ParseFlags(string line)
    {
        if (Sheet!.LastTrack is not CueTrack track)
        {
            Logger.LogWarning("FLAGS present before any track at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }
        TrackFlags flags = TrackFlags.None;
        string[] parts = line[6..] // FLAGS_
            .Replace("\"", "")
            .Replace("'", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            char trim = part[0];
            flags |= trim switch
            {
                '4' or 'f' or 'F' => TrackFlags.FourChannel,
                'p' or 'P' or 'e' or 'E' => TrackFlags.PreEmphasis,
                'd' or 'D' or 'C' or 'c' => TrackFlags.DigitalCopyPermitted,
                's' or 'S' => TrackFlags.SerialCopyManagementSystem,
                _ => TrackFlags.None,
            };
        }
        track.Flags = flags;
    }
    private void ParseISRC(string line)
    {
        if (Sheet!.LastTrack is not CueTrack track) return;
        string? isrc = GetValue(line, 5);// ISRC_
        if (isrc == null)
        {
            Logger.LogWarning("Invalid ISRC at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);

            return;
        }
        track.ISRC = isrc;
    }
    private void ParseCatalog(string line)
    {
        string? cata = GetValue(line, 8);// CATALOG_
        if (cata == null)
        {
            Logger.LogWarning("Invalid CATALOG at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }
        Sheet!.Catalog = cata;
    }
    private void ParseIndex(string line)
    {
        if (Sheet!.LastTrack is null)
        {
            Logger.LogWarning("INDEX line present before any track at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }
        string number = GetKeyword(line, 6);// INDEX_
        if (!int.TryParse(number, out int num))
        {
            //Logger.LogError("Incorrect Index number format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException($"Incorrect Index number format at line {CurrentLineIndex}: {line}");
        }
        if (!CueTime.TryParse(line.AsSpan(6 + number.Length + 1), out CueTime cueTime))
        {
            //Logger.LogError("Incorrect Index format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException($"Incorrect Index format at line {CurrentLineIndex}: {line}");
        }
        if (Sheet.LastTrack is CueTrack ctr && Sheet.LastFile is CueFile cfl)
        {
            ctr.ParentFile = cfl;
        }
        CueIndexImpl c = Sheet.AddIndexInternal(cueTime);
        (int Start, int End) = Sheet.GetCueIndicesOfTrack(c.Track.Index);
        //If this is the first added index for the track (by default new track do not have 0th index so its starts at 1)
        if (End - Start == 1)
            TrackHasZerothIndex.Add(num == 0);
    }
    private void ParseGap(string line, string gapType)
    {
        if (Sheet!.LastFile is null)
        {
            Logger.LogWarning("GAP line present before any file at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }
        if (Sheet!.LastTrack is not CueTrack track)
        {
            Logger.LogWarning("GAP line present before any track at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            return;
        }

        string type = GetKeyword(line, 0);
        if (!CueTime.TryParse(line.AsSpan(6 + type.Length + 1), out CueTime cueTime))
        {
            //Logger.LogError("Incorrect Gap format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException($"Incorrect Gap format at line {CurrentLineIndex}: {line}");
        }
        if (gapType.StartsWith("PRE"))
            track.PreGap = cueTime;
        else
            track.PostGap = cueTime;
    }
    private void ParseREM(string line)
    {
        string field = GetKeyword(line, 4).ToUpperInvariant();
        int valueStart = 4 + field.Length + 1;
        string? value = GetValue(line, valueStart);
        if (value == null) return;
        if (Sheet!.LastTrack is CueTrack track)
        {
            switch (field)
            {
                case "COMPOSER":
                    track.Composer = value;
                    break;
                case "COMMENT":
                    Sheet.AddComment(value);
                    break;
                default:
                    Sheet.AddRemark(field, value);
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
                    Sheet.AddRemark(field, value);
                    break;
            }
        }
    }

    /// <summary>
    /// Get the filename and file type. If quotation marks are present, simple iterating algorithm is used. Otherwise Regex is used
    /// </summary>
    /// <param name="s">String to get values from</param>
    /// <returns><see cref="ValueTuple"/> of filename and type</returns>
    private (string Path, string Type) GetFile(string s, int start)
    {
        ReadOnlySpan<char> spanny = s.AsSpan(start).Trim();
        string path, type;
        if (spanny[0] == Quotation)
        {
            int end = -1;
            for (int i = spanny.Length - 1; i >= 1; i--)
            {
                if (spanny[i] == Quotation)
                {
                    end = i;
                    break;
                }
            }
            if (end > 0)
            {
                path = spanny[1..end].ToString();
                type = GetSuffix(s.AsSpan()[end..]);
                return (path, type);
            }
        }
        // Line does not have Quotation, need to use regex
        string emer = spanny.ToString();
        Match m = NonQuotedFileRegex().Match(emer);
        if (m.Success)
        {
            path = m.Groups["PATH"].Value;
            type = m.Groups["TYPE"].Value;
            return (path, type);
        }
        // COuld not properly parse Path Type. Return Line as path, no type
        return (emer, "");
    }
    /// <summary>
    /// Gets the value in quotation marks, or trimmed area if no quotations are present
    /// </summary>
    /// <param name="s">String to get value from</param>
    /// <param name="start">Index from which the search will be started</param>
    /// <param name="end">How many characters are skipped at the end</param>
    /// <returns>String if there was text, enclosed in quotations or not; <see cref="null"/> if there was no non-whitespace text, or there were quutations with nothing inside</returns>
    private string? GetValue(string s, int start, int end = 0)
    {
        ReadOnlySpan<char> spanny = s.AsSpan(start, s.Length - start - end).Trim();
        if (spanny.Length == 0)
            return null;
        if (spanny.Length == 2 && spanny[0] == spanny[1] && spanny[0] == Quotation)
            return null;
        if (spanny[^1] == Quotation && spanny[0] == Quotation)
            return spanny[1..^1].ToString();
        else
            return spanny.ToString();
    }
    /// <summary>
    /// Get the first full word from the specified start, stops at whitespace
    /// </summary>
    /// <param name="s">String to get value from</param>
    /// <param name="startIndex">INdex from which the word will be sought</param>
    /// <param name="maxSearchLength">Maximum search length. If exceeded, empty string is returned</param>
    /// <returns>String of characters from <paramref name="startIndex"/> to first whitespace, or empty string if no whitespace has been detected in the first <paramref name="maxSearchLength"/> characters</returns>
    private static string GetKeyword(string s, int startIndex = 0, int maxSearchLength = -1)
    {
        if (maxSearchLength <= 0) maxSearchLength = s.Length;
        maxSearchLength = Math.Clamp(maxSearchLength, 0, s.Length - startIndex);
        ReadOnlySpan<char> spanish = s.AsSpan(startIndex, maxSearchLength).TrimStart();
        for (int i = 0; i < spanish.Length; i++)
        {
            if (char.IsWhiteSpace(spanish[i]))
                return spanish[..i].ToString();
        }
        return spanish.ToString();
    }
    /// <summary>
    /// GEts the last word of the string (string from the last whitespace till the end)
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private static string GetSuffix(ReadOnlySpan<char> s)
    {
        s = s.TrimEnd();
        for (int i = s.Length - 1; i >= s.Length / 2; i--)
        {
            if (char.IsWhiteSpace(s[i]))
                return s[i..].ToString();
        }
        return string.Empty;
    }

    [GeneratedRegex(@"(?<PATH>.*\.\S+)\s+(?<TYPE>\w*)", RegexOptions.Compiled)]
    private static partial Regex NonQuotedFileRegex();
}