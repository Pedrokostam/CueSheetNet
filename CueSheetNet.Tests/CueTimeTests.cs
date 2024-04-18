namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeTests
{
    [Flags]
    public enum Operations
    {
        None,
        Reversed=1,
        Add=2,
        Subtract =4,
        Multiply=8,
        Divide=16,
        Increment=32,
        Decrement =64,
        Negate =128,
    }
    public (string Text, CueTime Time)[] TestTexts { get; set; } = [];
    public string[] FailTexts { get; set; } = [];
    public (object Compared, int Result)[] TestObjectComparisons { get; set; } = [];
    public (object Object, bool CanConvert)[] TestObjectComparisonsThrow { get; set; } = [];
    public (CueTime Time, object Other, Operations Operation)[] OverflowingOperations { get; set; } = [];
    public (long Ticks, int TargetFrames)[] TestTickToFrameConversion { get; set; } = [];
    [TestInitialize]
    public void Initialize()
    {
        TestTickToFrameConversion = [
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
        TestTexts =
        [
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
        FailTexts =
        [
            "",
            //"10",// Is now valid, to comply with Foobar2000 standard
            //"101010",// Is now valid, to comply with Foobar2000 standard - it's just the number of frames
            "477219:0:0",
            "477217:9999:9999"
        ];
        TestObjectComparisons =
        [
             (CueTime.FromMinutes(5),0),
            (TimeSpan.FromMinutes(5),0),
            (TimeSpan.FromMinutes(4.5),1),
            (TimeSpan.FromMinutes(5.5),-1),

             (CueTime.FromSeconds(5*60),0),
            (TimeSpan.FromSeconds(5*60),0),
            (TimeSpan.FromSeconds(4.5*60),1),
            (TimeSpan.FromSeconds(5.5*60),-1),

             (CueTime.FromMilliseconds(5*60000),0),
            (TimeSpan.FromMilliseconds(5*60000),0),
            (TimeSpan.FromMilliseconds(4.5*60000),1),
            (TimeSpan.FromMilliseconds(5.5*60000),-1),

             (CueTime.FromMilliseconds(5.5*60000),0),
            (TimeSpan.FromMilliseconds(4.5*60000),1),
            (TimeSpan.FromMilliseconds(5.5*60000),0),
            (TimeSpan.FromMilliseconds(6.5*60000),-1),
        ];
        TestObjectComparisonsThrow =
        [
            (CueTime.Zero,true),
            (TimeSpan.Zero,true),
            (TimeSpan.FromDays(1.5),true),
            (2137,false),
            (2137D,false),
            (2137F,false),
            (2137M,false),
            ("2137",false),
            (DateTime.Now,false),
#if NET6_0_OR_GREATER
            (DateOnly.FromDayNumber(1),false),
#endif
        ];
        CueTime One = new CueTime(1);
        OverflowingOperations = [
            (CueTime.TheoreticalMax,2,Operations.Add),
            (CueTime.TheoreticalMax,2,Operations.Add|Operations.Reversed),
            (CueTime.TheoreticalMax,One,Operations.Add),
            (CueTime.TheoreticalMax,One,Operations.Add|Operations.Reversed),
            (CueTime.TheoreticalMax,2,Operations.Multiply),
            (CueTime.TheoreticalMax,2,Operations.Multiply|Operations.Reversed),
            (CueTime.TheoreticalMax,2,Operations.Increment),

            (CueTime.ThereoticalMin,2,Operations.Subtract),
            (CueTime.ThereoticalMin,2,Operations.Subtract|Operations.Reversed),
            (CueTime.TheoreticalMax,One,Operations.Subtract),
            (CueTime.TheoreticalMax,One,Operations.Subtract|Operations.Reversed),
            (CueTime.ThereoticalMin,2,Operations.Multiply),
            (CueTime.ThereoticalMin,2,Operations.Multiply|Operations.Reversed),
            (CueTime.ThereoticalMin,2,Operations.Decrement),

            (CueTime.TheoreticalMax,2d,Operations.Add),
            (CueTime.TheoreticalMax,2d,Operations.Add|Operations.Reversed),
            (CueTime.TheoreticalMax,2d,Operations.Multiply),
            (CueTime.TheoreticalMax,2d,Operations.Multiply|Operations.Reversed),
            (CueTime.TheoreticalMax,2d,Operations.Increment),
            (CueTime.ThereoticalMin,2d,Operations.Subtract),
            (CueTime.ThereoticalMin,2d,Operations.Subtract|Operations.Reversed),
            (CueTime.ThereoticalMin,2d,Operations.Multiply),
            (CueTime.ThereoticalMin,2d,Operations.Multiply|Operations.Reversed),
            (CueTime.ThereoticalMin,2d,Operations.Decrement),

            (CueTime.TheoreticalMax,2m,Operations.Add),
            (CueTime.TheoreticalMax,2m,Operations.Add|Operations.Reversed),
            (CueTime.TheoreticalMax,2m,Operations.Multiply),
            (CueTime.TheoreticalMax,2m,Operations.Multiply|Operations.Reversed),
            (CueTime.TheoreticalMax,2m,Operations.Increment),
            (CueTime.ThereoticalMin,2m,Operations.Subtract),
            (CueTime.ThereoticalMin,2m,Operations.Subtract|Operations.Reversed),
            (CueTime.ThereoticalMin,2m,Operations.Multiply),
            (CueTime.ThereoticalMin,2m,Operations.Multiply|Operations.Reversed),
            (CueTime.ThereoticalMin,2m,Operations.Decrement),
            ];
    }
    [TestMethod("Convert CueTime to TimeSpan and back to CueTime")]
    public void RoundTripConversionTest_CtTsCt()
    {
        for (int i = CueTime.Min.TotalFrames; i <= CueTime.Max.TotalFrames; i++)
        {
            CueTime ct = new(i);
            TimeSpan ts = ct;
            CueTime back = (CueTime)ts;
            Assert.AreEqual(back, ct);
        }
    }

    [TestMethod]
    public void TestConversion()
    {
        foreach (var item in TestTickToFrameConversion)
        {
            Assert.AreEqual(item.TargetFrames, CueTime.TicksToFrames(item.Ticks), 0, item.ToString());
            Assert.AreEqual(-item.TargetFrames, CueTime.TicksToFrames(-item.Ticks), 0, item.ToString());
        }
    }

    [TestMethod("TryParse various valid strings")]
    public void TryParseTestString()
    {
        foreach ((string Text, CueTime Time) in TestTexts)
        {
            if (!CueTime.TryParse(Text, out CueTime ct))
                Assert.Fail($"Could not parse {Text}");
            Assert.AreEqual(ct, Time);
        }
    }
    [TestMethod("Parse various valid strings as strings")]
    public void ParseTestString()
    {
        foreach ((string Text, CueTime Time) in TestTexts)
        {
            CueTime ct = CueTime.Parse(Text);
            Assert.AreEqual(ct, Time);
        }
    }
    [TestMethod("Parse various valid strings as ReadOnlySpan<char>")]
    public void ParseTestSpan()
    {
        foreach ((string Text, CueTime Time) in TestTexts)
        {
            CueTime ct = CueTime.Parse(Text.AsSpan());
            Assert.AreEqual(ct, Time);
        }
    }
    [TestMethod("TryParse various valid strings as ReadOnlySpan<char>")]
    public void TryParseTestSpan()
    {
        foreach ((string Text, CueTime Time) in TestTexts)
        {
            var span = Text.AsSpan();
            if (!CueTime.TryParse(span, out CueTime ct))
                Assert.Fail();
            Assert.AreEqual(ct, Time);
        }
    }

    [TestMethod("Fail TryParsing of invalid strings")]
    public void TryParseTestStringFail()
    {
        foreach (var item in FailTexts)
        {
            Assert.IsFalse(CueTime.TryParse(item, out CueTime ct), $"Text {item} parsed successfully ({ct})");
        }
    }
    [TestMethod("Fail TryParsing of invalid strings as ReadOnlySpan<char>")]
    public void TryParseTestSpanFail()
    {
        foreach (var item in FailTexts)
        {
            Assert.IsFalse(CueTime.TryParse(item.AsSpan(), out CueTime ct), $"Text {item} parsed successfully ({ct})");
        }
    }
    [TestMethod("Fail parsing of invalid strings")]
    public void ParseTestStringFail()
    {
        foreach (var item in FailTexts)
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
    }
    [TestMethod("Fail parsing of invalid strings as ReadOnlySpan<char>")]
    public void ParseTestSpanFail()
    {
        foreach (var item in FailTexts)
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
    }
    [TestMethod]
    public void ComparisonTest()
    {
        for (int i = 0; i < TestObjectComparisons.Length; i += 4)
        {
            (object ComparedBase, int ResultBase) = TestObjectComparisons[i];
            CueTime baseCue = (CueTime)ComparedBase;
            Assert.AreEqual(baseCue.CompareTo(ComparedBase), ResultBase);

            for (int j = 1; j < 4; j++)
            {
                (object Compared, int Result) = TestObjectComparisons[i + j];
                var t = (TimeSpan)(Compared);
                Assert.AreEqual(baseCue.CompareTo((CueTime)t), Result);
            }
        }

    }
    [TestMethod]
    public void ComparisonTestThrow()
    {
        CueTime baseTime = CueTime.Zero;
        foreach ((object Object, bool canConvert) in TestObjectComparisonsThrow)
        {
            if (canConvert)
            {
                baseTime.CompareTo(Object);

            }
            else
            {
                Assert.ThrowsException<ArgumentException>(() => baseTime.CompareTo(Object));
            }
        }
    }
    [TestMethod]
    public void CheckedOverflowDetectedTest()
    {
        foreach (var pack in OverflowingOperations)
        {
            var (Time, Other, Operation) = pack;
            Assert.ThrowsException<OverflowException>(() =>
            {
                CheckedOps(Time, Other, Operation);
            }, pack.ToString());
        }
    }
    private static object CheckedOps(CueTime time, object other, Operations operation)
    {
        checked
        {
            if (other is int i)
            {
                switch (operation)
                {
                    case Operations.Add:
                        return time + i;
                    //case Operations.Add | Operations.Reversed:
                    //    return m + time;
                    case Operations.Subtract:
                        return time - i;
                    //case Operations.Subtract | Operations.Reversed:
                    //    return m - time;
                    case Operations.Multiply:
                        return time * i;
                    case Operations.Multiply | Operations.Reversed:
                        return i * time;
                    case Operations.Divide:
                        return time / i;
                    //case Operations.Divide | Operations.Reversed:
                    //    return m / time;
                    case Operations.Increment:
                        time++;
                        return time;
                    case Operations.Decrement:
                        time--;
                        return time;
                    case Operations.Negate:
                        return -time;
                    default:
                        throw new OverflowException();
                }
            }
            else if (other is double d)
            {
                switch (operation)
                {
                    //case Operations.Add:
                    //    return time + m;
                    //case Operations.Add | Operations.Reversed:
                    //    return m + time;
                    //case Operations.Subtract:
                    //    return time - m;
                    //case Operations.Subtract | Operations.Reversed:
                    //    return m - time;
                    case Operations.Multiply:
                        return time * d;
                    case Operations.Multiply | Operations.Reversed:
                        return d * time;
                    case Operations.Divide:
                        return time / d;
                    //case Operations.Divide | Operations.Reversed:
                    //    return m / time;
                    case Operations.Increment:
                        time++;
                        return time;
                    case Operations.Decrement:
                        time--;
                        return time;
                    case Operations.Negate:
                        return -time;
                    default:
                        throw new OverflowException();
                }
            }

            throw new OverflowException();
        }
    }
}