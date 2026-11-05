using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Helpers;

internal static class AdditionalFileHelper
{
    // Static HashSet for constant file names to avoid repeated allocation
    private static readonly System.Collections.Generic.HashSet<string> ConstantTargetNames = 
        new(StringComparer.OrdinalIgnoreCase) { "album", "cover", "art" };

    public static IEnumerable<CueExtraFile> FindExtraFiles(CueSheet sheet)
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
            .Select(x => new CueExtraFile(x.FullName, sheet, sheet.UseQualifiedPaths));
    }
    private static bool MatchesBaseName(string nameNoExt, string baseName)
    {
        if(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
        {
            return nameNoExt.Equals(baseName, StringComparison.OrdinalIgnoreCase) ||
                   ConstantTargetNames.Contains(nameNoExt);
        }

        return nameNoExt.Equals(baseName, StringComparison.Ordinal) ||
               ConstantTargetNames.Contains(nameNoExt);
    }   

    private static IEnumerable<FileInfo> SeekRelated(DirectoryInfo dir, string baseName, HashSet<FileSystemInfo> skipFiles, bool keepQualified = false)
    {
        IEnumerable<FileInfo> files;
        try
        {
            files = dir.EnumerateFiles();
        }
        catch (System.IO.DirectoryNotFoundException)
        {
            yield break;
        }
        catch (System.UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            if (skipFiles.Contains(file))
            {
                continue;
            }
            var noExtName = Path.ChangeExtension(file.Name, null);
            // Check if filename matches baseName or one of the constant target names
            if (ConstantTargetNames.Contains(noExtName) || MatchesBaseName(noExtName, baseName))
            {
                yield return file;
            }
        }
    }
}
