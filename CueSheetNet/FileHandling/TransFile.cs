using CueSheetNet.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.FileHandling;
/// <summary>
/// Class which store information about original file, can backup its content to memory, delete the file, copy/move it to new location under a new name.
/// </summary>
internal record class TransFile
{
    public enum GeneralFileType
    {
        Cue,
        Audio,
        Extra
    }

    public string Subfolder { get; }
    public FileInfo SourceFile { get; }
    public GeneralFileType Type { get; }

    private string? extension;
    [AllowNull]
    public string Extension
    {
        get => extension ?? SourceFile.Extension;
        set
        {
            if (value is not null)
            {
                if (!value.StartsWith('.'))
                {
                    value = "." + value;
                }
            }
            extension = value;
        }
    }
    private string? newName;
    /// <summary>
    /// NewName of the file. No extension. If set to null, the old name will be used
    /// </summary>
    [AllowNull]
    public string NewName
    {
        get
        {
            return newName ?? Path.GetFileNameWithoutExtension(SourceFile.Name);
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
                return Path.ChangeExtension(SourceFile.Name, Extension);
            return newName + Extension;
        }
    }
    public TransFile(ICueFile source, DirectoryInfo? cueFolder, GeneralFileType type)
    {
        string? cueDir = source.SourceFile?.DirectoryName;
        if (cueFolder is null || cueDir is null)
        {
            Subfolder = ".";
        }
        else
        {
            Subfolder = Path.GetRelativePath(cueFolder.FullName, cueDir);
        }
        Type = type;
        SourceFile = source.SourceFile!;
    }

    public virtual FileInfo Copy(DirectoryInfo destination)
    {
        string dest = DestinationPath(destination);
        FileInfo res = SourceFile.CopyTo(dest);
        Logger.LogInformation("Copied file {File} from {Source}", res, SourceFile);
        return res;
    }
    public string DestinationPath(DirectoryInfo destination)
    {
        return Path.GetFullPath(Path.Combine(destination.FullName, Subfolder, NewNameWithExtension));
    }
    public virtual MovedFile Move(DirectoryInfo destination)
    {
        byte[] content = File.ReadAllBytes(SourceFile.FullName);
        string old = SourceFile.FullName;
        string dest = DestinationPath(destination);
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
        string checker = Path.Combine(directory.FullName, NewNameWithExtension);
        return File.Exists(checker);
    }

}
