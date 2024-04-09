using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;
internal static class PathHelper
{
    public static string GetRelativePath(string target, DirectoryInfo? baseDir)
    {
        string? basePath = baseDir is null ? null : baseDir.FullName + Path.DirectorySeparatorChar;
        return GetRelativePath(target, basePath);
    }
    public static string GetRelativePath(string target, FileInfo? baseFile)
    {
        string? basePath = baseFile is null ? null : baseFile.DirectoryName + Path.DirectorySeparatorChar;
        return GetRelativePath(target, basePath);
    }
    public static string GetRelativePath(FileInfo target, FileInfo? baseFile) => GetRelativePath(target.FullName, baseFile);
    public static string GetRelativePath(DirectoryInfo target, FileInfo? baseFile) => GetRelativePath(target.FullName+Path.DirectorySeparatorChar, baseFile);
    public static string GetRelativePath(FileInfo target, DirectoryInfo? baseDir) => GetRelativePath(target.FullName, baseDir);
    public static string GetRelativePath(DirectoryInfo target, DirectoryInfo? baseDir) => GetRelativePath(target.FullName + Path.DirectorySeparatorChar, baseDir);
    private static string GetRelativePath(string target, string? basePath)
    {
#if NET7_0_OR_GREATER
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
