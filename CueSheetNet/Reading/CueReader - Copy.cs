using CueSheetNet.Components;
using CueSheetNet.Extensions;
using CueSheetNet.Reading;
using CueSheetNet.Syntax;
using CueSheetNet.TextParser;
using Metalama.Framework;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CueSheetNet;

public sealed partial class CueReader2 : ISourcelessCueReader
{
    #region Factories
    /// <remarks>
    /// To learn more check the more fine-grained methods:
    /// <list type="table">
    /// <item><see cref="WithQuotation(char)"/></item>
    /// <item><see cref="WithEncoding(Encoding?)"/></item>
    /// <item><see cref="WithQualifiedPaths(bool)"/></item>
    /// </list>
    /// </remarks>
    /// <inheritdoc cref="ISourcelessCueReader.WithSettings(CueReaderSettings)"/>
    public CueReader2 WithSettings(CueReaderSettings settings)
    {
        Settings = settings;
        return this;
    }

    /// <inheritdoc cref="ISourcelessCueReader.WithQuotation(char)"/>
    public CueReader2 WithQuotation(char quote)
    {
        Settings = Settings with { Quotation = quote };
        return this;
    }

    /// <inheritdoc cref="ISourcelessCueReader.WithEncoding(Encoding?)"/>
    public CueReader2 WithEncoding(Encoding? encoding)
    {
        Settings = Settings with { Encoding = encoding };
        return this;
    }

    /// <inheritdoc cref="ISourcelessCueReader.WithQualifiedPaths(bool)"/>
    public CueReader2 WithQualifiedPaths(bool qualified)
    {
        Settings = Settings with { KeepQualified = qualified };
        return this;
    }

    ///<remarks></remarks>
    /// <inheritdoc cref="ISourcelessCueReader.FromString(string)"/>
    public static CueReader2 CreateFromString(string content)
    {
        var reader = new CueReader2(null);
        return reader.FromString(content);
    }
    /// <returns>The same instance of CueReader, reset and with the source set to the new value.</returns>
    /// <inheritdoc cref="ISourcelessCueReader.FromString(string)"/>
    public CueReader2 FromString(string content)
    {
        Reset();
        Source = new CueSource(content, SourceType.String);
        return this;
    }

    ///<remarks></remarks>
    /// <inheritdoc cref="ISourcelessCueReader.FromFile(FileInfo)"/>
    public static CueReader2 CreateFromFile(FileInfo fileInfo)
    {
        var reader = new CueReader2(null);
        return reader.FromFile(fileInfo);
    }
    /// <returns>The same instance of CueReader, reset and with the source set to the new value.</returns>
    /// <inheritdoc cref="ISourcelessCueReader.FromFile(FileInfo)"/>
    public CueReader2 FromFile(FileInfo file)
    {
        Reset();
        Source = new CueSource(file.FullName, SourceType.File);
        return this;
    }
    ///<remarks></remarks>
    /// <inheritdoc cref="ISourcelessCueReader.FromFile(string)"/>
    public static CueReader2 CreateFromFile(string filepath)
    {
        var reader = new CueReader2(null);
        return reader.FromFile(filepath);
    }
    /// <returns>The same instance of CueReader, reset and with the source set to the new value.</returns>
    /// <inheritdoc cref="ISourcelessCueReader.FromFile(string)"/>
    public CueReader2 FromFile(string filepath)
    {
        Reset();
        Source = new CueSource(filepath, SourceType.File);
        return this;
    }

    ///<remarks></remarks>
    /// <inheritdoc cref="ISourcelessCueReader.FromBytes(IList{byte})"/>
    public static CueReader2 CreateFromBytes(IList<byte> bytes)
    {
        var reader = new CueReader2(null);
        return reader.FromBytes(bytes);
    }
    /// <returns>The same instance of CueReader, reset and with the source set to the new value.</returns>
    /// <inheritdoc cref="ISourcelessCueReader.FromBytes(IList{byte})"/>
    public CueReader2 FromBytes(IList<byte> bytes)
    {
        Reset();
        Source = new CueSource(bytes, SourceType.Bytes);
        return this;
    }
    ///<remarks></remarks>
    /// <inheritdoc cref="ISourcelessCueReader.FromStream(Stream)"/>
    public static CueReader2 CreateFromStream(Stream stream)
    {
        var reader = new CueReader2(null);
        return reader.FromStream(stream);
    }
    /// <returns>The same instance of CueReader, reset and with the source set to the new value.</returns>
    /// <inheritdoc cref="ISourcelessCueReader.FromStream(Stream)"/>
    public CueReader2 FromStream(Stream stream)
    {
        Reset();
        Source = new CueSource(stream, SourceType.Stream);
        return this;
    }
    ISourcelessCueReader ISourcelessCueReader.WithSettings(CueReaderSettings settings)
    {
        return WithSettings(settings);
    }

