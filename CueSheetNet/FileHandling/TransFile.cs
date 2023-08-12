using CueSheetNet.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.FileHandling;

/// <summary>
/// Class which store information about original file, can backup its content to memory, delete the file, copy/move it to new location under a new name.
/// </summary>
public record class TransFile
{
    private string? newName;
    public string? Subfolder { get; init; }
    public byte[]? Backup { get; private set; }
    public ICueFile CueSource { get; }

    public string Extension => CueSource.SourceFile.Extension;
    /// <summary>
    /// NewName of the file. No extension. If set to null, the old name will be used
    /// </summary>
    [AllowNull]
    public string NewName
    {
        get
        {
            if (newName == null)
                return Path.GetFileNameWithoutExtension(CueSource.SourceFile.Name);
            return newName;
        }
        set
        {
            newName = value;
        }
    }

    public string NewNameWithExtension
    {
        get
        {
            if (newName == null)
                return Path.GetFileName(CueSource.SourceFile.Name);
            return newName + Extension;
        }
    }

    public TransFile(ICueFile source)
    {
        CueSource = source;
    }

    public virtual FileInfo Copy(DirectoryInfo destination)
    {
        string dest = Path.Combine(destination.FullName, NewNameWithExtension);
        FileInfo res = CueSource.SourceFile.CopyTo(dest);
        Logger.LogInformation("Copied file {File} from {Source}", res, CueSource);
        return res;
    }

    public virtual FileInfo Move(DirectoryInfo destination)
    {
        Backup = File.ReadAllBytes(CueSource.SourceFile.FullName);
        string dest = Path.Combine(destination.FullName, NewNameWithExtension);
        CueSource.SourceFile.MoveTo(dest);
        FileInfo res = new(dest);
        Logger.LogInformation("Moved file {File} from {Source}", res, CueSource);
        return res;
    }

    /// <summary>
    /// Check if the file with NewName exists in a given folder
    /// </summary>
    /// <param name="directory"></param>
    /// <returns>True if such file exists, False if not</returns>
    public bool CheckNewNameExists(DirectoryInfo directory)
    {
        string checker = Path.Join(directory.FullName, NewNameWithExtension);
        return Path.Exists(checker);
    }

    /// <summary>
    /// Deletes file under <see cref="CueSource"/>, copying to <see cref="Backup"/> first.
    /// </summary>
    internal void DeleteSource()
    {
        Backup = File.ReadAllBytes(CueSource.SourceFile.FullName);
        CueSource.SourceFile.Delete();
        Logger.LogInformation("Deleted file {File}", CueSource);
    }

    /// <summary>
    /// Recreates a moved/deleted file from the <see cref="Backup"/>. The restored file will have the path of <see cref="CueSource"/>
    /// </summary>
    /// <returns>True if file was restored; false if <see cref="Backup"/> was missing or file under the same name already existed</returns>
    public virtual bool Restore()
    {
        if (Backup is null) return false;
        if (CueSource.SourceFile.Exists) return false;
        File.WriteAllBytes(CueSource.SourceFile.FullName, Backup);
        Logger.LogInformation("Restored moved file {File}", CueSource);
        return true;
    }
}

public record class TransAudioFile : TransFile
{
    public int FileIndex { get; }
    public CueSheet ParentSheet { get; }
    public TransAudioFile(CueSheet sheet, int index) : base(sheet.Files[index])
    {
        ParentSheet = sheet;
        FileIndex = index;
    }
    public override FileInfo Copy(DirectoryInfo destination)
    {
        var f = base.Copy(destination);
        ParentSheet.ChangeFile(FileIndex, NewNameWithExtension);
        return f;
    }
    public override FileInfo Move(DirectoryInfo destination)
    {
        var f = base.Move(destination);
        ParentSheet.ChangeFile(FileIndex, NewNameWithExtension);
        return f;
    }
    public override bool Restore()
    {
        var b = base.Restore();
        ParentSheet.ChangeFile(FileIndex,)
    }
}