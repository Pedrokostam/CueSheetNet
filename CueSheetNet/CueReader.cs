using CueSheetNet.FileIO;
using CueSheetNet.Logging;
using CueSheetNet.Syntax;
using CueSheetNet.TextParser;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;

namespace CueSheetNet;

public class CueReader
{
    static private readonly Lazy<Regex> EmergencyFile = new(() => new(@"(?<PATH>.*\..+) (?<TYPE>\w*)", RegexOptions.Compiled));

    public int CurrentLineIndex { get;private set; }
    public string CurrentPath { get;private set; }
    public string CurrentLine { get; private set; }

    private void LogError(string msg) => Logger.Log(LogLevel.Error, msg, CurrentPath, $"Line {CurrentLineIndex}: \"{CurrentLine}\"");
    private void LogWarning(string msg) => Logger.Log(LogLevel.Warning, msg, CurrentPath, $"Line {CurrentLineIndex}: \"{CurrentLine}\"");
    private void LogInformation(string msg) => Logger.Log(LogLevel.Information, msg, CurrentPath, $"Line {CurrentLineIndex}: \"{CurrentLine}\"");
    private void LogVerbose(string msg) => Logger.Log(LogLevel.Verbose, msg, CurrentPath, $"Line {CurrentLineIndex}: \"{CurrentLine}\"");
    private void LogDebug(string msg) => Logger.Log(LogLevel.Debug, msg, CurrentPath, $"Line {CurrentLineIndex}: \"{CurrentLine}\"");
    CueSheet? Sheet { get; set; }
    public char Quotation { get; set; } = '"';
    public Encoding? Encoding { get; set; }

    readonly List<bool> TrackHasZerothIndex = new();
    static public Encoding GetEncoding(string filePath)
    {
        throw new NotImplementedException();
    }
    [MemberNotNull(nameof(CurrentLine), nameof(CurrentPath))]
    private void Reset()
    {
        Sheet = null;
        CurrentLineIndex = 1;
        CurrentPath = "No File";
        CurrentLine = "No line read";
        TrackHasZerothIndex.Clear();
    }
    public CueReader()
    {
        Reset();
    }
    [MemberNotNull(nameof(Sheet))]
    public CueSheet ParseCueSheet(string cuePath)
    {
        Reset();
        CurrentPath = cuePath;
        LogDebug("Loading bytes from file");
        if (!File.Exists(cuePath)) throw new FileNotFoundException($"{cuePath} does not exist");
        var cueFileBytes = File.ReadAllBytes(cuePath);
        return ParseCueSheet(cuePath, cueFileBytes);
    }

    [MemberNotNull(nameof(Sheet))]
    public CueSheet ParseCueSheet(string cuePath, byte[] cueFileBytes)
    {
        Reset();
        CurrentPath = cuePath;
        LogDebug("Creating memory stream");
        using MemoryStream fs = new(cueFileBytes, false);
        return ParseCueSheet(cuePath, fs);
    }

    [MemberNotNull(nameof(Sheet))]
    public CueSheet ParseCueSheet(string cuePath, Stream fs)
    {
        Reset();
        CurrentPath = cuePath;
        LogDebug("Parsing started");
        if (!File.Exists(cuePath))
        {
            LogError("Specified file does not exist");
            throw new FileNotFoundException($"{cuePath} does not exist");
        }

        Sheet = new(cuePath);
        Stopwatch st = Stopwatch.StartNew();
        Encoding encoding;
        if (Encoding is null)
        {
            var tester = new CueEncodingTester(fs,cuePath);
            encoding = tester.DetectCueEncoding();
        }
        else
        {
            encoding = Encoding;
        }
        st.Stop();
        fs.Seek(0, SeekOrigin.Begin);
        using StreamReader strr = new(fs, encoding, false);
        while (strr.ReadLine()?.Trim() is string line)
        {
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
            CurrentLineIndex++;
        }
        for (int i = 0; i < TrackHasZerothIndex.Count; i++)
        {
            Sheet.SetTrackHasZerothIndex(i, TrackHasZerothIndex[i]);
        }
        Sheet.RefreshIndices();
        LogInformation("Finished parsing");
        return Sheet;
    }

