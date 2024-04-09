using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;
[TestClass]
public class RelativePathTests
{
    private static FileInfo F(string path) => new FileInfo(path);
    private static DirectoryInfo D(string path) => new DirectoryInfo(path);
    public static IEnumerable<object[]> ReferencePaths
    {
        get
        {
            return [
                [F(@"c:\foo\bar\baz\bom.mp3"), F(@"c:\foo\bar\baz\bom.mp3"), "bom.mp3"],
                [F(@"c:\foo\bar\baz\bom.mp3"), D(@"c:\foo\bar\baz"), "bom.mp3"],
                [F(@"c:\foo\bar\baz\bom.mp3"), D(@"c:\foo\bar\baz\"), "bom.mp3"],
                [D(@"c:\foo\bar\baz"), D(@"c:\foo\bar\baz"), "."],
                [D(@"c:\foo\bar\baz"), D(@"c:\foo\bar\baz\"), "."],
                [D(@"c:\foo\bar\baz\"), D(@"c:\foo\bar\baz"), "."],
                [D(@"c:\foo\bar\baz"), F(@"c:\foo\bar\baz\bom.mp3"), "."],
                [D(@"c:\foo\bar\baz\"), F(@"c:\foo\bar\baz\bom.mp3"), "."],
                [D(@"c:\foo\bar\mun\bun"), F(@"c:\foo\bar\baz\bom.mp3"), "../mun/bun/"],
                [D(@"c:\foo\bar\mun\bun"), D(@"c:\foo\bar\baz\"), "../mun/bun/"],
                [D(@"c:\foo\bar\mun\bun"), D(@"c:\foo\bar\baz"), "../mun/bun/"],
                ];
        }
    }
    [TestMethod]
    [DynamicData(nameof(ReferencePaths))]
    public void TestEqual(FileSystemInfo target, FileSystemInfo? basePath, string template)
    {
        var result = PathHelper.GetRelativePath(target, basePath);
        result = Regex.Replace(result, @"[\\/]", "/");
        Assert.AreEqual(template, result);
    }
}
