namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeTests
{
    public (string Text, CueTime Time)[] TestTexts { get; set; } = [];
    public string[] FailTexts { get; set; } = [];
    public (object Compared, int Result)[] TestObjectComparisons { get; set; } = [];
    public (object Object, Type Type)[] TestObjectComparisonsThrow { get; set; } = [];
    [TestInitialize]
    public void Initialize()
    {
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
            (CueTime.Zero,typeof(CueTime)),
            (2137,typeof(int)),
            (2137D,typeof(double)),
            (2137F,typeof(float)),
            (2137M,typeof(decimal)),
            ("2137",typeof(string)),
            (DateTime.Now,typeof(DateTime)),
#if NET6_0_OR_GREATER
            (DateOnly.FromDayNumber(1),typeof(DateOnly)),
#endif
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
    
    [TestMethod("TryParse various valid strings")]
    public void TryParseTestString()
    {
        foreach ((string Text, CueTime Time) in TestTexts)
        {
            if (!CueTime.TryParse(Text, out CueTime ct)) Assert.Fail($"Could not parse {Text}");
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
            if (!CueTime.TryParse(span, out CueTime ct)) Assert.Fail();
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
        foreach ((object Object, Type _type) in TestObjectComparisonsThrow)
        {
            dynamic xD = Convert.ChangeType(Object, _type);
            if (_type == typeof(CueTime))
            {
                baseTime.CompareTo(xD);

            }
            else
            {
                Assert.ThrowsException<InvalidCastException>(() => baseTime.CompareTo(xD));
            }
        }

    }
}