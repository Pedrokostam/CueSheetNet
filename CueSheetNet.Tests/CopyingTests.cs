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
        var files = sourceParent!.EnumerateFiles().ToArray();
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
            using FileStream fs = match.OpenRead();
            var hash = md5.ComputeHash(fs);
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

    private const string SourceSheetName = "Spandau Ballet - True.cue";
    private const string DataFileName = "Spandau Ballet - True.flac";

    private static CueSheet CopyToTemp(string tempName)
    {
        string sourceSheetPath = Utils.GetFile("CopyingTests", SourceSheetName);
        string targetSheetPath = Path.Combine(CreateTempDirectory(tempName), "CopiedSheet.cue");
        return Coppyy(sourceSheetPath, targetSheetPath).Item2;
    }

    [TestMethod]
    public void CopiedFileInfoPointsAtNewLocation()
    {
        var copied = CopyToTemp("CueCopyInfo");
        string expected = Path.Combine(copied.SourceFile!.DirectoryName!, DataFileName);
        Assert.AreEqual(Path.GetFullPath(expected), copied.Files[0].Info!.FullName);
    }

    [TestMethod]
    public void CopyWithRelativePathsCopiesDataFiles()
    {
        // bug: CueExtensions.Copy passes keepPathQualified: true when makePathsRelative is set, then skips qualified files
        var copied = CopyToTemp("CueCopyRelativeSource");
        string target = Path.Combine(CreateTempDirectory("CueCopyRelativeTarget"), "Relative.cue");
        copied.Copy(target, true, true);
        Assert.IsTrue(File.Exists(Path.Combine(Path.GetDirectoryName(target)!, DataFileName)));
    }

    [TestMethod]
    public void CopyToSameDirectoryUnderNewName()
    {
        // bug: CueExtensions.Copy calls File.Copy with Source == Target for every data file
        var copied = CopyToTemp("CueCopySameDir");
        string dir = copied.SourceFile!.DirectoryName!;
        var renamed = copied.Copy(Path.Combine(dir, "Renamed.cue"), true, false);
        Assert.IsTrue(renamed.SourceFile!.Exists);
        Assert.IsTrue(File.Exists(Path.Combine(dir, DataFileName)));
    }

    [TestMethod]
    public void FailedCopyLeavesNoPartialFiles()
    {
        // bug: CueExtensions.Copy has no rollback; data files stay behind when a later File.Copy throws
        var copied = CopyToTemp("CueCopyPartialSource");
        string targetDir = CreateTempDirectory("CueCopyPartialTarget");
        File.WriteAllText(Path.Combine(targetDir, "cover.png"), "occupied");
        try
        {
            copied.Copy(Path.Combine(targetDir, "CopiedSheet.cue"), true, false);
        }
        catch (IOException)
        {
            Assert.IsFalse(File.Exists(Path.Combine(targetDir, DataFileName)), "Data file left behind after failed copy");
            return;
        }
        Assert.Fail("Expected the copy to fail on the existing cover.png");
    }

    [TestMethod]
    public void MoveOntoItselfKeepsSheet()
    {
        // bug: CueExtensions.Move copies onto itself, then Unlink deletes the only copy
        var copied = CopyToTemp("CueMoveSelf");
        string path = copied.SourceFile!.FullName;
        try
        {
            copied.Move(path, true, false);
        }
        catch (Exception)
        {
        }
        Assert.IsTrue(File.Exists(path), "Cue sheet deleted");
        Assert.IsTrue(File.Exists(Path.Combine(Path.GetDirectoryName(path)!, DataFileName)), "Data file deleted");
    }

    [TestMethod]
    public void UnlinkKeepsFilesSharedWithOtherSheet()
    {
        // bug: AdditionalFileHelper treats cover.* as belonging to every sheet in the folder; debatable expectation
        var copied = CopyToTemp("CueUnlinkShared");
        string dir = copied.SourceFile!.DirectoryName!;
        File.Copy(copied.SourceFile.FullName, Path.Combine(dir, "Other.cue"));
        copied.Unlink(true, false);
        Assert.IsTrue(File.Exists(Path.Combine(dir, "cover.png")));
    }

    [TestMethod]
    public void CopyRecreatesSubdirectories()
    {
        string sourceDir = CreateTempDirectory("CueSubdirSource");
        Directory.CreateDirectory(Path.Combine(sourceDir, "CD1"));
        File.WriteAllText(Path.Combine(sourceDir, "CD1", "a.wav"), "data");
        string sourceSheet = Path.Combine(sourceDir, "Sub.cue");
        File.WriteAllText(sourceSheet, """
            FILE "CD1/a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
            """);
        string targetDir = CreateTempDirectory("CueSubdirTarget");
        CueSheet.Parse(sourceSheet).Copy(Path.Combine(targetDir, "Sub.cue"), false, false);
        Assert.IsTrue(File.Exists(Path.Combine(targetDir, "CD1", "a.wav")));
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
