using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;
public static class PathHelper
{
    private static string GetPathWithSeparator(DirectoryInfo directory)
    {
        string fullname = directory.FullName;
        char lastChar = fullname[^1];
        return lastChar switch
        {
            //'/' or '\\' or '\u00A5' => fullname, // with yen sign
            '/' or '\\' => fullname,
            _ => fullname + Path.DirectorySeparatorChar,
        };
    }
    public static string GetRelativePath(string target, DirectoryInfo? baseDir)
    {
        string? basePath = baseDir is null ? null : GetPathWithSeparator(baseDir);
        return GetRelativePath(target, basePath);
    }
    public static string GetRelativePath(string target, FileInfo? baseFile)
    {
        string? basePath = baseFile?.Directory is null ? null : GetPathWithSeparator(baseFile.Directory!);
        return GetRelativePath(target, basePath);
    }
    public static string GetRelativePath(FileInfo target, FileInfo? baseFile) => GetRelativePath(target.FullName, baseFile);
    public static string GetRelativePath(DirectoryInfo target, FileInfo? baseFile) => GetRelativePath(GetPathWithSeparator(target), baseFile);
    public static string GetRelativePath(FileInfo target, DirectoryInfo? baseDir) => GetRelativePath(target.FullName, baseDir);
    public static string GetRelativePath(DirectoryInfo target, DirectoryInfo? baseDir) => GetRelativePath(GetPathWithSeparator(target), baseDir);
    public static string GetRelativePath(FileSystemInfo target, FileSystemInfo? baseDir)
    {
        return (target, baseDir) switch
        {
            (DirectoryInfo t, DirectoryInfo b) => GetRelativePath(t, b),
            (DirectoryInfo t, FileInfo b) => GetRelativePath(t, b),
            (FileInfo t, DirectoryInfo b) => GetRelativePath(t, b),
            (FileInfo t, FileInfo b) => GetRelativePath(t, b),
            (FileInfo t, null) => GetRelativePath(t, baseDir: null),
            (DirectoryInfo t, null) => GetRelativePath(t, baseDir: null),
            _ => throw new NotSupportedException(),
        };
    }
    private static string GetRelativePath(string target, string? basePath)
    {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER // GetRelativePath introduced in NET Core 2.0 and NETStandard2.1
        return Path.GetRelativePath(basePath ?? ".", target);
#else
        // base has to end with a separator
        var fileUri = new Uri(target);
        basePath ??= Environment.CurrentDirectory + Path.DirectorySeparatorChar;
        var referenceUri = new Uri(basePath);
        string relative = Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        if(relative == string.Empty)
        {
            relative = ".";
        }
        return relative;
#endif
    }
}
