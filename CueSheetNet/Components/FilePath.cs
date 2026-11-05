using System.Diagnostics.CodeAnalysis;
using IoPath = System.IO.Path;

namespace CueSheetNet;

public abstract class FilePath
{
    protected readonly CueSheet _sheet;
    protected readonly IList<string> _parts = [];
    /// <summary>
    /// The path as it was written in the cue sheet.
    /// </summary>
    public string Path => string.Join(IoPath.AltDirectorySeparatorChar, _parts);
    public string Name => _parts[^1];
    public string BaseName => IoPath.GetFileNameWithoutExtension(Name);
    public bool IsQualified => IsFirstPartQualified(_parts);
    protected FileInfo? Info { get; set; }
    public string? AbsolutePath
    {
        get
        {
            if (_sheet.SourceFile is null)
            {
                return null;
            }
            return GetAbsolutePartsWith(_sheet.SourceFile.FullName);
        }
    }
    [MemberNotNullWhen(returnValue: true, nameof(AbsolutePath), nameof(Info))]
    /// <inheritdoc cref="FileInfo.Exists"/>
    public bool Exists => Info?.Exists ?? false;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="sheet"></param>
    /// <param name="keepQualified">When true and if the given <paramref name="path"/> is a qualified path, the result will remain a qualified path.
    /// <br/>
    /// Relevant only when the path is qualified and <paramref name="sheet"/> has a source file.
    /// <br>
    /// If the sheet has no source file a qualified path will always remain qualified.
    /// </br></param>
    /// <exception cref="ArgumentException"></exception>
    protected FilePath(string path, CueSheet sheet, bool keepQualified = false)
    {
        _sheet = sheet;
        Update(path, keepQualified);
        if (_parts.Count == 0)
        {
            throw new ArgumentException("Cannot create a FilePath without any parts");
        }
    }

    protected static bool GetParts(ReadOnlySpan<char> path, ICollection<string> parts)
    {
        parts.Clear();
        var s = path
            .Trim()
            .TrimEnd(IoPath.DirectorySeparatorChar)
            .TrimEnd(IoPath.AltDirectorySeparatorChar);
        if (s.Length == 0)
        {
            return false;
        }
        if (s[0] == '.')
        {
            if (s[1] == IoPath.DirectorySeparatorChar || s[1] == IoPath.AltDirectorySeparatorChar)
            {
                s = s[2..];
            }
        }
        Span<Range> ranges = stackalloc Range[64];
        int count = Partos(s, ranges);
        if (count == 0)
        {
            return false;
        }
        for (int i = 0; i < count; i++)
        {
            parts.Add(s[ranges[i]].ToString());
        }
        return true;
    }

    public void Update(string newPath, bool keepAbsolute = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPath);
        newPath = newPath.Replace(@"\\", @"\", StringComparison.Ordinal);
        bool isAbsolute = IoPath.IsPathFullyQualified(newPath);
        if (isAbsolute && _sheet.SourceFile is not null && !keepAbsolute)
        {
            newPath = PathHelper.GetRelativePath(newPath, _sheet.SourceFile);
        }
        GetParts(newPath.AsSpan(), _parts);
        Info = AbsolutePath is string x ? new FileInfo(x) : null;
    }

    public void Copy(string cueSheetDirectory)
    {
        if (Info is null)
        {
            return;
        }
        string newAbsPath = GetAbsolutePartsWith(cueSheetDirectory);
        Info.CopyTo(newAbsPath);
    }
    public void Delete()
    {
        if (Info is null)
        {
            return;
        }
        Info.Delete();
    }

    private string GetAbsolutePartsWith(string directory)
    {
        string[] pp = new string[_parts.Count + 1];
        pp[0] = directory;
        _parts.CopyTo(pp, 1);
        var newAbsPath = IoPath.Join(pp);
        return newAbsPath;
    }

    private static bool IsFirstPartQualified(IList<string> parts)
    {
        if (parts.Count == 0)
        {
            return false;
        }
        if (OperatingSystem.IsWindows())
        {
            var firstPart = parts[0];
            if (firstPart.Length == 2
                && char.IsLetter(firstPart[0])
                && firstPart[1] == ':')
            {
                return true;
            }
            return false;
        }
        else
        {
            var firstPart = parts[0];
            if (firstPart.Length == 0 && firstPart[0] == IoPath.DirectorySeparatorChar)
            {
                return true;
            }
            return false;
        }
    }
    private static int Partos(ReadOnlySpan<char> path, Span<Range> ranges)
    {
        if (path.Length == 0)
        {
            return 0;
        }
        path = path.Trim();
        if (OperatingSystem.IsWindows())
        {
            var count = path.Split(ranges,
                           [IoPath.DirectorySeparatorChar, IoPath.AltDirectorySeparatorChar],
                           StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return count;
        }
        else
        {
            if (path[0] == IoPath.DirectorySeparatorChar)
            {
                if (path.Length == 1)
                {
                    return 0;
                }
                ranges[0] = new Range(0, 1);
                path = path[1..];
                var count = path.Split(ranges[1..],
                           [IoPath.DirectorySeparatorChar, IoPath.AltDirectorySeparatorChar],
                           StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return count + 1;
            }
            if (path.Length > 1 && path[0] == '.' && path[1] == '/')
            {
                //skip relative path start
                path = path[2..];
            }
            return path.Split(ranges,
                       [IoPath.DirectorySeparatorChar, IoPath.AltDirectorySeparatorChar],
                       StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns>True, if the object is now relative.</returns>
    private bool TryMakeRelative()
    {
        if (!IsQualified)
        {
            return true;
        }
        if (_sheet.SourceFile is null)
        {
            return true;
        }
        var relative = PathHelper.GetRelativePath(AbsolutePath!, _sheet.SourceFile);
        Update(relative, false);
        return IsQualified;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns>True, if the object is now qualified.</returns>
    private bool TryMakeQualified()
    {
        if (IsQualified)
        {
            return true;
        }
        if (_sheet.SourceFile is null)
        {
            return false;
        }
        Update(AbsolutePath!, true);
        return IsQualified;
    }
    internal void ChangeFilePathType(bool keepPathQualified)
    {
        if (keepPathQualified)
        {
            TryMakeQualified();
        }
        else
        {
            TryMakeRelative();
        }
    }
}
