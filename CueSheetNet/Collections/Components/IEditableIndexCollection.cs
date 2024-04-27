using System.Collections;

namespace CueSheetNet.Collections;

public interface IIndexCollection : IReadOnlyList<CueIndex>  
{
    /// <summary>
    /// Find the index of the <see cref="CueIndex"/> with the given <paramref name="time"/>.
    /// </summary>
    /// <param name="time">The time of the item to be found.</param>
    /// <returns>Position of the matching item in the collection. If no match found, returns -1</returns>
    int IndexOf(CueTime time);
    /// <summary>
    /// Find the index of the <see cref="CueIndex"/> with the given <paramref name="number"/>.
    /// </summary>
    /// <param name="number">The number of the item to be found.</param>
    /// <inheritdoc cref="IndexOf(CueTime)"/>
    int IndexOf(int number);
}

public interface IEditableIndexCollection : IIndexCollection
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
    
}

public interface ITrackCollection : IReadOnlyList<CueTrack>
{
    /// <summary>
    /// Find the index of the file with the given <paramref name="title"/>.
    /// </summary>
    /// <param name="title">The title of the file to be found.</param>
    /// <param name="stringComparison">How title string should be compared.</param>
    /// <returns>Position of the matching file in the collection. If no match found, returns -1</returns>
    int IndexOf(string title, StringComparison stringComparison);

    /// <summary>
    /// Find the index of the <paramref name="track"/>.
    /// </summary>
    /// <param name="track">The file to be found.</param>
    /// <inheritdoc cref=" IndexOf(string, StringComparison)"/>
    int IndexOf(CueTrack track);
}

public interface IEditableTrackCollection : ITrackCollection
{
    /// <summary>
    /// Creates and inserts a new <see cref="CueTrack"/> after every other element in the container.
    /// </summary>
    /// <inheritdoc cref="Insert(int, TrackType)"/>
    CueTrack Add(TrackType trackType);
    /// <summary>
    /// Creates and inserts a new <see cref="CueTrack"/> at the specified position.
    /// </summary>
    /// <param name="index">The position at which a new item should be inserted.</param>
    /// <param name="trackType">Type of the new file.</param>
    /// <returns>The newly created file.</returns>
    CueTrack Insert(int index, TrackType trackType);
    /// <summary>
    /// Removes the specified <paramref name="track"/> from the collection.
    /// <para>
    /// Does nothing if the file is not part of this collection.
    /// </para>
    /// </summary>
    /// <param name="track">The file to be removed</param>
    /// <returns><see langword="true"/> if item was deleted; otherwise, <see langword="false"/></returns>
    bool Remove(CueTrack track);
    /// <summary>
    /// Removes item with at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="number"></param>
    void RemoveAt(int index);

}

public interface IFileCollection : IReadOnlyCollection<CueDataFile>
{
    /// <summary>
    /// Creates and inserts a new <see cref="CueDataFile"/> after every other element in the container.
    /// </summary>
    /// <inheritdoc cref="Insert(int,string, FileType)"/>
    CueDataFile Add(string path, FileType fileType);

    CueDataFile AddFileWithEacGaps(string path, FileType fileType);

    /// <remarks>
    /// <see cref="FileType"/> is decided based on the <paramref name="path"/>.</remarks>
    /// <inheritdoc cref="Add( string, FileType)"/>
    CueDataFile Add(string path);
    /// <summary>
    /// Creates and inserts a new <see cref="CueDataFile"/> at the specified position.
    /// </summary>
    /// <param name="index">The position at which a new item should be inserted.</param>
    /// <param name="path">Path of the new file, relative or absolute.</param>
    /// <param name="fileType">Type of the new file.</param>
    /// <returns>The newly created file.</returns>
    CueDataFile Insert(int index, string path, FileType fileType);
    /// <remarks>
    /// <see cref="FileType"/> is decided based on the <paramref name="path"/>.</remarks>
    /// <inheritdoc cref="Insert(int, string, FileType)"/>
    CueDataFile Insert(int index, string path);
    /// <summary>
    /// Removes the specified <paramref name="file"/> from the collection.
    /// <para>
    /// Does nothing if the file is not part of this collection.
    /// </para>
    /// </summary>
    /// <param name="file">The file to be removed</param>
    /// <returns><see langword="true"/> if item was deleted; otherwise, <see langword="false"/></returns>
    bool Remove(CueDataFile file);
    /// <summary>
    /// Removes item with at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="number"></param>
    void RemoveAt(int index);
    /// <summary>
    /// Find the index of the file with the given <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path (relative or absolute) of the file to be found.</param>
    /// <returns>Position of the matching file in the collection. If no match found, returns -1</returns>
    /// <param name="stringComparison"></param>
    int IndexOf(string path, StringComparison stringComparison);

    /// <summary>
    /// Find the index of the <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The file to be found.</param>
    /// <inheritdoc cref=" IndexOf(string, StringComparison)"/>
    int IndexOf(CueDataFile file);
}