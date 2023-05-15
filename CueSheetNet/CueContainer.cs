using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CueSheetNet;

[DebuggerDisplay("Files: {Files.Count}, Track: {Tracks.Count}, Indexes: {Indexes.Count}")]
internal class CueContainer
{
    private CueSheet ParentSheet { get; }
    public List<CueFile> Files { get; } = new();
    public List<CueTrack> Tracks { get; } = new();
    public List<CueIndexImpl> Indexes { get; } = new();
    public void RefreshIndexIndices(int startFromFile = 0)
    {
        if (Files.Count <= startFromFile) return;
        int fileIndexStart = Indexes.FindIndex(x => x.File == Files[startFromFile]);
        int len = Indexes.Count - fileIndexStart;
        CueTrack? activeTrack = null;
        int numbering = 0;
        for (int i = fileIndexStart; i < len; i++)
        {
            CueIndexImpl currentIndex = Indexes[i];
            if (currentIndex.Track != activeTrack)
            {
                activeTrack = currentIndex.Track;
                numbering = activeTrack.HasZerothIndex ? 0 : 1;
            }
            currentIndex.Number = numbering;
            currentIndex.Index = i;
            numbering++;
        }
    }
    public void RefreshTracksIndices(int startFrom = 0)
    {
        if (Tracks.Count <= startFrom) return;
        int len = Tracks.Count - startFrom;
        for (int i = startFrom; i < len; i++)
        {
            Tracks[i].Index = i;
        }
    }
    public void RefreshFileIndices(int startFrom = 0)
    {
        if (Files.Count <= startFrom) return;
        int len = Files.Count - startFrom;
        for (int i = startFrom; i < len; i++)
        {
            Files[i].Index = i;
        }
    }
    public CueContainer(CueSheet cueSheet)
    {
        ParentSheet = cueSheet;
    }

