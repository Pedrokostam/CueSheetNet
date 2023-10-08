using CueSheetNet.FileHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;

[TestClass]
public class CopyingTests
{

    [TestMethod]
    public void CopyMoveSimple()
    {
        using var md5 = MD5.Create();
        var files = Utils.GetFiles("*.*", "CopyingTests");
        Dictionary<string, byte[]> hashes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (Path.GetExtension(file).ToLowerInvariant() == ".cue")
            {
                continue;
            }
            using FileStream fs = File.OpenRead(file);
            hashes.Add(Path.GetFileName(file), md5.ComputeHash(fs));
        }
        CueSheet sheet = CueSheet.Read(Utils.GetFile("CopyingTests", "Spandau Ballet - True.cue"));
        string output = Path.Combine(Path.GetTempPath(), "CueCopying");
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        Directory.CreateDirectory(output);

        sheet.CopyPackage(output, null);
        CueSheet copied = CueSheet.Read(Path.Combine(Path.GetTempPath(), "CueCopying", sheet.SourceFile.Name));
        Assert.AreEqual(copied, sheet);
        foreach (var file in Directory.EnumerateFiles(output))
        {
            if (Path.GetExtension(file).ToLowerInvariant() == ".cue")
            {
                continue;
            }
            using FileStream fs = File.OpenRead(file);
            var hash = md5.ComputeHash(fs);
            Assert.IsTrue(hashes[Path.GetFileName(file)].SequenceEqual(hash));
        }

    }
}
