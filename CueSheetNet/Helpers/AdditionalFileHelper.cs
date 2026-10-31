using CueSheetNet.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Helpers;

internal static class AdditionalFileHelper
{
    public static IEnumerable<CueExtraFile> FindExtraFiles(Sheet sheet, bool keepQualified = false)
    {
        if (sheet.SourceFile is null)
        {
            return [];
        }
        var baseName = Path.ChangeExtension(sheet.SourceFile.Name, null)!;
        HashSet<FileSystemInfo> skipFiles = new(
            sheet.Files.Select(x => new FileInfo(x.AbsolutePath!)),
            FileSystemInfoComparer.Default)
        {
            sheet.SourceFile,
        };

        return  sheet.Files
            .Select(x => x.AbsolutePath)
            .OfType<string>()
            .Select(x => new DirectoryInfo(x))
            .Append(sheet.SourceFile.Directory!)
            .Distinct(FileSystemInfoComparer.Default)
            .Cast<DirectoryInfo>()
            .SelectMany(x => SeekRelated(x, baseName, skipFiles))
            .Select(x => new CueExtraFile(sheet, x.FullName, keepQualified));
    }

    private static IEnumerable<FileInfo> SeekRelated(DirectoryInfo dir, string baseName, HashSet<FileSystemInfo> skipFiles, bool keepQualified = false)
    {
        foreach (var file in dir.EnumerateFiles())
        {
            if (skipFiles.Contains(file))
            {
                continue;
            }
            var noExtName = Path.ChangeExtension(file.Name, null);
            if (noExtName.Equals(baseName, StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
            if (noExtName.Equals("album", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
            if (noExtName.Equals("cover", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
            if (noExtName.Equals("art", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }
}