    public CueFile AddFile(string filePath, string type)
    {
        if (string.IsNullOrEmpty(type)) type = "WAVE";
        CueFile cf = new(ParentSheet, filePath, type)
        {
            Index = Files.Count
        };
        Files.Add(cf);
        return cf;
    }
    public CueFile InsertFile(int insertionIndex, string filePath, string type)
    {
        CueFile cf = new(ParentSheet, filePath, type);
        Files.Insert(insertionIndex, cf);
        RefreshFileIndices(insertionIndex);
        return cf;
    }
    public CueTrack AddTrack(int parsedIndex, int fileIndex = -1)
    {
        if (Files.Count == 0) throw new InvalidOperationException("Cannot add track without any file");
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        CueTrack cf = new(Files[fileIndex])
        {
            Index = Tracks.Count == 0 ? 0 : Tracks[^1].Index + 1
        };
        cf.Offset = parsedIndex - cf.Number;
        Tracks.Add(cf);
        return cf;
    }
    public CueTrack InsertTrack(int insertionIndex, int parsedIndex, int fileIndex = -1)
    {
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        CueTrack cf = new(Files[fileIndex]);
        Tracks.Insert(insertionIndex, cf);
        RefreshIndexIndices(insertionIndex);
        cf.Offset = cf.Number - parsedIndex;
        return cf;
    }
    public CueIndexImpl AddIndex(CueTime time, int fileIndex = -1, int trackIndex = -1)
    {
        if (Tracks.Count == 0) throw new InvalidOperationException("Cannot add index without any tracks");
        if (time < CueTime.Zero)
            throw new ArgumentOutOfRangeException("Cannot add negative index");
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        if (trackIndex < 0) trackIndex = Tracks.Count - 1;
        CueTrack track = Tracks[trackIndex];
        CueFile file = Files[fileIndex];
        if (track.ParentFile != file) throw new InvalidOperationException("Specified track does not belong to specified file");
        if (file.Meta?.CueDuration is CueTime maxTime && time > maxTime)
            throw new ArgumentOutOfRangeException("Specified time occurs after the file ends");

        //No indices at all
        if (Indexes.Count == 0)
        {
            CueIndexImpl pioneer = new CueIndexImpl(track, file) { Time = time, Number = 1 };
            Indexes.Add(pioneer);
            return pioneer;
        }
        (int Start, int End) fileIndices = GetCueIndicesOfFile_Range(fileIndex);
        //No indices in selected file
        if (fileIndices.Start == fileIndices.End)
        {
            return AddIndex_NoIndexInTrack(time, file, track);
        }
        //Go through all indices of file and find the immediate successor
        for (int i = fileIndices.Start; i < fileIndices.End; i++)
        {
            CueIndexImpl curr = Indexes[i];
            if (curr.Time == time) throw new ArgumentException("Index with specified time already exists in the file");
            if (curr.Time > time)
            {
                CueIndexImpl inserted = new(track, file) { Time = time };
                Indexes.Insert(i, inserted);
                RefreshIndexIndices(i);
                return inserted;
            }
        }
        //Found no successors
        CueIndexImpl endTime = Indexes[fileIndices.End - 1];
        CueIndexImpl insertedEnd = new(track, file) { Time = time, Number = endTime.Number + 1 };
        Indexes.Insert(fileIndices.End, insertedEnd);
        RefreshTracksIndices(fileIndices.End + 1);
        return insertedEnd;
    }
    private CueIndexImpl AddIndex_NoIndexInTrack(CueTime time, CueFile file, CueTrack lastTrack)
    {
        CueIndexImpl lastIndex = Indexes[^1];
        (int Start, int End) trackIndices = GetCueIndicesOfTrack_Range(lastTrack.Index, true);
        // track has no indices, and the previous file has no tracks - move track to current file
        int length = trackIndices.End - trackIndices.Start;
        if (length == 0)
        {
            lastTrack.ParentFile = file;
            CueIndexImpl insertedNoPrev = new(lastTrack, file) { Time = time, Number = 1 };
            Indexes.Add(insertedNoPrev);
            return insertedNoPrev;
        }
        //there are multiple indices for the track in the previous file
        if (length > 1) throw new InvalidOperationException("Track cannot be split due to having more than one index in the previous file");
        // track has 1 index in previous time, time to split
        lastTrack.ParentFile = file;
        lastTrack.HasZerothIndex = true;
        CueIndexImpl insertedSplit = new(lastTrack, file) { Time = time, Number = 1 };
        Indexes.Add(insertedSplit);
        return insertedSplit;
    }
    internal void CloneFrom(CueContainer donor)
    {
        foreach (CueFile file in donor.Files)
        {
            Files.Add(file.ClonePartial(ParentSheet));
            foreach (CueTrack track in donor.GetCueTracksOfFile(file.Index))
            {
                Tracks.Add(track.ClonePartial(Files[^1]));
                //var t = donor.GetCueIndicesOfTrack(track.Index);
                foreach (CueIndexImpl ci in donor.GetCueIndicesOfTrack(track.Index))
                {
                    Indexes.Add(ci.ClonePartial(Tracks[^1], Files[^1]));
                }
            }
        }
    }
    internal (int Start, int End) GetCueTracksOfFile_Range(int fileIndex = -1)
    {
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        CueFile file = Files[fileIndex];
        int start = -1;
        int count = 0;
        for (int i = 0; i < Tracks.Count; i++)
        {
            if (Tracks[i].ParentFile == file)
            {
                count++;
                if (start < 0)
                    start = i;
            }
            else if (start >= 0) break;
        }
        return (start, start + count);
    }
    internal IEnumerable<CueTrack> GetCueTracksOfFile(int fileIndex = -1)
    {
        (int Start, int End) = GetCueTracksOfFile_Range(fileIndex);
        return Tracks.Skip(Start).Take(End - Start);
    }
    internal IEnumerable<CueIndexImpl> GetCueIndicesOfTrack(int fileIndex = -1)
    {
        (int Start, int End) = GetCueIndicesOfTrack_Range(fileIndex);
        return Indexes.Skip(Start).Take(End - Start);
    }
    internal (int Start, int End) GetCueIndicesOfFile_Range(int fileIndex = -1)
    {
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        CueFile file = Files[fileIndex];
        int start = -1;
        int count = 0;
        for (int i = 0; i < Indexes.Count; i++)
        {
            if (Indexes[i].File == file)
            {
                count++;
                if (start < 0)
                    start = i;
            }
            else if (start >= 0) break;
        }
        return (start, start + count);
    }
    internal (int Start, int End) GetCueIndicesOfTrack_Range(int trackIndex = -1, bool includeDangling = false)
    {
        if (trackIndex < 0) trackIndex = Tracks.Count - 1;
        CueTrack track = Tracks[trackIndex];
        int start = -1;
        int count = 0;
        for (int i = 0; i < Indexes.Count; i++)
        {
            bool condition = includeDangling ? Indexes[i].Track == track : Indexes[i].Track == track && Indexes[i].File == track.ParentFile;
            if (condition)
            {
                count++;
                if (start < 0)
                    start = i;
            }
            else if (start >= 0) break;
        }
        return (start, start + count);
    }
}