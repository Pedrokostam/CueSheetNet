using Microsoft.VisualStudio.TestTools.UnitTesting;
using Metalama.Framework;
using System.Reflection;
[assembly: TestDataSourceDiscovery(TestDataSourceDiscoveryOption.DuringExecution)]

namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeTests
{
    static public (string Text, CueTime Time)[] TestTexts { get; set; } = [
             ("00:00:00",        new(0)),
            ("-00:00:00",       new(0)),
            ("0:0:0",           new(0)),
            ("50:360:750",      new(252750)),
            ("10:10:10",        new(10,10,10)),
            ("-10:10:10",       new(-10,-10,-10)),
            ("99:59:74",        new(99,59,74)),
            ("99:000059:00074", new(99,59,74)),
            ("99:     59:74",   new(99,59,74)),
            ("00:00:01",        new(0,0,1)),
            ("00:     00:01",   new(0,0,1)),
            ("50:360:750",      new(50,360,750)),
            ("9999:360:750",      new(9999,360,750)),
        ];
    static public object[][] FailTexts { get; set; } = [
            ["", typeof(ArgumentException)],
            //"10",// Is now valid, to comply with Foobar2000 standard
            //"101010",// Is now valid, to comply with Foobar2000 standard - it's just the number of frames
            ["477219:0:0", typeof(OverflowException)],
            ["477217:9999:9999", typeof(OverflowException)]
        ];
    private static CueTime Cm(double minutes) => CueTime.FromMinutes(minutes);
    private static TimeSpan Ts(double minutes) => TimeSpan.FromMinutes(minutes);
    static public (CueTime Ct, (TimeSpan Ts, int Result)[])[] TestObjectComparisons { get; set; } = [
        (CueTime.FromMinutes(5),[(TimeSpan.FromMinutes(5),0), (TimeSpan.FromMinutes(4.5),1), (TimeSpan.FromMinutes(5.5),-1)]),

        (CueTime.FromSeconds(5*60),[(TimeSpan.FromSeconds(5*60),0), (TimeSpan.FromSeconds(4.5*60),1), (TimeSpan.FromSeconds(5.5*60),-1)]),

        (CueTime.FromMilliseconds(5*60000),[(TimeSpan.FromMilliseconds(5* 60000),0), (TimeSpan.FromMilliseconds(4.5* 60000),1), (TimeSpan.FromMilliseconds(5.5* 60000),-1)]),

        (CueTime.FromMilliseconds(5.5*60000),[(TimeSpan.FromMilliseconds(5* 60000),1), (TimeSpan.FromMilliseconds(5.5* 60000),0), (TimeSpan.FromMilliseconds(6.5* 60000),-1)]),
        ];

    static public (object Object, bool CanConvert)[] TestObjectComparisonsThrow { get; set; } = [
                (CueTime.Zero,true),
            (TimeSpan.Zero,true),
            (TimeSpan.FromDays(1.5),true),
            (2137,false),
            (2137D,false),
            (2137F,false),
            (2137M,false),
            ("2137",false),
            (DateTime.Now,false),
            (DateOnly.FromDayNumber(1),false),
        ];
    public static object[][] OverflowingOperations { get; } = [
            ["Max++", () => { var t = CueTime.MaxValue; return ++t; }],
            ["Min--", () => { var t = CueTime.MinValue; return --t; }],
            ["-Min", () => -CueTime.MinValue],

            ["Max + One", () => CueTime.MaxValue + CueTime.One],
            ["One + Max", () => CueTime.One + CueTime.MaxValue],
            ["Min - One", () => CueTime.MinValue - CueTime.One],
            ["Zero - Min", () => CueTime.Zero - CueTime.MinValue],
            ["-2 - Max", () => new CueTime(-2) - CueTime.MaxValue],

            ["Max + 1", () => CueTime.MaxValue + 1],
            ["1 + Max", () => new CueTime(1) + CueTime.MaxValue],
            ["Max * 2", () => CueTime.MaxValue * 2],
            ["2 * Max", () => 2 * CueTime.MaxValue],

            ["Min - 1", () => CueTime.MinValue - 1],
            ["1 - Min", () => new CueTime(1) - CueTime.MinValue],
            ["Min * 2", () => CueTime.MinValue * 2],
            ["2 * Min", () => 2 * CueTime.MinValue],

            ["Max * 1.1", () => CueTime.MaxValue * 1.1d],
            ["1.1 * Max", () => 1.1d * CueTime.MaxValue],
            ["Min * 1.1", () => CueTime.MinValue * 1.1d],
            ["1.1 * Min", () => 1.1d * CueTime.MinValue],
            ];
    static public (long Ticks, int TargetFrames)[] TestTickToFrameConversion { get; set; } = [
            (10000000, 75), // 1 second
            (20000000, 150), // 2 seconds
            (5000000, 38), // 0.5 second
            (3333333, 25), // 1/3 second
            (3333332, 25), // 1/3 second - 1 tick
            (3333334, 25), // 1/3 second + 1 tick
            (133333, 1), // 1/75 second
            (133332, 1), // 1/75 second - 1 tick
            (133334, 1), // 1/75 second +- 1 tick
            (66665, 0),  // 1/150 second rounded down - 1 tick
            (66666, 0), // 1/150 second rounded down
            (66667, 1), // 1/150 second rounded down + 1 tick
            (266666, 2), // 2/75 second
            (400000, 3), // 3/75 second
            (533333, 4), // 4/75 second
            ];

    [TestMethod("Convert CueTime to TimeSpan and back to CueTime")]
    public void RoundTripConversionTest_CtTsCt()
    {
        for (int i = CueTime.StandardCueSheetMinValue.TotalFrames; i <= CueTime.StandardCueSheetMaxValue.TotalFrames; i++)
        {
            CueTime ct = new(i);
            TimeSpan ts = ct;
            CueTime back = (CueTime)ts;
            Assert.AreEqual(ct, back);
            Assert.AreEqual(0, ct.CompareTo(ts), $"{ct}");
        }
    }

    [DataTestMethod]
    [DynamicData(nameof(TestTickToFrameConversion))]
    public void TestConversion(long ticks, int targetFrames)
    {
        Assert.AreEqual(targetFrames, CueTime.TicksToFrames(ticks), 0);
        Assert.AreEqual(-targetFrames, CueTime.TicksToFrames(-ticks), 0);
    }

    [TestMethod("TryParse various valid strings")]
    [DynamicData(nameof(TestTexts))]
    public void TryParseTestString(string text, CueTime time)
    {
        if (!CueTime.TryParse(text, out CueTime ct))
            Assert.Fail($"Could not parse {text}");
        Assert.AreEqual(time, ct);
    }
    [DataTestMethod("Parse various valid strings as strings")]
    [DynamicData(nameof(TestTexts))]
    public void ParseTestString(string text, CueTime time)
    {
        CueTime ct = CueTime.Parse(text);
        Assert.AreEqual(time, ct);
    }
    [DataTestMethod("Parse various valid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(TestTexts))]
    public void ParseTestSpan(string text, CueTime time)
    {
        CueTime ct = CueTime.Parse(text.AsSpan());
        Assert.AreEqual(time, ct);
    }
    [DataTestMethod("TryParse various valid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(TestTexts))]
    public void TryParseTestSpan(string text, CueTime time)
    {
        var span = text.AsSpan();
        if (!CueTime.TryParse(span, out CueTime ct))
            Assert.Fail();
        Assert.AreEqual(time, ct);
    }

    [DataTestMethod("Fail TryParsing of invalid strings")]
    [DynamicData(nameof(FailTexts))]
    public void TryParseTestStringFail(string item, Type _)
    {
        Assert.IsFalse(CueTime.TryParse(item, out CueTime ct), $"Text {item} parsed successfully ({ct})");
    }
    [DataTestMethod("Fail TryParsing of invalid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(FailTexts))]
    public void TryParseTestSpanFail(string item, Type _)
    {
        Assert.IsFalse(CueTime.TryParse(item.AsSpan(), out CueTime ct), $"Text {item} parsed successfully ({ct})");
    }
    [DataTestMethod("Fail parsing of invalid strings")]
    [DynamicData(nameof(FailTexts))]
    public void ParseTestStringFail(string item, Type expected)
    {
        Assert.AreEqual(expected, ThrownType(() => CueTime.Parse(item)), item);
    }
    [DataTestMethod("Fail parsing of invalid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(FailTexts))]
    public void ParseTestSpanFail(string item, Type expected)
    {
        Assert.AreEqual(expected, ThrownType(() => CueTime.Parse(item.AsSpan())), item);
    }
    private static Type? ThrownType(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            return e.GetType();
        }
        return null;
    }
    [DataTestMethod]
    [DynamicData(nameof(TestObjectComparisons))]
    public void ComparisonTest(CueTime time,(TimeSpan ts, int res)[] timespans)
    {
        foreach (var (ts, res) in timespans)
        {
            Assert.AreEqual(res, time.CompareTo(ts),$"{time} - {ts.Ticks}");
        }
    }
    [DataTestMethod]
    [DynamicData(nameof(TestObjectComparisonsThrow))]
    public void ComparisonTestThrow(object obj, bool canConvert)
    {
        CueTime baseTime = CueTime.Zero;
        if (canConvert)
        {
            baseTime.CompareTo(obj);
        }
        else
        {
            Assert.ThrowsException<ArgumentException>(() => baseTime.CompareTo(obj));
        }
    }
   
    [DataTestMethod]
    [DynamicData(nameof(OverflowingOperations))]
    public void CheckedOverflowDetectedTest2(string name, Func<CueTime> operation)
    {
        Assert.ThrowsException<OverflowException>(() => operation(), name);
    }
}
