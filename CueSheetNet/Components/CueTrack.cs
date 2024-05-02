using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using CueSheetNet.Collections;
using CueSheetNet.Internal;
using CueSheetNet.Syntax;

namespace CueSheetNet;

public class CueTrack : CueItemBase,
        IEquatable<CueTrack>,
        IRemCommentable,
    IIndexValidator
{
    private CueDataFile _parentFile;

    public FieldsSet CommonFieldsSet { get; private set; }

    /// <summary>
    /// Absolute, zero-based, index for the whole CueSheet.
    /// </summary>
    public int Index { get; internal set; }

    public TrackType Type { get; internal set; }
    public CueTime PostGap { get; set; }
    public CueTime PreGap { get; set; }

    public CueTime? EacEndIndex { get; internal set; }

    public TrackIndexCollection Indices { get; }

    /// <summary>
    /// File in which the start of the content occurs (see <see cref="AudioStartIndex"/>).
    /// </summary>
    public CueDataFile ParentFile
    {
        get
        {
            CheckOrphaned();
            return _parentFile;
        }
        set { _parentFile = value; }
    }

    /// <summary>
    /// Number of track as it appears in the CUE sheet.
    /// <para>
    /// CUE sheet tracks do not have to be number continuously, there can be gaps.
    /// </para>
    /// </summary>
    public int Number { get; set; }

    private string? _Title;
    [AllowNull]
    public string Title
    {
        get => _Title ?? Path.ChangeExtension(ParentFile.SourceFile.Name, extension: null);
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Title = null;
                CommonFieldsSet &= ~FieldsSet.Title;
            }
            else
            {
                _Title = value;
                CommonFieldsSet |= FieldsSet.Title;
            }
        }
    }

    private string? _Performer;
    public string? Performer
    {
        get => _Performer ?? ParentSheet.Performer;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Performer = null;
                CommonFieldsSet &= ~FieldsSet.Performer;
            }
            else
            {
                _Performer = value;
                CommonFieldsSet |= FieldsSet.Performer;
            }
        }
    }

    private string? _Composer;

    public CueTrack(CueDataFile parentFile, TrackType type) : base(parentFile.ParentSheet)
    {
        _parentFile = parentFile;
        Type = type;
        Indices = new(this);
    }

    public string? Composer
    {
        get => _Composer ?? ParentSheet.Composer;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Composer = null;
                CommonFieldsSet &= ~FieldsSet.Composer;
            }
            else
            {
                _Composer = value;
                CommonFieldsSet |= FieldsSet.Composer;
            }
        }
    }
    public TrackFlags Flags { get; set; } = TrackFlags.None;

    public string? ISRC { get; set; }

    public bool HasZerothIndex { get; internal set; }

    /// <summary>
    /// Gets the time index at which the content start.
    /// <para>
    /// The content starts at the first index, unless the track has more than 1 indices and one of them is the zeroth index. In this case the first non-zeroth index is returned.
    /// </para>
    /// </summary>
    public CueIndex AudioStartIndex => Indices.GetAudioStartIndex();
    public CueTime? Duration
    {
        get
        {
            if (EacEndIndex is not null)
            {
                return EacEndIndex.Value;
            }
            if (ParentFile.Tracks.GetNextTrack(this) is CueTrack nextTrack && nextTrack.Indices.Count > 0)
            {
                return nextTrack.Indices[0].Time;
            }
            if (ParentSheet.Files.GetNextFile(ParentFile) is CueDataFile nextFile)
            {
                var timeByTrack =nextFile.Tracks.FirstOrDefault()?.Indices.FirstOrDefault()?.Time;
                return nextFile.Tracks.FirstOrDefault()?.Indices.FirstOrDefault()?.Time ?? ParentFile.Meta?.CueDuration;
            }
            return null;
        }
    }

    public RemarkCollection Remarks { get; } = [];
    public CommentCollection Comments { get; } = [];

    public override string ToString()
    {
        return "Track " + Number.ToString("D2") + ": " + Title;
    }

    internal void ClonePartial(CueTrack newTrack)
    {
        newTrack.CommonFieldsSet = CommonFieldsSet;
        newTrack.Composer = Composer;
        newTrack.Flags = Flags;
        newTrack.Title = Title;
        newTrack.PostGap = PostGap;
        newTrack.PreGap = PreGap;
        newTrack.Performer = Performer;
        newTrack.ISRC = ISRC;
        newTrack.HasZerothIndex = HasZerothIndex;
        newTrack.Index = Index;
        newTrack.Number = Number;
        newTrack.Remarks.Add(Remarks);
        newTrack.Comments.Add(Comments);
    }
    internal CueTrack ClonePartial(CueDataFile newOwner)
    {
        CueTrack newTrack =
            new(newOwner, Type);
        ClonePartial(newTrack);
        return newTrack;
    }

    public bool Equals(CueTrack? other) => Equals(other, StringComparison.InvariantCulture);

    public bool Equals(CueTrack? other, StringComparison stringComparison)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other == null)
            return false;
        if (Comments.Count != other.Comments.Count)
            return false;
        if (Remarks.Count != other.Remarks.Count)
            return false;
        if (
            PostGap != other.PostGap
            || PreGap != other.PreGap
            || !string.Equals(Performer, other.Performer, stringComparison)
            || !string.Equals(ISRC, other.ISRC, stringComparison)
            || !string.Equals(Composer, other.Composer, stringComparison)
            || !string.Equals(Title, other.Title, stringComparison)
            || Flags != other.Flags)
        {
            return false;
        }

        bool commentsEqual = Comments.SequenceEqual(other.Comments,StringHelper.GetComparer(stringComparison));
        bool remarksEqual = Remarks.SequenceEqual(other.Remarks,StringHelper.GetComparer(stringComparison));
        if (!commentsEqual || !remarksEqual)
        { return false; }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CueTrack, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Title.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Performer?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            ISRC?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Composer?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            PostGap.GetHashCode(),
            Comments.Count.GetHashCode(),
            Remarks.Count.GetHashCode()
        );
    }

    public bool ValidateIndex(int index, CueTime time, int number, bool replacesItem)
    {
        Debug.Assert(index >= 0 && index <= Indices.Count);
        var prevTrack = ParentFile.Tracks.GetPreviousTrack(this);
        var nextTrack = ParentFile.Tracks.GetNextTrack(this);
        var virtualIndexes = new List<(CueIndex? index, bool thisTrack)>(Indices.Count+2);

        virtualIndexes.Add((prevTrack?.Indices.LastOrDefault(), false));
        foreach (var i in Indices)
        {
            virtualIndexes.Add((i, true));
        }
        virtualIndexes.Add((nextTrack?.Indices.FirstOrDefault(), false));

        CueIndex?[] virtualCueIndex = [prevTrack?.Indices.LastOrDefault(), ..Indices, nextTrack?.Indices.FirstOrDefault()];
        //The first index is from the previous track, so offset it by 1
        index += 1;
        int prevIndex= index-1;
        int nextIndex = replacesItem switch
        {
            true => index+1,// replaces at index, so index+1 is the prev element
            false => index // insert before index, so index will be the prev element
        };
        var precedingValue = virtualIndexes[prevIndex];
        var followingValue = virtualIndexes[nextIndex];
        bool prevGit=true;
        bool nextGit=true;

        if (precedingValue.index is CueIndex preC)
        {
            prevGit = preC.Time < time;
            if (precedingValue.thisTrack)
            {
                prevGit &= preC.Number < number;
            }
        }

        if (followingValue.index is CueIndex postC)
        {
            nextGit = postC.Time > time;
            if (followingValue.thisTrack)
            {
                nextGit &= postC.Number > number;
            }
        }

        return prevGit && nextGit;
    }
}
