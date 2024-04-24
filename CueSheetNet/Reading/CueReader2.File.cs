using System;
using System.Text.RegularExpressions;
using CueSheetNet.Logging;
using CueSheetNet.Syntax;

namespace CueSheetNet;

public partial class CueReader2
{
    private class File : IChainLink<File>
    {
        public File? Previous { get; set; }
        public File? Next { get; set; }
        public string Path { get; }
        public FileType Type { get; }
        public InfoBag Parent { get; }
        public Chain<Track> Tracks { get; } = [];

        public File(string path, FileType type, InfoBag parent)
        {
            Path = path;
            Type = type;
            Parent = parent;
            Tracks.JoinChainAfter(parent.Files.ChainEnd?.Tracks);
            parent.Files.Add(this);
        }

        public override string ToString()
        {
            return $"{System.IO.Path.GetFileName(Path)}";
        }

        public IEnumerable<File> FollowSince()
        {
            yield return this;
            var i = this.Next;
            while (i is not null)
            {
                yield return i;
                i = i.Next;
            }
        }

        public void GetPromoted()
        {

        }
    }

    /// <summary>
    /// Get the filename and file type. If quotation marks are present, simple iterating algorithm is used. Otherwise Regex is used
    /// </summary>
    /// <param name="s">String to get values from</param>
    /// <returns><see cref="ValueTuple"/> of filename and type</returns>
    private (string Path, string Type) GetFilePath(string s, int start)
    {
        ReadOnlySpan<char> span = s.AsSpan(start).Trim();
        string path,
            type;
        if (span[0] == Quotation)
        {
            int end = -1;
            for (int i = span.Length - 1; i >= 1; i--)
            {
                if (span[i] == Quotation)
                {
                    end = i;
                    break;
                }
            }
            if (end > 0)
            {
                path = span[1..end].ToString();
                type = GetSuffix(s.AsSpan()[end..]);
                return (path, type);
            }
        }
        // Line does not have Quotation, need to use regex
        string emer = span.ToString();
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

    private File ParseFileLine(Line line, InfoBag parent)
    {
        (string path, string type) = GetFilePath(line.Text, 5); // FILE_
        if (!Enum.TryParse<FileType>(type.Trim().ToUpperInvariant(), out FileType typeEnum))
        {
            Logger.LogWarning(
                "Text {type} does not match eny file type - assigning type WAVE",
                type
            );
            typeEnum = FileType.WAVE;
        }
        return new File(path, typeEnum, parent);
    }

    private void ParseFile(IList<KeywordedLine> fileLines, InfoBag data)
    {
        // first line has to be file declaration
        File currentFile = ParseFileLine(fileLines[0].Line,data);

        IList<IList<KeywordedLine>> tracksLines = [[]];
        int trackCount = 0;
        // skip the first line - its FILE
        foreach (var fileLine in fileLines.Skip(1))
        {
            (Keywords key, Line line) = fileLine;
            if (key == Keywords.TRACK)
            {
                trackCount++;
                tracksLines.Add([]);
            }
            tracksLines[trackCount].Add(fileLine);
        }

        if (tracksLines[0].Count != 0)
        {
            if (currentFile.Previous is null)
            {
                throw new InvalidDataException("Sheet contains tracks with no file.");
            }
            if (currentFile.Tracks.ChainStart is null)
            {
                throw new InvalidDataException("No tracks for an EAC-style index.");
            }
            // dangling eac-style track
            currentFile.Previous.Tracks.PromoteLastItemToFollowingChain();
            ParseTrackImpl(tracksLines[0], currentFile.Tracks.ChainStart);
        }
        tracksLines.RemoveAt(0);
        ParseTracks(tracksLines, currentFile);

        if (fileLines[1].Keyword == Keywords.TRACK)
        {
            //parsetrack
        }
        else
        {
            throw new InvalidDataException($"Unexpected line at {fileLines[0]}");
        }
    }

    private void ParseFiles(IList<IList<KeywordedLine>> filesLines, InfoBag data)
    {
        if (filesLines.Count == 0)
        {
            throw new InvalidDataException("CUE sheet has no files.");
        }
        foreach (var fileLines in filesLines)
        {
            ParseFile(fileLines, data);
        }
    }
}