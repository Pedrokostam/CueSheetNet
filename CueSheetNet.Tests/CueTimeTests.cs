namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeTests
{
    [TestMethod("Convert CueTime to TimeSpan and back to CueTime")]
    public void RoundTripConversionTest()
    {
        for (int i = -99000; i <= 99000; i++)
        {
            CueTime ct = new(i);
            TimeSpan ts = ct;
            CueTime back = (CueTime)ts;
           Assert.AreEqual(back, ct);
        }
    }
    public (string Text, CueTime Time)[] TestTexts = new (string Text, CueTime Time)[] {
    ("00:00:00",new(0)),
    ("-00:00:00",new(0)),
    ("10:10:10",new(10,10,10)),
    ("-10:10:10",new(-10,-10,-10)),
    ("99:59:74",new(99,59,74)),
    ("99:000059:00074",new(99,59,74)),
    ("99:     59:74",new(99,59,74)),
    ("0:0:0",new(0)),
    ("00:00:01",new(0,0,1)),
    ("00:     00:01",new(0,0,1)),
    };
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
    public string[] FailTexts = new string[]
    {
        "",
        "10:70:90",
        "477219:0:0",
        "477217:9999:9999"
    };
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
    
}