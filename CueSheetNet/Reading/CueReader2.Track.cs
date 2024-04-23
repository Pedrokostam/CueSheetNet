using System.Globalization;
using CueSheetNet.Extensions;
using CueSheetNet.Internal;
using CueSheetNet.Logging;
using CueSheetNet.Syntax;

namespace CueSheetNet;

public partial class CueReader2
{
    private class Track : IChainLink<Track>
    {
        public string? Performer { get; set; }
        public string? Title { get; set; }
        public string? ISRC { get; set; }
        public TrackFlags Flags { get; set; }
        public CueTime PreGap { get; set; }
        public CueTime PostGap { get; set; }
        public Track? Previous { get; set; }
        public Track? Next { get; set; }
        public int Number { get; }
        public File ParentFile { get; }
        public Chain<Index> Indexes { get; } = [];
        public List<CueRemark> Remarks { get; } = [];

        /// <summary>
        /// Creates track and adds it as the last track of <paramref name="parent"/>
        /// </summary>
        /// <param name="number"></param>
        /// <param name="parent"></param>
        public Track(int number,File parent)
        {
            Number = number;
            ParentFile = parent;
            Indexes.JoinChainAfter(ParentFile.Tracks.Last?.Indexes);
            ParentFile.Tracks.Add(this);
        }

        public override string ToString()
        {
            return $"{Number}-{Title}-{Path.GetFileName(ParentFile.Path)}";
        }

        public IEnumerable<Track> FollowSince()
        {
            yield return this;
            var i = this.Next;
            while (i is not null)
            {
                yield return i;
                i = i.Next;
            }
        }
    }

    private void ParseTracks(IList<IList<KeywordedLine>> tracksLines, File currentFile)
    {
        foreach (IList<KeywordedLine> trackLines in tracksLines)
        {
            if (trackLines[0].Keyword != Keywords.TRACK)
            {
                throw new InvalidDataException("Expected a TRACK keyword.");
            }
            ParseTrack(trackLines,currentFile);

        }
    }

    private void ParseTrack(IList<KeywordedLine> trackLines, File currentFile)
    {
        if (trackLines[0].Keyword != Keywords.TRACK)
        {
            throw new InvalidDataException("Expected a TRACK keyword.");
        }
        string num = GetKeyword(trackLines[0].Line.Text, 6); // TRACK_
        if (!int.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
        {
            number = currentFile.Tracks.Last?.Number + 1 ?? 1;
            Logger.LogWarning("Invalid TRACK number at line {line}. Substituting {Substitute number:d2}", trackLines[0].Line, number);
        }
        //string type = GetKeyword(trackLines[0].Line.Text, 6 + 1 + num.Length);
        Track currentTrack = new Track(number,currentFile);
        for (int i = 1; i < trackLines.Count; i++)
        {
            KeywordedLine kwline = trackLines[i];
            Line line = kwline.Line;
            switch (kwline.Keyword)
            {
                case Keywords.REM:
                    currentTrack.Remarks.AddNotNull(ParseREM(line));
                    break;
                case Keywords.PERFORMER:
                    currentTrack.Performer = ParsePerformer(line);
                    break;
                case Keywords.TITLE:
                    currentTrack.Title = ParseTitle(line);
                    break;
                case Keywords.FLAGS:
                    currentTrack.Flags = ParseFlags(line);
                    break;
                case Keywords.INDEX:
                    ParseIndex(line,currentTrack);
                    break;
                case Keywords.POSTGAP:
                case Keywords.PREGAP:
                    ParseGap(line, kwline.Keyword.ToString(), currentTrack);
                    break;
                case Keywords.ISRC:
                    currentTrack.ISRC = ParseISRC(line);
                    break;
            }
        }

    }
    private TrackFlags ParseFlags(Line line)
    {
       
        TrackFlags flags = TrackFlags.None;
        string[] parts = line.Text[6..] // FLAGS_
            .Replace("\"", "", StringComparison.Ordinal)
            .Replace("'", "", StringComparison.Ordinal)
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
        return flags;
    }

   /// <summary>
   /// Parse gap and adds it to track
   /// </summary>
   /// <param name="line"></param>
   /// <param name="gapType"></param>
   /// <param name="track"></param>
   /// <exception cref="FormatException"></exception>
    private void ParseGap(Line line, string gapType,Track track)
    {
        if (!CueTime.TryParse(line.Text.AsSpan(6 + gapType.Length + 1),null, out CueTime cueTime))
        {
            //Logger.LogError("Incorrect Gap format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException($"Incorrect Gap format at line {line}");
        }
        // the whole line is guarentedd to be uppercase
        if (gapType.StartsWith("PRE", StringComparison.Ordinal))
            track.PreGap = cueTime;
        else
            track.PostGap = cueTime;
    }

    private string? ParseISRC(Line line)
    {
        string? isrc = GetValue(line.Text, 5); // ISRC_
        if (isrc == null)
        {
            Logger.LogWarning(
                "Invalid ISRC at line {Line}",
               line
            );

        }
        return isrc;
    }
}