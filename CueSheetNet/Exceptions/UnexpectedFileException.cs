using CueSheetNet.Components;

namespace CueSheetNet.Exceptions;

public class UnexpectedFileException(string previousFile, string unexceptedFile) : Exception($"Attempted to add file \"{unexceptedFile}\" when the previous \"{previousFile}\" had no tracks")
{

}
public class UnexpectedIndexException() : Exception($"Attempted to add index without a file or track")
{

}

public class DuplicateTimeException(CueTime time, CueDataFile file) : Exception($"Attempted to mark the same index {time} as two indices in one file \"{file}\"")
{

}

public class IndexOutsideOfFileException(string fileName, CueTime index) : Exception($"Attempted to add index {index} with index greater than the duration of the file \"{fileName}\"")
{

}

public class InvalidTrackPlacementException : Exception
{
    public InvalidTrackPlacementException(CueTime time) : base($"Attempted to insert a track in the middle of another track at {time}")
    {
    }
    public InvalidTrackPlacementException(int desiredPosition) : base($"Attempted to insert a track with position that was incompatible with other tracks ({desiredPosition})")
    {
    }
}
public class IndexOutsideOfTrackException(CueTrack track, CueTime time) : Exception($"Attempted to add index {time} to a track {track} that is outside of the track's range")
{

}

public class NegativeIndexTimeException : Exception
{
    public static void ThrowIfNegative(CueTime time)
    {
        if (time < CueTime.Zero)
        {
            throw new NegativeIndexTimeException("Attempted to add a negative index index to a cue sheet");
        }
    }
    public static void ThrowIfNegative(CueIndex index)
    {
        if (index.Time < CueTime.Zero)
        {
            throw new NegativeIndexTimeException($"Attempted to add a negative index index to a cue sheet ({index})");
        }
    }
    private NegativeIndexTimeException(string message)
    {

    }
}

public class InvalidIndexTimeException : Exception
{
    public InvalidIndexTimeException(CueIndex previousIndex, CueIndex nextIndex) : base($"Attempted to add index {nextIndex} after {previousIndex}")
    {
    }
    public InvalidIndexTimeException(CueIndex negativeTimeIndex) : base($"Attempted to add index with negative index {negativeTimeIndex}")
    {
    }
}

public class InvalidIndexPositionException : Exception
{
    public InvalidIndexPositionException(CueIndex previousIndex, CueIndex nextIndex) : base($"Attempted to add index \"{nextIndex}\" after \"{previousIndex}\"")
    {
    }

    public InvalidIndexPositionException(CueIndex lateIndex) : base($"Atempted to add index {lateIndex} as the first index of a track")
    {

    }
}

public class TrackSplitAcrossFileException(CueTrack track) : Exception($"Attempted to add an index of {track} to a different file than {track.File.Name}")
{

}

public class InvalidIndexEditException(string property, object? oldValue, object? newValue) : Exception($"Error when changing {property} from {oldValue ?? "<null>"} to {newValue ?? "<null>"}")
{

}