    ISourcelessCueReader ISourcelessCueReader.WithQuotation(char quote)
    {
        return WithQuotation(quote);
    }

    ISourcelessCueReader ISourcelessCueReader.WithEncoding(Encoding? encoding)
    {
        return WithEncoding(encoding);
    }

    ISourcelessCueReader ISourcelessCueReader.WithQualifiedPaths(bool qualified)
    {
        return WithQualifiedPaths(qualified);
    }
    #endregion

    [AllowNull]
    public CueReaderSettings Settings
    {
        get; set
        {
            if (value.Quotation == default)
            {
                field = value with { Quotation = CueReaderSettings.DefaultQuotation };
            }
            else
            {
                field = value;
            }
        }
    }
    internal ILogger Loggerio => Loggingq.ParsingLogger;

    private static readonly CultureInfo InvCul = CultureInfo.InvariantCulture;

    public CueSource Source { get; private set; }

    [AllowNull]
    CueSheet Sheet { get; set; }

    [MemberNotNullWhen(returnValue: true, nameof(LastTrack))]
    private bool SheetHasTracks => Sheet.Data.Count > 0;

    [MemberNotNullWhen(returnValue: true, nameof(LastFile))]

    private bool SheetHasFiles => Sheet.Data.Count > 0;

    private Encoding? UsedEncoding { get; set; }

    public ISourcelessCueReader Reset()
    {
        Sheet = null;
        CurrentLineIndex = -1;
        UsedEncoding = null;
        Source = default;
        return (ISourcelessCueReader)this;
    }

    /// <summary>
    /// Parses the sheet from the given source.
    /// </summary>
    /// <remarks>
    /// The reader can be reused after the method. Set the new source and it will automatically call <see cref="Reset"/>.
    /// </remarks>
    /// <returns>The new parsed cue sheet.</returns>
    public CueSheet Parse()
    {
        var start = Stopwatch.GetTimestamp();
        Loggerio.StartedParsing(Source);
        Exception? error = null;
        try
        {
            if (!Source.Initialized)
            {
                throw new InvalidOperationException("You must set a source (From* methods) to be able to parse");
            }
            if (Source.Type == SourceType.None || Source.Data is null)
            {
                throw new InvalidOperationException("Cannot parse when Source is \"None\"");
            }
            if (Source.Type == SourceType.String)
            {
                return ParseString((string)Source.Data);
            }
            IList<byte> bytes = Source.Type switch
            {
                SourceType.File => File.ReadAllBytes((string)Source.Data),
                SourceType.Stream => ((Stream)Source.Data).GetBytes(),
                SourceType.Bytes => (IList<byte>)Source.Data,
                SourceType.None => throw new UnreachableException("None should have already been dealt with"),
                SourceType.String => throw new UnreachableException("String should have already been dealt with"),
                _ => throw new InvalidEnumArgumentException(nameof(Source), (int)Source.Type, typeof(SourceType))
            };
            return bytes switch
            {
                byte[] arr => ParseBytes(arr.AsSpan()),
                List<byte> list => ParseBytes(CollectionsMarshal.AsSpan(list)),
                _ => ParseBytes(bytes.ToArray()),
            };
        }
        catch (Exception e)
        {
            error = e;
            throw;
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            Loggerio.EndedParsing(Source, elapsed.TotalMilliseconds, error);
        }
    }
    /// <summary>
    /// Currently parsed file
    /// </summary>
    private CueDataFile? _currentFile = null;
    /// <summary>
    /// Currently parsed file OR the last file added to the sheet
    /// </summary>
    private CueDataFile? LastFile => _currentFile ?? (Sheet.Data.Count > 0 ? Sheet.Data[^1].File : null);
    /// <summary>
    /// Currently parsed track
    /// </summary>
    private CueTrack? _currentTrack = null;
    /// <summary>
    /// Currently parsed track OR the last track added to the sheet
    /// </summary>
    private CueTrack? LastTrack => _currentTrack ?? (Sheet.Data.Count > 0 ? Sheet.Data[^1].Track : null);

