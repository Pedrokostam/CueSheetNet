using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Collections;
public class CueFileCollection : Collection<CueDataFile>
{
    private readonly CueContainer _container;
    private CueSheet ParentSheet =>_container.ParentSheet;
    internal CueFileCollection(CueContainer container)
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

    public void Add(string filePath, FileType type) => Add(new CueDataFile(ParentSheet, filePath, type));

    public void Insert(int insertionIndex, string filePath, FileType type)
    {
        var cueFile = new CueDataFile(ParentSheet,filePath, type);
        Insert(insertionIndex, cueFile);
    }

    protected override void InsertItem(int index, CueDataFile item)
    {
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
