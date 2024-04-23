﻿using System.Diagnostics;
using CueSheetNet.Internal;

namespace CueSheetNet.Collections;

[DebuggerDisplay("Files: {Files.Count}, Track: {Tracks.Count}, Indexes: {Indexes.Count}")]
internal sealed class CueContainer
{
    public bool ParsingMode { get; set; }
    internal CueSheet ParentSheet { get; }
    //public List<CueDataFile> Files { get; } = [];
    public CueFileCollection Files { get; }
    public CueTrackCollection Tracks { get; }
    public List<CueIndexImpl> Indexes { get; } = [];

    public CueContainer(CueSheet cueSheet)
    {
        ParentSheet = cueSheet;
        Files = new(this);
        Tracks=new(this);
    }

    private void RefreshIndexIndices(int startFromFile = 0)
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

    public void Refresh()
    {
        RefreshIndexIndices();
        Tracks.Refresh();
        Files.Refresh();
    }

    public CueIndexImpl AddIndex(CueTime time, int fileIndex = -1, int trackIndex = -1)
    {
        if (Tracks.Count == 0) throw new InvalidOperationException("Cannot add index without any tracks");
        if (time < CueTime.Zero)
            throw new ArgumentOutOfRangeException(nameof(time), "Cannot add negative time index");
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        if (trackIndex < 0) trackIndex = Tracks.Count - 1;
        CueTrack track = Tracks[trackIndex];
        CueDataFile file = Files[fileIndex];
        if (track.ParentFile != file) throw new InvalidOperationException("Specified track does not belong to specified file");
        if (!ParsingMode && file.Meta?.CueDuration is CueTime maxTime && time > maxTime)
            throw new ArgumentOutOfRangeException(nameof(time), "Specified time occurs after the file ends");

        //No indices at all
        if (Indexes.Count == 0)
        {
            CueIndexImpl pioneer = new(track, file) { Time = time, Number = 1 };
            Indexes.Add(pioneer);
            return pioneer;
        }
        (int Start, int End) = GetCueIndicesOfFile_Range(fileIndex);
        //No indices in selected file
        if (Start == End)
        {
            return AddIndex_NoIndexInTrack(time, file, track);
        }
        //Go through all indices of file and find the immediate successor
        for (int i = Start; i < End; i++)
        {
            CueIndexImpl curr = Indexes[i];
            if (curr.Time == time && !ParsingMode) throw new ArgumentException("Index with specified time already exists in the file");
            if (curr.Time > time)
            {
                CueIndexImpl inserted = new(track, file) { Time = time };
                Indexes.Insert(i, inserted);
                RefreshIndexIndices(i);
                return inserted;
            }
        }
        //Found no successors
        CueIndexImpl endTime = Indexes[End - 1];
        CueIndexImpl insertedEnd = new(track, file) { Time = time, Number = endTime.Number + 1 };
        Indexes.Insert(End, insertedEnd);
        Tracks.Refresh(End + 1);
        return insertedEnd;
    }
    private CueIndexImpl AddIndex_NoIndexInTrack(CueTime time, CueDataFile file, CueTrack lastTrack)
    {
        (int Start, int End) = GetCueIndicesOfTrack_Range(lastTrack.Index, includeDangling: true);
        // track has no indices, and the previous file has no tracks - move track to current file
        int length = End - Start;
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
        foreach (CueDataFile file in donor.Files)
        {
            Files.Add(file.ClonePartial(ParentSheet));
        }
        foreach (CueTrack track in donor.Tracks)
        {
            int fIndex = track.ParentFile.Index;
            Tracks.Add(track.ClonePartial(Files[fIndex]));
        }
        foreach (CueIndexImpl cimpl in donor.Indexes)
        {
            int fIndex = cimpl.File.Index;
            int tIndex = cimpl.Track.Index;
            Indexes.Add(cimpl.ClonePartial(Tracks[tIndex], Files[fIndex]));
        }
        return;
    }
    internal (int Start, int End) GetCueTracksOfFile_Range(int fileIndex = -1)
    {
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        CueDataFile file = Files[fileIndex];
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
    internal IEnumerable<CueIndexImpl> GetCueIndicesOfTrackWithDangling(int fileIndex = -1)
    {
        (int Start, int End) = GetCueIndicesOfTrack_Range(fileIndex, includeDangling: true);
        return Indexes.Skip(Start).Take(End - Start);
    }
    internal (int Start, int End) GetCueIndicesOfFile_Range(int fileIndex = -1)
    {
        if (fileIndex < 0) fileIndex = Files.Count - 1;
        CueDataFile file = Files[fileIndex];
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
    /// <summary>
    /// Get range [inclusive,exclusive) of all indices pertaining to the track
    /// </summary>
    /// <param name="trackIndex"></param>
    /// <param name="includeDangling">If true, will include Index 00 if it occurs on the previous file. Otherwise that index is excluded</param>
    /// <returns></returns>
    internal (int Start, int End) GetCueIndicesOfTrack_Range(int trackIndex = -1, bool includeDangling = false)
    {
        if (trackIndex < 0) trackIndex = Tracks.Count - 1;
        CueTrack track = Tracks[trackIndex];
        int start = -1;
        int count = 0;
        for (int i = 0; i < Indexes.Count; i++)
        {
            bool condition;
            if (includeDangling) // Index only has to be match the track
            {
                condition = Indexes[i].Track == track;
            }
            else // Index has to be on the same file as well
            {
                condition = Indexes[i].Track == track && Indexes[i].File == track.ParentFile;
            }

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