    private void ParseTitle(string line)
    {

        string? title = GetValue(line, 5);
        if (title == null)
        {
            LogWarning("Invalid TITLE line");
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
        (string path, string type) = GetFile(line, 5);
        _ = Sheet!.AddFile(path, type);
    }
    private void ParseTrack(string line)
    {

        string num = GetKeyword(line, 6);
        if (!int.TryParse(num, out int number))
        {
            number = Sheet!.LastTrack?.Number + 1 ?? 1;
            LogWarning("Invalid TRACK number. Substituting " + number);
        }
        CueTrack tr = Sheet!.AddTrack(number);

    }
    private void ParsePerformer(string line)
    {

        string? performer = GetValue(line, 10);
        if (performer == null)
        {
            LogWarning("Invalid PERFORMER line");
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

        string? cdt = GetValue(line, 11);
        if (cdt == null)
        {
            LogWarning("Invalid CDTEXT line");
            return;
        }
        Sheet!.SetCdTextFile(cdt);
    }
    private void ParseFlags(string line)
    {

        if (Sheet!.LastTrack is not CueTrack track)
        {
            LogWarning("FLAGS present before any track");
            return;
        }
        TrackFlags flags = TrackFlags.None;
        var parts = line[6..].Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
        string? isrc = GetValue(line, 5);
        if (isrc == null)
        {
            LogWarning("Invalid ISRC line");

            return;
        }
        track.ISRC = isrc;
    }
    private void ParseCatalog(string line)
    {
        string? cata = GetValue(line, 8);
        if (cata == null)
        {
            LogWarning("Invalid CATALOG line");
            return;
        }
        Sheet!.Catalog = cata;

    }
    private void ParseIndex(string line)
    {

        if (Sheet!.LastTrack is not CueTrack track)
        {
            LogWarning("INDEX line present before any track");
            return;
        }

        string number = GetKeyword(line, 6);
        if (!int.TryParse(number, out int num))
        {
            LogError("Incorrect Index number format ");
            throw new FormatException($"Incorrect Index number format at line {CurrentLineIndex}: {line}");
        }
        if (!CueTime.TryParse(line.AsSpan(6 + number.Length + 1), out CueTime cueTime))
        {
            LogError("Incorrect Index format");
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
            LogWarning("GAP line present before any file");
            return;
        }
        if (Sheet!.LastTrack is not CueTrack track)
        {
            LogWarning("GAP line present before any track");
            return;
        }

        string type = GetKeyword(line, 0);
        if (!CueTime.TryParse(line.AsSpan(6 + type.Length + 1), out CueTime cueTime))
        {
            LogError("Incorrect Gap format");
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
                    Sheet.AddRem(field, value);
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
                    Sheet.AddRem(field, value);
                    break;
            }
        }
    }

    /// <summary>
    /// Get the filename and file type. If quotation marks are present, simple iterating algorithm is used. Otherwise Regex is used
    /// </summary>
    /// <param name="s">String to get values from</param>
    /// <returns><see cref="ValueTuple"/> of filename and type</returns>
    [Pure]
    public (string Path, string Type) GetFile(string s, int start)
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
                type = GetSuffix(s[end..]);
                return (path, type);
            }
        }
        string emer = spanny.ToString();
        Match m = EmergencyFile.Value.Match(emer);
        if (m.Success)
        {
            path = m.Groups["PATH"].Value;
            type = m.Groups["TYPE"].Value;
            return (path, type);
        }
        return (emer, "");
    }
    /// <summary>
    /// Gets the value in quotation marks, or trimmed area if no quotations are present
    /// </summary>
    /// <param name="s">String to get value from</param>
    /// <param name="start">Index from which the search will be started</param>
    /// <param name="end">How many characters are skipped at the end</param>
    /// <returns>String if there was text, enclosed in quotations or not; <see cref="null"/> if there was no non-whitespace text, or there were quutations with nothing inside</returns>
    [Pure]
    public string? GetValue(string s, int start, int end = 0)
    {
        ReadOnlySpan<char> spanny = s.AsSpan(start, s.Length - start - end).Trim();
        if (spanny.Length == 0 || spanny.SequenceEqual("\"\""))
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
    [Pure]
    public static string GetKeyword(string s, int startIndex = 0, int maxSearchLength = -1)
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
    [Pure]
    public static string GetSuffix(ReadOnlySpan<char> s)
    {
        s = s.TrimEnd();
        for (int i = s.Length - 1; i >= s.Length / 2; i--)
        {
            if (char.IsWhiteSpace(s[i]))
                return s[i..].ToString();
        }
        return string.Empty;
    }
}