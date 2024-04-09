using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;
internal static class PathHelper
{
    public static string GetRelativePath(DirectoryInfo targetDirectory, string? basePath) => GetRelativePath(targetDirectory.FullName, basePath);
    public static string GetRelativePath(FileInfo targetFile, string? basePath) => GetRelativePath(targetFile.FullName, basePath);
    public static string GetRelativePath(string target, string? basePath)
    {
#if NET7_0_OR_GREATER
        return Path.GetRelativePath(basePath ?? ".", target);
#else
        var fileUri = new Uri(target);
        var referenceUri = new Uri(basePath ?? (Environment.CurrentDirectory + Path.DirectorySeparatorChar));
        return Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
#endif
    }
}
