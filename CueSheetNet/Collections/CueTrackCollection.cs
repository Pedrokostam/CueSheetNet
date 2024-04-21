using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Collections;
public class CueTrackCollection : Collection<CueTrack>
{
    private readonly CueContainer _container;
    private CueSheet ParentSheet =>_container.ParentSheet;
    private CueFileCollection Files => _container.Files;
    internal CueTrackCollection(CueContainer container)
    {
            _container = container;
    }

    /// <summary>
    /// Ensures that all files are indexed continuously, starting from zero.
    /// </summary>
    /// <param name="startFrom">Index from which to refresh.</param>
    public void Refresh(int startFrom=0)
    {
        if (Items.Count <= startFrom)
            return;
        int len = Items.Count - startFrom;
        for (int i = startFrom; i < len; i++)
        {
            Items[i].Index = i;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parsedNumber">The number of the track. Track number do not have to be continuous.</param>
    /// <param name="type"></param>
    /// <param name="fileIndex"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public void Add(int parsedNumber, TrackType type, int fileIndex = -1)
    {
        if (fileIndex < 0)
            fileIndex = Files.Count - 1;

        int trackIndex = Items.Count switch
        {
            0 => 0,
            _ => Items[^1].Index+1,
        };
        CueTrack track = new(Files[fileIndex], type)
        {
            Number = parsedNumber,
            Index = trackIndex,
        };

        Add(track);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parsedNumber">The number of the track. Track number do not have to be continuous.</param>
    /// <param name="type"></param>
    /// <param name="fileIndex"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public void Insert(int insertionIndex, int parsedNumber, TrackType type, int fileIndex = -1)
    {
        if (fileIndex < 0)
            fileIndex = Files.Count - 1;
        CueTrack newTrack = new(Files[fileIndex], type){ Number=parsedNumber};
        Insert(insertionIndex, newTrack);
    }

    protected override void InsertItem(int index, CueTrack item)
    {
        if (Files.Count == 0)
            throw new InvalidOperationException("Cannot add track without any file");
        ExceptionHelper.ThrowIfNotEqual(ParentSheet, item.ParentSheet, "Specified file does not belong to this cuesheet");
        Items.Insert(index, item);
        Refresh(index);
    }


    ///// <param name="index">Index of file to modify</param>
    ///// <inheritdoc cref="CueDataFile.SetFile(string, FileType?)"/>
    //public void Change(int index, string newPath, FileType? newType = null)
    //{
    //    Items[index].SetFile(newPath, newType);
    //}

    [DoesNotReturn]
    protected override void RemoveItem(int index)
    {
        throw new NotImplementedException();
    }
    [DoesNotReturn]
    protected override void ClearItems()
    {
        throw new NotImplementedException();
    }
}