    public int CurrentLineIndex { get; private set; }
    CueReaderSettings ISourcelessCueReader.Settings { get; }

    private CueReader2(CueReaderSettings? settings)
    {
        Settings = settings ?? new();
        Reset();
    }
    private static UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    [TraceExecutionTime]
    private CueSheet ParseBytes(ReadOnlySpan<byte> data)
    {
        var fileInfo = Source.Type switch
        {
            SourceType.File => new FileInfo((string)Source.Data!),
            _ => null
        };
        Sheet = new(fileInfo);
        Span<char> text = stackalloc char[data.Length];

        if (Settings.Encoding is null)
        {
            var tester = new CueEncodingTester2(Source);
            UsedEncoding = tester.DetectCueEncoding(data);
            if (UsedEncoding is null)
            {
                try
                {
                    Utf8Strict.GetChars(data, text);
                }
                catch (DecoderFallbackException)
                {
                    CueEncodingTester2.Win1252.GetChars(data, text);
                    UsedEncoding = CueEncodingTester2.Win1252;
                }
            }
            //Logger.LogInformation(
            //    "Detected {Encoding.EncodingName} encoding in {Time}ms",
            //    Settings.Encoding,
            //    st.ElapsedMilliseconds
            //);
        }
        else
        {
            UsedEncoding = Settings.Encoding;
            UsedEncoding.GetChars(data, text);
        }
        return ParseString(text);
    }
    [TraceExecutionTime("Parsing Cue Sheet", 2)]
    private CueSheet ParseString(ReadOnlySpan<char> txtRead)
    {
        foreach (var lineRaw in txtRead.EnumerateLines())
        {
            var line = lineRaw.Trim();
            CurrentLineIndex++;
            if (line.Length == 0)
            {
                continue;
            }
            string value = GetKeyword(line).ToUpperInvariant();
            if (!Enum.TryParse(value, out Keywords key))
            {
                throw new InvalidDataException("Invalid Cuesheet");
            }
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
        Sheet.SourceEncoding = UsedEncoding;
        return Sheet;
    }

    [TraceExecutionTime]
    private void ParseTitle(ReadOnlySpan<char> line)
    {
        string? title = GetValue(line, 5); // TITLE_
        if (title == null)
        {
            Loggerio.InvalidKeyword("TITLE", CurrentLineIndex, line.ToString());
            return;
        }

        if (LastTrack is CueTrack lastTrack)
        {
            lastTrack.Title = title;
        }
        else
        {
            Sheet.Title = title;
        }
    }

    [TraceExecutionTime]
    private void ParseFile(ReadOnlySpan<char> line)
    {
        (string path, string type) = GetFile(line, 5); // FILE_
        var fileType = FileType.FromString(type);
        //if (!Enum.TryParse<FileType>(type.Trim().ToUpperInvariant(), out FileType typeEnum))
        //{
        //    Logger.LogWarning(
        //        "Text {type} does not match eny file type - assigning type WAVE",
        //        type
        //    );
        //    typeEnum = FileType.WAVE;
        //}
        _currentFile = new CueDataFile(path, Sheet, fileType, Settings.KeepQualified);
    }
    private int CountTracks()
    {
        int i = 0;
        CueTrack? last = null;
        foreach (var t in Sheet.Data)
        {
            if (!ReferenceEquals(t.Track, last))
            {
                i++;
                last = t.Track;
            }
        }
        return i;
    }

    [TraceExecutionTime]
    private void ParseTrack(ReadOnlySpan<char> line)
    {
        string num = GetKeyword(line, 6); // TRACK_
        int? desired = null;
        if (!int.TryParse(num, NumberStyles.Integer, InvCul, out int number))
        {
            Loggerio.InvalidKeyword("TRACK", CurrentLineIndex, line.ToString());
        }
        else
        {
            desired = number;
        }
        string type = GetKeyword(line, 6 + 1 + num.Length);
        int absPos = CountTracks() + 1;
        int gap = desired switch
        {
            not null when desired.Value > absPos => desired.Value - absPos,
            _ => 0,
        };
        _currentTrack = new CueTrack(type, Sheet) { Gap = gap };
    }

    [TraceExecutionTime]
    private void ParsePerformer(ReadOnlySpan<char> line)
    {
        string? performer = GetValue(line, 10); // PERFORMER_
        if (performer == null)
        {
            Loggerio.InvalidKeyword("PEFORMER", CurrentLineIndex, line.ToString());
            return;
        }

        if (LastTrack is CueTrack track)
        {
            track.Performer = performer;
        }
        else
        {
            Sheet.Performer = performer;
        }
    }

    [TraceExecutionTime]
    private void ParseCdTextFile(ReadOnlySpan<char> line)
    {
        string? cdt = GetValue(line, 11); // CDTEXTFILE_
        if (cdt == null)
        {
            Loggerio.InvalidKeyword("CDTEXT", CurrentLineIndex, line.ToString());
            return;
        }
        Sheet.CdTextFile = new CueExtraFile(cdt, Sheet, Settings.KeepQualified);
    }

    [TraceExecutionTime]
    private void ParseFlags(ReadOnlySpan<char> line)
    {
        if (LastTrack is not CueTrack track)
        {
            Loggerio.InvalidKeyword("FLAGS", CurrentLineIndex, line.ToString());
            return;
        }
        TrackFlags flags = TrackFlags.None;
        Span<Range> ranges = stackalloc Range[8];
        line = line[6..];// FLAGS_
        line.Trim("\"'")
            .Split(ranges, ' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var range in ranges)
        {
            var part = line[range];
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
    [TraceExecutionTime]
    private void ParseISRC(ReadOnlySpan<char> line)
    {
        if (LastTrack is not CueTrack track)
            return;
        string? isrc = GetValue(line, 5); // ISRC_
        if (isrc == null)
        {
            Loggerio.InvalidKeyword("ISRC", CurrentLineIndex, line.ToString());
            return;
        }
        track.Isrc = isrc;
    }

    [TraceExecutionTime]
    private void ParseCatalog(ReadOnlySpan<char> line)
    {
        string? cata = GetValue(line, 8); // CATALOG_
        if (cata == null)
        {
            Loggerio.InvalidKeyword("CATALOG", CurrentLineIndex, line.ToString());
            return;
        }
        Sheet.Catalog = cata;
    }

    [TraceExecutionTime]
    private void ParseIndex(ReadOnlySpan<char> line)
    {
        if (LastTrack is null || LastFile is null)
        {
            if (Settings.IgnoreIncorrectPlacement)
            {
                Loggerio.PrematureIndex(CurrentLineIndex, line.ToString());
                return;
            }
            throw new NotImplementedException("index before track");
        }
        string number = GetKeyword(line, 6); // INDEX_
        if (!int.TryParse(number, NumberStyles.Integer, InvCul, out int num))
        {
            //Logger.LogError("Incorrect Index number format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException(
                $"Incorrect Position number format at line {CurrentLineIndex}: {line}"
            );
        }
        if (!CueTime.TryParse(line.Slice(6 + number.Length + 1), InvCul, out CueTime cueTime))
        {
            //Logger.LogError("Incorrect Index format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException($"Incorrect Position format at line {CurrentLineIndex}: {line}");
        }
        var index = new CueIndex(num, cueTime, Sheet);
        Sheet.AddIndex(index, LastTrack, LastFile);
        // reset both - until a new track/file comes along we should use the last added stuff in Data
        _currentFile = null;
        _currentTrack = null;
    }

    [TraceExecutionTime]
    private void ParseGap(ReadOnlySpan<char> line, string gapType)
    {
        if (LastTrack is null)
        {
            if (Settings.IgnoreIncorrectPlacement)
            {
                Loggerio.PrematureGap(CurrentLineIndex, line.ToString());
                return;
            }
            throw new NotImplementedException("gap before track/file");
        }

        string type = GetKeyword(line, 0);
        if (!CueTime.TryParse(line.Slice(6 + type.Length + 1), InvCul, out CueTime cueTime))
        {
            //Logger.LogError("Incorrect Gap format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException($"Incorrect Gap format at line {CurrentLineIndex}: {line}");
        }
        // the whole line is guarentedd to be uppercase
        if (gapType.StartsWith("PRE", StringComparison.Ordinal))
            LastTrack.PreGap = cueTime;
        else
            LastTrack.PostGap = cueTime;
    }
    [TraceExecutionTime]
    private void ParseREM(ReadOnlySpan<char> line)
    {
        string firstWord = GetKeyword(line, 4).ToUpperInvariant();
        int valueStart = 4 + firstWord.Length + 1;
        string? rest = GetValue(line, valueStart);
        IRemarkable remmy = SheetHasTracks ? LastTrack : Sheet;
        if (rest is null)
        {
            // we only have one word and maybe some spaces - it cant be any common rem field
            // add it as legacy - add the whole line after REM
            remmy.Remarks.Add(line[4..].ToString());
        }
        else
        {
            remmy.Remarks.Add(firstWord, rest);
        }
    }

    /// <summary>
    /// Get the filename and file type. If quotation marks are present, simple iterating algorithm is used. Otherwise Regex is used
    /// </summary>
    /// <param name="s">String to get values from</param>
    /// <returns><see cref="ValueTuple"/> of filename and type</returns>
    private (string Path, string Type) GetFile(ReadOnlySpan<char> s, int start)
    {
        ReadOnlySpan<char> spanny = s[start..].Trim();
        string path;
        string type;
        if (spanny[0] == Settings.Quotation)
        {
            int end = -1;
            for (int i = spanny.Length - 1; i >= 1; i--)
            {
                if (spanny[i] == Settings.Quotation)
                {
                    end = i;
                    break;
                }
            }
            if (end > 0)
            {
                path = spanny[1..end].ToString(); // 1 to skip quotation
                type = GetSuffix(spanny[end..]);
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
        // Could not properly parse Path Type. Return Line as path, no type
        return (emer, "");
    }

    /// <summary>
    /// Gets the value in quotation marks, or trimmed area if no quotations are present
    /// </summary>
    /// <param name="s">String to get value from</param>
    /// <param name="start">Index from which the search will be started</param>
    /// <param name="end">How many characters are skipped at the end</param>
    /// <returns>String if there was text, enclosed in quotations or not; <see cref="null"/> if there was no non-whitespace text, or there were quutations with nothing inside</returns>
    private string? GetValue(ReadOnlySpan<char> s, int start, int end = 0) => GetValue(s, start, out _, end);
    /// <inheritdoc cref="GetValue(ReadOnlySpan<char>, int, int)"/>
    /// <param name="wasQuoted">Whether the return string was surrouned by quotation marks</param>
    private string? GetValue(ReadOnlySpan<char> s, int start, out bool wasQuoted, int end = 0)
    {
        ReadOnlySpan<char> spanny = s.Slice(start, s.Length - start - end).Trim();
        if (spanny.Length == 0)
        {
            wasQuoted = false;
            return null;
        }

        if (spanny.Length == 2 && spanny[0] == spanny[1] && spanny[0] == Settings.Quotation)
        {
            wasQuoted = true;
            return null;
        }

        if (spanny.Length > 2 && spanny[^1] == Settings.Quotation && spanny[0] == Settings.Quotation)
        {
            wasQuoted = true;
            return spanny[1..^1].ToString();
        }
        wasQuoted = false;
        return spanny.ToString();
    }

    /// <summary>
    /// Get the first full word from the specified start, stops at whitespace
    /// </summary>
    /// <param name="s">String to get value from</param>
    /// <param name="startIndex">Index from which the word will be sought</param>
    /// <param name="maxSearchLength">Maximum search length. If exceeded, empty string is returned</param>
    /// <returns>String of characters from <paramref name="startIndex"/> to first whitespace, or empty string if no whitespace has been detected in the first <paramref name="maxSearchLength"/> characters</returns>
    internal static string GetKeyword(ReadOnlySpan<char> s, int startIndex = 0, int maxSearchLength = -1)
    {
        if (maxSearchLength <= 0)
            maxSearchLength = s.Length;
        maxSearchLength = maxSearchLength.Clamp(0, s.Length - startIndex);
        ReadOnlySpan<char> spanish = s.Slice(startIndex, maxSearchLength).TrimStart();
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
        for (int i = s.Length - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(s[i]))
                return s[i..].Trim().ToString();
        }
        return string.Empty;
    }

    [GeneratedRegex(@"(?<PATH>\w+)\s+(?<TYPE>\w*)", RegexOptions.Compiled, 500)]
    private static partial Regex NonQuotedFileRegex();


}
