using System.Collections;

namespace CueSheetNet.Collections;

public interface IIndexCollection : IReadOnlyList<CueIndex>
{

    /// <summary>
    /// Changes the time of the CueIndex at the specified absolute <paramref name="index"/>.
    /// </summary>
    /// <param name="index">Absolute index of item (for the whole container) to be updated. This is not the <see cref="CueIndex.Number">Number</see> of the CueIndex.</param>
    /// <param name="newTime">New time of the index. Set it to <see langword="null"/> to leave it unchanged.</param>
    /// <param name="newNumber">New number of the index. Set it to <see langword="null"/> to leave it unchanged. Number cannot be zero.
    /// <para>
    /// All indexes following this will have their number increased if necessary.
    /// </para></param>
    void ChangeIndex(int index, CueTime? newTime, int? newNumber);
    /// <summary>
    /// Creates and inserts a new <see cref="CueIndex"/> after every other element in the container.
    /// </summary>
    /// <inheritdoc cref="Insert(int, CueTime, int?)"/>
    void Add(CueTime time, int? number = null);
    /// <summary>
    /// Creates and inserts a new <see cref="CueIndex"/> at the specified position.
    /// </summary>
    /// <param name="index">The position at which a new item should be inserted.</param>
    /// <param name="time">Time of the new item. Must be greater than the preceding element's, and smaller than the following's.</param>
    /// <param name="number">Number of the new item. If set to <see langword="null"/>, the next available value will be used. Otherwise, will be clamped to suitable range.</param>
    void Insert(int index, CueTime time, int? number = null);
    /// <summary>
    /// Removes item with the specified <paramref name="number"/>.
    /// </summary>
    /// <param name="number"></param>
    /// <returns><see langword="true"/> if item was deleted; otherwise, <see langword="false"/></returns>
    bool Remove(int number);
    /// <summary>
    /// Removes item with the specified <paramref name="time"/>.
    /// </summary>
    /// <param name="number"></param>
    /// <returns><see langword="true"/> if item was deleted; otherwise, <see langword="false"/></returns>
    bool Remove(CueTime time);
    /// <summary>
    /// Removes item with at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="number"></param>
    void RemoveAt(int index);
    int IndexOf(CueTime time);
    int IndexOf(int number);
}

public interface ITrackCollection : IReadOnlyList<CueTrack>
{
    ///// <summary>
    ///// Changes the time of the CueIndex at the specified absolute <paramref name="index"/>.
    ///// </summary>
    ///// <param name="index">Absolute index of item (for the whole container) to be updated. This is not the <see cref="CueIndex.Number">Number</see> of the CueIndex.</param>
    ///// <param name="newTime">New time of the index. Set it to <see langword="null"/> to leave it unchanged.</param>
    ///// <param name="newNumber">New number of the index. Set it to <see langword="null"/> to leave it unchanged. Number cannot be zero.
    ///// <para>
    ///// All indexes following this will have their number increased if necessary.
    ///// </para></param>
    //public void ChangeTrack(int index, CueTime? newTime, int? newNumber);
    /// <summary>
    /// Creates and inserts a new <see cref="CueIndex"/> after every other element in the container.
    /// </summary>
    /// <inheritdoc cref="Insert(int, CueTime, int?)"/>
    CueTrack Add(TrackType trackType);
    /// <summary>
    /// Creates and inserts a new <see cref="CueIndex"/> at the specified position.
    /// </summary>
    /// <param name="index">The position at which a new item should be inserted.</param>
    /// <param name="time">Time of the new item. Must be greater than the preceding element's, and smaller than the following's.</param>
    /// <param name="number">Number of the new item. If set to <see langword="null"/>, the next available value will be used. Otherwise, will be clamped to suitable range.</param>
    CueTrack Insert(int index, TrackType trackType);
    /// <summary>
    /// Removes the specified <paramref name="track"/> from the collection.
    /// <para>
    /// Does nothing if the track is not part of this collection.
    /// </para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns><see langword="true"/> if item was deleted; otherwise, <see langword="false"/></returns>
    bool Remove(CueTrack track);
    /// <summary>
    /// Removes item with at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="number"></param>
    void RemoveAt(int index);
    /// <summary>
    /// Find the index of the track with the given <paramref name="title"/>.
    /// </summary>
    /// <param name="title">The title of the track to be found.</param>
    /// <returns>Position of the matching track in the collection. If no match found, returns -1</returns>
    int IndexOf(string title, StringComparison stringComparison);
    int IndexOf(CueTrack track);
}