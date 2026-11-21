using CueSheetNet.Components;
using CueSheetNet.Syntax;
using System.Reflection;

namespace CueSheetNet.Tests;

[TestClass]
public class PropertyPreservationTests
{
    private void TestReflectionStrings<T>(Func<CueSheet, T> getter, Func<CueSheet, CueSheet> cloning)
    {
        var props = typeof(T).GetProperties().Where(x => x.PropertyType == typeof(string));
        foreach (var item in props)
        {
            var cue = TestSheets.NewCue();
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
        var cue = TestSheets.NewCue();
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
        var cue = TestSheets.NewCue();
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
        var cue = TestSheets.NewCue();
        var prop = SheetPropertyInfos.First(x => x.Name == name);
        prop.SetValue(cue, value);
        var clone = cue.Clone();
        Assert.AreEqual(prop.GetValue(cue), prop.GetValue(clone), $"Reflection property {prop.Name} of CueSheet");
    }
    [DynamicData(nameof(SheetProps))]
    [DataTestMethod]
    public void TestSheetPropertiesPreservedWritten(string name, object value)
    {
        var cue = TestSheets.NewCue();
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
}
