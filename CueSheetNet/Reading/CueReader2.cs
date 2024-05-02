using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using CueSheetNet.Collections;
using CueSheetNet.Extensions;
using CueSheetNet.Internal;
using CueSheetNet.Logging;
using CueSheetNet.Reading;
using CueSheetNet.Syntax;
using CueSheetNet.TextParser;

namespace CueSheetNet;

public partial class CueReader2
{
    public const char DefaultQuotation = '"';
    public char Quotation { get; set; } = DefaultQuotation;

    public CueSheet Read(string filePath)
    {
        var data = new CueSheet(filePath);
        using  FileStream fs = new FileStream(filePath,FileMode.Open);
        //var tester = new CueEncodingTester(fs, Source);
        Encoding encoding=default!;
        using TextReader reader = new StreamReader(fs,encoding, detectEncodingFromByteOrderMarks: false);
        List<Line> lines = [];
        int index=0;
        while (reader.ReadLine() is string line)
        {
            lines.Add((index, line));
            index++;
        }
        Parse(lines, data);
        throw new NotImplementedException();
    }
    private void ParseSheetLines(IList<KeywordedLine> lines, CueSheet data)
    {
        foreach (KeywordedLine kwline in lines)
        {
            (Keywords key, Line line) = kwline;
            // here we can have only oneliners for sheet or general oneliners.
            switch (key)
            {
                case Keywords.PERFORMER:
                    data.Performer = ParsePerformer(line);
                    break;
                case Keywords.REM:
                    data.Remarks.AddNotNull(ParseREM(line));
                    break;
                case Keywords.TITLE:
                    data.Title = ParseTitle(line);
                    break;
                case Keywords.CDTEXTFILE:
                    data.SetCdTextFile (ParseCdTextFile(line));
                    break;
                case Keywords.CATALOG:
                    data.Catalog = ParseCatalog(line);
                    break;
                default:
                    throw new InvalidDataException($"Keyword {key} was not expected before first file.");
            }
        }
    }

    //private void ParseFiles(IList<IList<KeywordedLine>> filesLines, InfoBag data)
    //{
    //    if (filesLines.Count == 0)
    //    {
    //        throw new InvalidDataException("CUE sheet has no files.");
    //    }
    //    File? previousFile = null;
    //    foreach (var fileLines in filesLines)
    //    {
    //        ParseFile(fileLines, data, previousFile);
    //    }
    //}

    //private File ParseFile(IList<KeywordedLine> fileLines, InfoBag data, File? previousFile)
    //{
    //    // first line has to be file declaration
    //    File currentFile = ParseFileLine(fileLines[0].Line);
    //    currentFile.Previous = previousFile;

    //    IList<IList<KeywordedLine>> tracksLines = [[]];
    //    int trackCount = 0;
    //    foreach (var fileLine in fileLines)
    //    {
    //        (Keywords key, Line line) = fileLine;

    //        if (key == Keywords.TRACK)
    //        {
    //            trackCount++;
    //            tracksLines.Add([]);
    //        }
    //        tracksLines[trackCount].Add(fileLine);
    //    }
    //    if (tracksLines[0].Count != 0)
    //    {
    //        // dangling eac-style track
    //    }
    //    tracksLines.RemoveAt(0);

    //    if (fileLines[1].Keyword == Keywords.INDEX)
    //    {
    //        // first line of file is index, we have a dangling eac-style track
    //        Track currentTrack = (currentFile.Previous?.Tracks.ChainEnd) ?? throw new InvalidDataException("Index was specified with no track");
    //        /// parseindex -> add
    //    }
    //    else if (fileLines[1].Keyword == Keywords.TRACK)
    //    {
    //        //parsetrack
    //    }
    //    else
    //    {
    //        throw new InvalidDataException($"Unexpected line at {fileLines[0]}");
    //    }
    //}



    //private File ParseFileLine(Line line)
    //{
    //    (string path, string type) = GetFilePath(line.Text, 5); // FILE_
    //    if (!Enum.TryParse<FileType>(type.Trim().ToUpperInvariant(), out FileType typeEnum))
    //    {
    //        Logger.LogWarning(
    //            "Text {type} does not match eny file type - assigning type WAVE",
    //            type
    //        );
    //        typeEnum = FileType.WAVE;
    //    }
    //    return new File(path, typeEnum);
    //}



    ///// <summary>
    ///// Get the filename and file type. If quotation marks are present, simple iterating algorithm is used. Otherwise Regex is used
    ///// </summary>
    ///// <param name="s">String to get values from</param>
    ///// <returns><see cref="ValueTuple"/> of filename and type</returns>
    //private (string Path, string Type) GetFilePath(string s, int start)
    //{
    //    ReadOnlySpan<char> span = s.AsSpan(start).Trim();
    //    string path,
    //        type;
    //    if (span[0] == Quotation)
    //    {
    //        int end = -1;
    //        for (int i = span.Length - 1; i >= 1; i--)
    //        {
    //            if (span[i] == Quotation)
    //            {
    //                end = i;
    //                break;
    //            }
    //        }
    //        if (end > 0)
    //        {
    //            path = span[1..end].ToString();
    //            type = GetSuffix(s.AsSpan()[end..]);
    //            return (path, type);
    //        }
    //    }
    //    // Line does not have Quotation, need to use regex
    //    string emer = span.ToString();
    //    Match m = NonQuotedFileRegex().Match(emer);
    //    if (m.Success)
    //    {
    //        path = m.Groups["PATH"].Value;
    //        type = m.Groups["TYPE"].Value;
    //        return (path, type);
    //    }
    //    // Could not properly parse Path Type. Return Line as path, no type
    //    return (emer, "");
    //}

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

