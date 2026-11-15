using CueSheetNet.Components;
using CueSheetNet.Syntax;
using System.Reflection;

namespace CueSheetNet.Tests;

[TestClass]
public class StructureTests
{
    private static CueSheet Load(string folder, string name) => CueReader.CreateFromFile(Utils.GetFile(folder, name)).Parse();

    private static CueSheet NewCue()
    {
        var cue = new CueSheet();
        cue.InsertFile(0, "Tescik.mp3", "WAVE", null);
        cue.InsertTrack(cue.Files[^1], new Components.TrackInsertParameters("AUDIO"), CueTime.One);
        return cue;
    }

    private void TestReflectionStrings<T>(Func<CueSheet, T> getter, Func<CueSheet, CueSheet> cloning)
    {
        var props = typeof(T).GetProperties().Where(x => x.PropertyType == typeof(string));
        foreach (var item in props)
        {
            var cue = NewCue();
            item.SetValue(getter(cue), "TEST TEST");
            var clone = cloning(cue);
            Assert.AreEqual(item.GetValue(getter(cue)), item.GetValue(getter(clone)), $"Reflection property {item.Name} of {typeof(T).Name}");
        }
    }
    [TestInitialize]
    public void Init()
    {
        SheetPropertyInfos = typeof(CueSheet).GetProperties();
        TrackPropertyInfos = typeof(CueTrack).GetProperties();
    }
    static PropertyInfo[] SheetPropertyInfos = [];
    static PropertyInfo[] TrackPropertyInfos = [];

    public static (string Name, object Value)[] TrackProps { get; } = [
            ("Performer", "TEST TEST"),
            ("Songwriter","TEST TEST"),
            ("Isrc","TEST TEST"),
            ("Title","TEST TEST"),
            ("Pregap",CueTime.One),
            ("Postgap",CueTime.One),
            ("Type",TrackType.MODE2_2336),
            ("Flags",TrackFlags.PreEmphasis|TrackFlags.DigitalCopyPermitted|TrackFlags.FourChannel|TrackFlags.SerialCopyManagementSystem),
    ];

    public static (string Name, object Value)[] SheetProps { get; }= [
            ("Performer", "TEST TEST"),
            ("Songwriter","TEST TEST"),
            ("Catalog","TEST TEST"),
            ("Title","TEST TEST"),
    ];
    [DynamicData(nameof(TrackProps))]
    [DataTestMethod]
    public void TestTrackPropertiesPreservedCloned(string name, object value)
    {
        var cue = NewCue();
        var prop = TrackPropertyInfos.First(x => x.Name == name);
        prop.SetValue(cue.Tracks[0], value);
        prop.SetValue(cue.Tracks[1], value);
        var clone = cue.Clone();
        Assert.AreEqual(prop.GetValue(cue.Tracks[0]), prop.GetValue(clone.Tracks[0]), $"Reflection property {prop.Name} of CueSheet, track 1");
        Assert.AreEqual(prop.GetValue(cue.Tracks[1]), prop.GetValue(clone.Tracks[1]), $"Reflection property {prop.Name} of CueSheet, track 2");
    }
    [DynamicData(nameof(TrackProps))]
    [DataTestMethod]
    public void TestTrackPropertiesPreservedWritten(string name, object value)
    {
        var cue = NewCue();
        var prop = TrackPropertyInfos.First(x => x.Name == name);
        prop.SetValue(cue.Tracks[0], value);
        prop.SetValue(cue.Tracks[1], value);
        var clone = CueSheet.ParseFromString(cue.WriteToString());
        Assert.AreEqual(prop.GetValue(cue.Tracks[0]), prop.GetValue(clone.Tracks[0]), $"Reflection property {prop.Name} of CueSheet, track 1");
        Assert.AreEqual(prop.GetValue(cue.Tracks[1]), prop.GetValue(clone.Tracks[1]), $"Reflection property {prop.Name} of CueSheet, track 2");
    }

    [DynamicData(nameof(SheetProps))]
    [DataTestMethod]
    public void TestSheetPropertiesPreservedCloned(string name, object value)
    {
        var cue = NewCue();
        var prop = SheetPropertyInfos.First(x => x.Name == name);
        prop.SetValue(cue, value);
        var clone = cue.Clone();
        Assert.AreEqual(prop.GetValue(cue), prop.GetValue(clone), $"Reflection property {prop.Name} of CueSheet");
    }
    [DynamicData(nameof(SheetProps))]
    [DataTestMethod]
    public void TestSheetPropertiesPreservedWritten(string name, object value)
    {
        var cue = NewCue();
        var prop = SheetPropertyInfos.First(x => x.Name == name);
        prop.SetValue(cue, value);
        var clone = CueSheet.ParseFromString(cue.WriteToString());
        Assert.AreEqual(prop.GetValue(cue), prop.GetValue(clone), $"Reflection property {prop.Name} of CueSheet");
    }

    [TestMethod]
    public void TestReflectionPropertiesPreservedWritten()
    {
        TestReflectionStrings(x => x, x => CueSheet.ParseFromString(x.WriteToString()));
        TestReflectionStrings(x => x.Tracks[0], x => CueSheet.ParseFromString(x.WriteToString()));
    }

    [TestMethod]
    public void TestReflectionPropertiesPreservedCloned()
    {
        TestReflectionStrings(x => x, x => x.Clone());
        TestReflectionStrings(x => x.Tracks[0], x => x.Clone());
    }

    [TestMethod]
    public void NonConsecutiveTrackNumbersArePreserved()
    {
        var cue = Load("Structure", "NonConsecutive.cue");
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, cue.Tracks.Select(t => t.Position).ToArray());

        var reloaded = cue.Clone();
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, reloaded.Tracks.Select(t => t.Position).ToArray());
    }

    [TestMethod]
    public void MultiFileEacLayout()
    {
        // Track 2 and 3 start with INDEX 00 at the end of the previous file (EAC "gaps appended to previous track").
        var cue = Load("SavingTests", "MultiFile.cue");
        var q = cue.Tracks[0].Indices;

        Assert.AreEqual(3, cue.Tracks.Count);
        Assert.AreEqual(3, cue.Files.Count);
        Assert.AreEqual(5, cue.Indices.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cue.Tracks.Select(t => t.Position).ToArray());

        CollectionAssert.AreEqual(new[] { false, true, false, true, false }, cue.Indices.Select(i => i.IsSplit).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1 }, cue.Indices.Select(i => i.Position).ToArray());

        CollectionAssert.AreEqual(new[] { false, true, true }, cue.Tracks.Select(t => t.IsEacStyle).ToArray());
        // Split indices do not belong to any track
        CollectionAssert.AreEqual(new[] { false, false, false }, cue.Tracks.Select(t => t.HasZerothIndex).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 1, 1 }, cue.Tracks.Select(t => t.Indices.Count).ToArray());

        // A track belongs to the file holding its first non-split index.
        CollectionAssert.AreEqual(
            new[] { "MultiFile - 0 - Uno.wav", "MultiFile - 1 - Dos.wav", "MultiFile - 2 - Tres.wav" },
            cue.Tracks.Select(t => t.File.Name).ToArray());
    }
}
