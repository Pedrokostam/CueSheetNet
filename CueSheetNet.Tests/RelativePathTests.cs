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
    private static string SystemizePath(string path)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            var s = Regex.Replace(path, @"^[a-zA-Z]:", "/opt");
            return s.Replace('\\', Path.DirectorySeparatorChar);
        }
        return path;
    }
    private static FileInfo F(string path) => new(SystemizePath(path));

    private static DirectoryInfo D(string path) => new(SystemizePath(path));

    public static IEnumerable<object[]> ReferencePaths
    {
        get
        {
            return [
                [F(@"c:\001\bar\baz\bom.mp3"), F(@"c:\001\bar\baz\bom.mp3"), "bom.mp3"],
                [F(@"c:\002\bar\baz\bom.mp3"), D(@"c:\002\bar\baz"), "bom.mp3"],
                [F(@"c:\003\bar\baz\bom.mp3"), D(@"c:\003\bar\baz\"), "bom.mp3"],
                [D(@"c:\004\bar\baz"), D(@"c:\004\bar\baz"), "."],
                [D(@"c:\005\bar\baz"), D(@"c:\005\bar\baz\"), "."],
                [D(@"c:\006\bar\baz\"), D(@"c:\006\bar\baz"), "."],
                [D(@"c:\007\bar\baz"), F(@"c:\007\bar\baz\bom.mp3"), "."],
                [D(@"c:\008\bar\baz\"), F(@"c:\008\bar\baz\bom.mp3"), "."],
                [D(@"c:\009\bar\mun\bun"), F(@"c:\009\bar\baz\bom.mp3"), "../mun/bun/"],
                [D(@"c:\010\bar\mun\bun"), D(@"c:\010\bar\baz\"), "../mun/bun/"],
                [D(@"c:\011\bar\mun\bun"), D(@"c:\011\bar\baz"), "../mun/bun/"],

                [D(@".\012\mun\bun"), null!, "012/mun/bun/"],
                [D(@"013\mun\bun"), null!, "013/mun/bun/"],
                [F(@".\014\mun\bun.mp3"), null!, "014/mun/bun.mp3"],
                [F(@"015\mun\bun.mp3"), null!, "015/mun/bun.mp3"],

                [F(@"c:\016\bar\baz\bom.mp3"), F(@"c:\goo\bar\baz\bom.mp3"), "../../../016/bar/baz/bom.mp3"],

                [F(@"c:\017\bar\baz\bom.mp3"), D(@"c:\goo\bar\baz\bom"),  "../../../../017/bar/baz/bom.mp3"],
                [F(@"c:\018\bar\baz\bom.mp3"), D(@"c:\goo\bar\baz\bom\"), "../../../../018/bar/baz/bom.mp3"],
                [F(@"c:\019\bar/baz\bom.mp3"), D(@"c:\goo\bar\baz\bom/"), "../../../../019/bar/baz/bom.mp3"],

                [D(@"c:\020\bar\baz\bom"), F(@"c:\goo\bar\baz\bom.mp3"), "../../../020/bar/baz/bom/"],
                [D(@"c:\021\bar\baz\bom"), F(@"c:\goo\bar\baz\bom.mp3"), "../../../021/bar/baz/bom/"],
                [D(@"c:\022\bar/baz\bom"), F(@"c:\goo\bar\baz\bom.mp3"), "../../../022/bar/baz/bom/"],

                [D(@"c:\023\bar\baz\bom\"), F(@"c:\goo\bar\baz\bom.mp3"), "../../../023/bar/baz/bom/"],
                [D(@"c:\024\bar\baz\bom\"), F(@"c:\goo\bar\baz\bom.mp3\"), "../../../../024/bar/baz/bom/"],
                [D(@"c:\025\bar/baz\bom/"), F(@"c:\goo\bar\baz\bom.mp3/"), "../../../../025/bar/baz/bom/"],
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
