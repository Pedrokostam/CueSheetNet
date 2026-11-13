using CueSheetNet.Components;
using CueSheetNet.FileHandling;
using System.Net.WebSockets;
using System.Security.Cryptography;

namespace CueSheetNet.Tests;

[TestClass]
public class CopyingTests
{
    private static (Dictionary<string,byte[]>,CueSheet) Coppyy(string sourceSheetPath, string targetSheetPath)
    {
        var cue = CueReader.CreateFromFile(sourceSheetPath).Parse();
        using var md5 = MD5.Create();
        var sourceParent = Directory.GetParent(sourceSheetPath);
        var files = sourceParent.EnumerateFiles().ToArray();
        Dictionary<string, byte[]> hashes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (file.Extension.Equals(".cue", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            using FileStream fs = file.OpenRead();
            hashes.Add(file.Name, md5.ComputeHash(fs));
        }
        var targetDir = Directory.GetParent(targetSheetPath)!;
        targetDir.Create();
        foreach (var exFile in targetDir.EnumerateFiles())
        {
            exFile.Delete();
        }
        var copied = cue.Copy(targetSheetPath, true, false);
        var newFiles = targetDir.EnumerateFiles().ToArray();
        foreach (var item in hashes)
        {
            Assert.IsTrue(newFiles.Any(x => x.Name == item.Key),$"Missing copied item: {item.Key}");
            var match = newFiles.First(x=>x.Name == item.Key);
            var hash = md5.ComputeHash(match.OpenRead());
            Assert.IsTrue(item.Value.SequenceEqual(hash), $"Different file hash: {item.Key}");
        }
        return (hashes, copied);
    }

    [TestMethod("Copies sheet package and compares hashes of non-cue files")]
    public void Copy()
    {
        string sourceSheetPath = Utils.GetFile("CopyingTests", "Spandau Ballet - True.cue");
        string targetSheetPath = Path.Combine(CreateTempDirectory("CueCopying"), "CopiedSheet.cue");
        Coppyy(sourceSheetPath, targetSheetPath);
    }

    [TestMethod("Moved a copied sheet and compares file hashes")]
    public void Move()
    {
        using var md5 = MD5.Create();
        string sourceSheetPath = Utils.GetFile("CopyingTests", "Spandau Ballet - True.cue");
        string targetSheetPath = Path.Combine(CreateTempDirectory("CueCopying2"), "CopiedSheet.cue");
        string moveTargetSheetPath = Path.Combine(CreateTempDirectory("CueMoving"), "MovedSheet.cue");
        (var hashes, var copied) = Coppyy(sourceSheetPath, targetSheetPath);
        var moved = copied.Move(moveTargetSheetPath, true, false);
        foreach (var deadFile in copied.AdditionalFiles)
        {
            deadFile.Info?.Refresh();
            Assert.IsFalse(deadFile.Exists);
        }
        foreach (var deadFile in copied.Files)
        {
            deadFile.Info?.Refresh();
            Assert.IsFalse(deadFile.Exists);
        }
        foreach (var file in moved.SourceFile!.Directory!.EnumerateFiles())
        {
            if (file.Extension.Equals(".cue", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            using FileStream fs = file.OpenRead();
            var hash = md5.ComputeHash(fs);
            Assert.IsTrue(hashes.ContainsKey(file.Name));
            Assert.IsTrue(hashes[Path.GetFileName(file.Name)].SequenceEqual(hash));
        }
    }

    [TestMethod("Deletes copied sheet")]
    public void Unlink()
    {
        using var md5 = MD5.Create();
        string sourceSheetPath = Utils.GetFile("CopyingTests", "Spandau Ballet - True.cue");
        string targetSheetPath = Path.Combine(CreateTempDirectory("CueCopying3"), "CopiedSheet.cue");
        (var hashes, var copied) = Coppyy(sourceSheetPath, targetSheetPath);
        copied.Unlink( true, false);
        foreach (var deadFile in copied.AdditionalFiles)
        {
            deadFile.Info?.Refresh();
            Assert.IsFalse(deadFile.Exists);
        }
        foreach (var deadFile in copied.Files)
        {
            deadFile.Info?.Refresh();
            Assert.IsFalse(deadFile.Exists);
        }
    }

    private static string CreateTempDirectory(params string[] parts)
    {
        List<string> pp = ["TestItems", "TempData", .. parts];
        string[] p = [.. pp];
        string output = Path.Combine(p);
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        Directory.CreateDirectory(output);
        return output;
    }

    //[TestMethod("Converts package using RecipeConverter and checks converted.txt")]
    //public void ConvertTest()
    //{

    //    CueSheet sheet = CueSheet.Parse(Utils.GetFile("CopyingTests", "Spandau Ballet - True.cue"));
    //    string output = CreateTempDirectory("ConversionTest");
    //    CuePackage.Convert(sheet, output, null, ".Wav");
    //    string converted = File.ReadAllText(Utils.GetFile("CopyingTests", "converted.txt"));
    //    string[] parts = converted.Split(';');
    //    string old = parts[0];
    //    string @new = parts[1].Trim();
    //    Assert.AreEqual(old, Utils.GetFile("CopyingTests", "Spandau Ballet - True.flac"));
    //    string expectedNew = Path.Combine(output, "Spandau Ballet - True.wav");
    //    Assert.AreEqual(@new, expectedNew);
    //}
}
