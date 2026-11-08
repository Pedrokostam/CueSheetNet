using Microsoft.VisualStudio.TestTools.UnitTesting;
using Metalama.Framework;
using System.ComponentModel;
using System.Reflection;
using static CueSheetNet.Tests.CueTimeTests;
[assembly: TestDataSourceDiscovery(TestDataSourceDiscoveryOption.DuringExecution)]

namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeTests
{
    [Flags]
    public enum Operations
    {
        None,
        Reversed = 1,
        Add = 2,
        Subtract = 4,
        Multiply = 8,
        Divide = 16,
        Increment = 32,
        Decrement = 64,
        Negate = 128,
    }
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
    static public string[][] FailTexts { get; set; } = [
            [""],
            //"10",// Is now valid, to comply with Foobar2000 standard
            //"101010",// Is now valid, to comply with Foobar2000 standard - it's just the number of frames
            ["477219:0:0"],
            ["477217:9999:9999"]
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
    public static (CueTime Time, object Other, Operations Operation)[] OverflowingOperations { get; } = [
            (CueTime.MaxValue,new(),Operations.Increment),
            (CueTime.MinValue,new(),Operations.Decrement),
            (CueTime.MinValue,new(),Operations.Negate),

            (CueTime.MaxValue,CueTime.One,Operations.Add),
            (CueTime.MaxValue,CueTime.One,Operations.Add|Operations.Reversed),
            (CueTime.MinValue,CueTime.One,Operations.Subtract),
            (CueTime.Zero,CueTime.MinValue,Operations.Subtract),
            (CueTime.MaxValue,new CueTime(-2),Operations.Subtract|Operations.Reversed),

            (CueTime.MaxValue,1,Operations.Add),
            (CueTime.MaxValue,1,Operations.Add|Operations.Reversed),
            (CueTime.MaxValue,2,Operations.Multiply),
            (CueTime.MaxValue,2,Operations.Multiply|Operations.Reversed),

            (CueTime.MinValue,1,Operations.Subtract),
            (CueTime.MinValue,1,Operations.Subtract|Operations.Reversed),
            (CueTime.MinValue,2,Operations.Multiply),
            (CueTime.MinValue,2,Operations.Multiply|Operations.Reversed),

            (CueTime.MaxValue,1.1d,Operations.Multiply),
            (CueTime.MaxValue,1.1d,Operations.Multiply|Operations.Reversed),
            (CueTime.MinValue,1.1d,Operations.Multiply),
            (CueTime.MinValue,1.1d,Operations.Multiply|Operations.Reversed),

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
            Assert.AreEqual(back, ct);
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
        Assert.AreEqual(ct, time);
    }
    [DataTestMethod("Parse various valid strings as strings")]
    [DynamicData(nameof(TestTexts))]
    public void ParseTestString(string text, CueTime time)
    {
        CueTime ct = CueTime.Parse(text);
        Assert.AreEqual(ct, time);
    }
    [DataTestMethod("Parse various valid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(TestTexts))]
    public void ParseTestSpan(string text, CueTime time)
    {
        CueTime ct = CueTime.Parse(text.AsSpan());
        Assert.AreEqual(ct, time);
    }
    [DataTestMethod("TryParse various valid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(TestTexts))]
    public void TryParseTestSpan(string text, CueTime time)
    {
        var span = text.AsSpan();
        if (!CueTime.TryParse(span, out CueTime ct))
            Assert.Fail();
        Assert.AreEqual(ct, time);
    }

    [DataTestMethod("Fail TryParsing of invalid strings")]
    [DynamicData(nameof(FailTexts))]
    public void TryParseTestStringFail(string item)
    {
        Assert.IsFalse(CueTime.TryParse(item, out CueTime ct), $"Text {item} parsed successfully ({ct})");
    }
    [DataTestMethod("Fail TryParsing of invalid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(FailTexts))]
    public void TryParseTestSpanFail(string item)
    {
        Assert.IsFalse(CueTime.TryParse(item.AsSpan(), out CueTime ct), $"Text {item} parsed successfully ({ct})");
    }
    [DataTestMethod("Fail parsing of invalid strings")]
    [DynamicData(nameof(FailTexts))]
    public void ParseTestStringFail(string item)
    {
        try
        {
            CueTime.Parse(item);
            Assert.Fail();
        }
        catch (Exception)
        {
        }
    }
    [DataTestMethod("Fail parsing of invalid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(FailTexts))]
    public void ParseTestSpanFail(string item)
    {
        try
        {
            CueTime.Parse(item.AsSpan());
            Assert.Fail();
        }
        catch (Exception)
        {
        }
    }
    [DataTestMethod]
    [DynamicData(nameof(TestObjectComparisons))]
    public void ComparisonTest(CueTime time,(TimeSpan ts, int res)[] timespans)
    {
        foreach (var (ts, res) in timespans)
        {
            Assert.AreEqual(time.CompareTo(ts), res,$"{time} - {ts.Ticks}");
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
    public void CheckedOverflowDetectedTest2(CueTime time, object other, Operations operation)
    {
        Assert.ThrowsException<OverflowException>(() =>
        {
            CheckedOps(time, other, operation);
        }, $"{time} - {other?.GetType().Name ?? null} {other} - {operation}");
    }
    private static object CheckedOps(CueTime time, object? other, Operations operation)
    {
        checked
        {
            switch (operation)
            {
                case Operations.Increment:
                    time++;
                    return time;
                case Operations.Decrement:
                    time--;
                    return time;
                case Operations.Negate:
                    return -time;
                default:
                    break;
            }
            if (other is int i)
            {
                switch (operation)
                {
                    case Operations.Add:
                        return time + i;
                    case Operations.Add | Operations.Reversed:
                        return new CueTime(i) + time;
                    case Operations.Subtract:
                        return time - i;
                    case Operations.Subtract | Operations.Reversed:
                        return new CueTime(i) - time;
                    case Operations.Multiply:
                        return time * i;
                    case Operations.Multiply | Operations.Reversed:
                        return i * time;
                    case Operations.Divide:
                        return time / i;
                    //case Operations.Divide | Operations.Reversed:
                    default:
                        throw new InvalidEnumArgumentException($"Int operation {operation} not supported");
                }
            }
            else if (other is double d)
            {
                switch (operation)
                {
                    //case Operations.Add:
                    //case Operations.Add | Operations.Reversed:
                    //case Operations.Subtract:
                    //case Operations.Subtract | Operations.Reversed:
                    case Operations.Multiply:
                        return time * d;
                    case Operations.Multiply | Operations.Reversed:
                        return d * time;
                    case Operations.Divide:
                        return time / d;
                    //case Operations.Divide | Operations.Reversed:
                    default:
                        throw new InvalidEnumArgumentException($"Double operation {operation} not supported");
                }
            }
            else if (other is CueTime m)
            {
                switch (operation)
                {
                    case Operations.Add:
                        return time + m;
                    case Operations.Add | Operations.Reversed:
                        return m + time;
                    case Operations.Subtract:
                        return time - m;
                    case Operations.Subtract | Operations.Reversed:
                        return m - time;
                    //case Operations.Multiply:
                    //case Operations.Multiply | Operations.Reversed:
                    //case Operations.Divide:
                    //case Operations.Divide | Operations.Reversed:
                    default:
                        throw new InvalidEnumArgumentException($"Double operation {operation} not supported");
                }
            }

            throw new InvalidOperationException($"{other.GetType().Name} {other} not supported");
        }
    }
}