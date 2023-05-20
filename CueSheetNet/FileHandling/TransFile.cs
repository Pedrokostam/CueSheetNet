using CueSheetNet.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.FileHandling;

public record class TransFile
{
    private string? newName;

    public byte[]? Backup { get; private set; }
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
    public TransFile(FileInfo source)
    {
        SourceFile = source;
    }
    public FileInfo Copy(DirectoryInfo destination)
    {
        string dest = Path.Combine(destination.FullName, NewNameWithExtension);
        FileInfo res = SourceFile.CopyTo(dest);
        Logger.LogInformation("Copied file {File} from {Source}", res, SourceFile);
        return res;
    }
    public FileInfo Move(DirectoryInfo destination)
    {
        Backup = File.ReadAllBytes(SourceFile.FullName);
        string dest = Path.Combine(destination.FullName, NewNameWithExtension);
        SourceFile.MoveTo(dest);
        FileInfo res = new(dest);
        Logger.LogInformation("Moved file {File} from {Source}", res, SourceFile);
        return res;
    }
    //public FileInfo Rename()
    //{
    //    if (newName == Path.GetFileNameWithoutExtension(SourceFile.Name))
    //    {
    //        return SourceFile;
    //    }
    //    string dest = Path.Combine(SourceFile.DirectoryName!, $"{NewName}{Extension}");
    //    SourceFile.MoveTo(dest);
    //    Logger.LogInformation("Moved file {File} from {Source}", res, SourceFile);
    //    return SourceFile;
    //}


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
    internal void DeleteSource()
    {
        SourceFile.Delete();
        Logger.LogInformation("Deleted file {File}", SourceFile);
    }

    public bool Restore()
    {
        if (Backup is null) return false;
        if (SourceFile.Exists) return false;
        File.WriteAllBytes(SourceFile.FullName, Backup);
        Logger.LogInformation("Restored moved file {File}", SourceFile);
        return true;
    }
}