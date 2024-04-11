using CueSheetNet.FileHandling;
using System.Security.Cryptography;

namespace CueSheetNet.Tests;

[TestClass]
public class CopyingTests
{

    [TestMethod("Copies sheet package and compares hashes of non-cue files, then moves the copied one and compares hashes again")]
    public void CopyMoveSimple()
    {
        using var md5 = MD5.Create();
        var files = Utils.GetFiles("*.*", "CopyingTests");
        Dictionary<string, byte[]> hashes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (Path.GetExtension(file).Equals(".cue", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            using FileStream fs = File.OpenRead(file);
            hashes.Add(Path.GetFileName(file), md5.ComputeHash(fs));
        }
        CueSheet sheet = CueSheet.Read(Utils.GetFile("CopyingTests", "Spandau Ballet - True.cue"));
        string output = CreateTempDirectory("CueCopying");

        sheet.CopyPackage(output, null);
        CueSheet copied = CueSheet.Read(Path.Combine(Path.GetTempPath(), "CueCopying", sheet.SourceFile!.Name));
        Assert.AreEqual(copied, sheet);
        foreach (var file in Directory.EnumerateFiles(output))
        {
            if (Path.GetExtension(file).Equals(".cue", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            using FileStream fs = File.OpenRead(file);
            var hash = md5.ComputeHash(fs);
            Assert.IsTrue(hashes[Path.GetFileName(file)].SequenceEqual(hash));
        }
        string outputMove = CreateTempDirectory("CueCopying", "Moving");
        copied.MovePackage(outputMove, null);
        CueSheet moved = CueSheet.Read(Path.Combine(Path.GetTempPath(), "CueCopying", "Moving", sheet.SourceFile.Name));
        Assert.AreEqual(copied, moved);
        foreach (var file in Directory.EnumerateFiles(outputMove))
        {
            if (Path.GetExtension(file).Equals(".cue", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            using FileStream fs = File.OpenRead(file);
            var hash = md5.ComputeHash(fs);
            Assert.IsTrue(hashes[Path.GetFileName(file)].SequenceEqual(hash));
        }
    }

    private static string CreateTempDirectory(params string[] parts)
    {
        List<string> pp = new List<string>(parts);
        pp.Insert(0, Path.GetTempPath());
        string[] p = [.. pp];
        string output = Path.Combine(p);
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        Directory.CreateDirectory(output);
        return output;
    }

    [TestMethod("Converts package using RecipeConverter and checks converted.txt")]
    public void ConvertTest()
    {
        CueSheet sheet = CueSheet.Read(Utils.GetFile("CopyingTests", "Spandau Ballet - True.cue"));
        string output = CreateTempDirectory("ConversionTest");
        CuePackage.Convert(sheet, output, null, ".Wav");
        string converted = File.ReadAllText(Utils.GetFile("CopyingTests", "converted.txt"));
        string[] parts = converted.Split(';');
        string old = parts[0];
        string @new = parts[1].Trim();
        Assert.AreEqual(old, Utils.GetFile("CopyingTests", "Spandau Ballet - True.flac"));
        string expectedNew = Path.Combine(output, "Spandau Ballet - True.wav");
        Assert.AreEqual(@new, expectedNew);
    }
}
