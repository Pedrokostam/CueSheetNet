﻿using System.Collections;
using System.Collections.ObjectModel;

namespace CueSheetNet.Collections;

public class SheetFileCollection(CueSheet parent) : IFileCollection
{

    readonly ReferenceKeyedCollection<CueDataFile> _files=[];
    //readonly Dictionary<CueDataFile,int> _filePositions= new Dictionary<CueDataFile, int>(ReferenceEqualityComparer.Instance);
    readonly CueSheet _parentSheet = parent;
    public int Count => _files.Count;

    public CueDataFile this[int index] => _files[index];

    private CueDataFile CreateFile(string path, FileType fileType)
    {
        return new CueDataFile(_parentSheet, path, fileType);
    }

    private CueDataFile InsertImpl(int index, string path, FileType fileType)
    {
        _files.Insert(index, CreateFile(path, fileType));
        return _files[index];
    }

    public CueDataFile Add(string path, FileType fileType) => InsertImpl(Count, path, fileType);

    public CueDataFile Add(string path)
    {
        var type = CueDataFile.GetFileTypeFromPath(path);
        return Add(path, type);
    }

    internal CueDataFile? GetPreviousFile(CueDataFile file) => _files.GetPreviousItem(file);
    internal CueDataFile? GetNextFile(CueDataFile file) => _files.GetNextItem(file);
    internal IEnumerable<CueDataFile> GetNextFiles(CueDataFile file)=>_files.GetNextItems(file);

    public CueDataFile AddFileWithEacGaps(string path, FileType fileType)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<CueDataFile> GetEnumerator() => _files.GetEnumerator();

    public int IndexOf(string path, StringComparison stringComparison)
    {
        int index=0;
        foreach (var item in _files)
        {
            if (item.CheckPathEqual(path, stringComparison))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    public int IndexOf(CueDataFile file) => _files.IndexOf(file);
    public CueDataFile Insert(int index, string path, FileType fileType) => InsertImpl(index, path, fileType);
    public CueDataFile Insert(int index, string path)
    {
        var type = CueDataFile.GetFileTypeFromPath(path);
        return Insert(index, path, type);
    }

    public bool Remove(CueDataFile file) => _files.Remove(file);

    public void RemoveAt(int index) => _files.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return $"{Count} files";
    }
}