    private void Parse(ICollection<Line> lines, CueSheet data)
    {
        IList<IList<KeywordedLine>> partLines = [[]]; // list of lists with one list
        int fileCount = 0;
        foreach (var kwline in lines.Select(x => new KeywordedLine(GetKeyword(x), x)))
        {
            (Keywords key, Line line) = kwline;

            if (key == Keywords.FILE)
            {
                fileCount++;
                partLines.Add([]);
            }
            partLines[fileCount].Add(kwline);
        }
        // first list has non-file lines; may be empty
        var sheetLines = partLines[0];
        ParseSheetLines(sheetLines, data);
        // sheet lines were parsed, so we can remove them.
        partLines.RemoveAt(0);
        // now we only have file lines.
        ParseFiles(partLines, data);




        // no file yet

        // we have started the first file


    }

    private string? ParseCatalog(Line line)
    {
        string? cata = GetValue(line.Text, 8); // CATALOG_
        if (cata == null)
        {
            Logger.LogWarning(
                "Invalid CATALOG at line {line}",
                line
            );
        }
        return cata;
    }


    private string? ParseCdTextFile(Line line)
    {
        string? cdt = GetValue(line.Text, 11); // CDTEXTFILE_
        if (cdt == null)
        {
            Logger.LogWarning(
                "Invalid CDTEXT at line {line}", line
            );
        }
        return cdt;
    }

    private string? ParsePerformer(Line line)
    {
        string? performer = GetValue(line.Text, 10); // PERFORMER_
        if (performer == null)
        {
            Logger.LogWarning(
                "Invalid PERFORMER at line {line}", line
            );
        }
        return performer;

    }

    private CueRemark? ParseREM(Line line)
    {
        string field = GetKeyword(line.Text, 4).ToUpperInvariant();
        int valueStart = 4 + field.Length + 1;
        string? value = GetValue(line.Text, valueStart);
        if (value == null)
        {
            return null;
        }
        return field switch
        {
            "DATE" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int d) ? new CueRemark(field, d.ToString(CultureInfo.InvariantCulture)) : null,
            _ => new(field, value)
        };
    }

    private string? ParseTitle(Line line)
    {
        string? title = GetValue(line.Text, 5); // TITLE_
        if (title == null)
        {
            Logger.LogWarning(
                "Invalid TITLE at line {line}", line
            );
        }
        return title;

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
        ReadOnlySpan<char> span = s.AsSpan(start, s.Length - start - end).Trim();
        if (span.Length == 0)
            return null;
        if (span.Length == 2 && span[0] == span[1] && span[0] == Quotation)
            return null;
        if (span[^1] == Quotation && span[0] == Quotation)
            return span[1..^1].ToString();

        return span.ToString();
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
        if (maxSearchLength <= 0)
            maxSearchLength = s.Length;
        maxSearchLength = maxSearchLength.Clamp(0, s.Length - startIndex);
        ReadOnlySpan<char> spanish = s.AsSpan(startIndex, maxSearchLength).TrimStart();
        for (int i = 0; i < spanish.Length; i++)
        {
            if (char.IsWhiteSpace(spanish[i]))
                return spanish[..i].ToString();
        }
        return spanish.ToString();
    }

    private static Keywords GetKeyword(Line line)
    {
        string value = GetKeywordString(line.Text).ToUpperInvariant();
        if (!Enum.TryParse(value, out Keywords key))
        {
            throw new InvalidDataException($"Invalid Cuesheet. Unexpected keyword ${value}");
        }

        return key;
    }

    /// <summary>
    /// Get the first full word from the specified start, stops at whitespace
    /// </summary>
    /// <param name="s">String to get value from</param>
    /// <param name="startIndex">INdex from which the word will be sought</param>
    /// <param name="maxSearchLength">Maximum search length. If exceeded, empty string is returned</param>
    /// <returns>String of characters from <paramref name="startIndex"/> to first whitespace, or empty string if no whitespace has been detected in the first <paramref name="maxSearchLength"/> characters</returns>
    private static string GetKeywordString(string s, int startIndex = 0, int maxSearchLength = -1)
    {
        if (maxSearchLength <= 0)
            maxSearchLength = s.Length;
        maxSearchLength = maxSearchLength.Clamp(0, s.Length - startIndex);
        ReadOnlySpan<char> spanish = s.AsSpan(startIndex, maxSearchLength).TrimStart();
        for (int i = 0; i < spanish.Length; i++)
        {
            if (char.IsWhiteSpace(spanish[i]))
                return spanish[..i].ToString();
        }
        return spanish.ToString();
    }

#if NET7_0_OR_GREATER // GeneratedRegex introduces in NET7
    [GeneratedRegex(@"(?<PATH>\w+)\s+(?<TYPE>\w*)", RegexOptions.Compiled, 500)]
    private static partial Regex NonQuotedFileRegex();
#else
    private static readonly Regex NonQuotedFileRegexImpl =
        new(@"(?<PATH>\w+)\s+(?<TYPE>\w*)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    private static Regex NonQuotedFileRegex() => NonQuotedFileRegexImpl;

#endif
}