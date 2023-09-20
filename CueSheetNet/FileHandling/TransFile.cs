using CueSheetNet.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.FileHandling;

internal record class MovedFile(string OldPath, string NewPath, byte[] Content)
{
    public void Undo()
    {
        File.WriteAllBytes(OldPath, Content);
        File.ReadAllBytes(NewPath);
    }
}
/// <summary>
/// Class which store information about original file, can backup its content to memory, delete the file, copy/move it to new location under a new name.
/// </summary>
internal record class TransFile
{
    private string? newName;
    public string Subfolder { get; }
    public FileInfo SourceFile { get; }

    public string Extension => SourceFile.Extension;
    /// <summary>
    /// NewName of the file. No extension. If set to null, the old name will be used
    /// </summary>
    [AllowNull]
    public string NewName
    {
        get
        {
            if (newName == null)
                return Path.GetFileNameWithoutExtension(SourceFile.Name);
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
                return Path.GetFileName(SourceFile.Name);
            return newName + Extension;
        }
    }

    public TransFile(ICueFile source, DirectoryInfo? cueFolder)
    {
        if (cueFolder is null)
        {
            Subfolder = ".";
        }
        else
        {
            Subfolder = Path.GetRelativePath(cueFolder.FullName, source.SourceFile.FullName);
        }
        SourceFile = source.SourceFile;
    }

    public virtual FileInfo Copy(DirectoryInfo destination)
    {
        string dest = Path.GetFullPath(Path.Join(destination.FullName, Subfolder, NewNameWithExtension));
        FileInfo res = SourceFile.CopyTo(dest);
        Logger.LogInformation("Copied file {File} from {Source}", res, SourceFile);
        return res;
    }

    public virtual MovedFile Move(DirectoryInfo destination)
    {
        byte[] content = File.ReadAllBytes(SourceFile.FullName);
        string old = SourceFile.FullName;
        string dest = Path.GetFullPath(Path.Join(destination.FullName, Subfolder, NewNameWithExtension));
        SourceFile.MoveTo(dest);
        FileInfo res = new(dest);
        Logger.LogInformation("Moved file {File} from {Source}", res, SourceFile);
        return new MovedFile(old, res.FullName, content);
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

}
/*
internal record class TransAudioFile : TransFile
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
    public override MovedFile Move(DirectoryInfo destination)
    {
        var f = base.Move(destination);
        ParentSheet.ChangeFile(FileIndex, NewNameWithExtension);
        return f;
    }
}*/