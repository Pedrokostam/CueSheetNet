namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeParsingTests
{
    [TestMethod("TryParse various valid strings")]
    [DynamicData(nameof(CueTimeTestData.TestTexts), typeof(CueTimeTestData))]
    public void TryParseTestString(string text, CueTime time)
    {
        if (!CueTime.TryParse(text, out CueTime ct))
            Assert.Fail($"Could not parse {text}");
        Assert.AreEqual(time, ct);
    }
    [DataTestMethod("Parse various valid strings as strings")]
    [DynamicData(nameof(CueTimeTestData.TestTexts), typeof(CueTimeTestData))]
    public void ParseTestString(string text, CueTime time)
    {
        CueTime ct = CueTime.Parse(text);
        Assert.AreEqual(time, ct);
    }
    [DataTestMethod("Parse various valid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(CueTimeTestData.TestTexts), typeof(CueTimeTestData))]
    public void ParseTestSpan(string text, CueTime time)
    {
        CueTime ct = CueTime.Parse(text.AsSpan());
        Assert.AreEqual(time, ct);
    }
    [DataTestMethod("TryParse various valid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(CueTimeTestData.TestTexts), typeof(CueTimeTestData))]
    public void TryParseTestSpan(string text, CueTime time)
    {
        var span = text.AsSpan();
        if (!CueTime.TryParse(span, out CueTime ct))
            Assert.Fail();
        Assert.AreEqual(time, ct);
    }

    [DataTestMethod("Fail TryParsing of invalid strings")]
    [DynamicData(nameof(CueTimeTestData.FailTexts), typeof(CueTimeTestData))]
    public void TryParseTestStringFail(string item, Type _)
    {
        Assert.IsFalse(CueTime.TryParse(item, out CueTime ct), $"Text {item} parsed successfully ({ct})");
    }
    [DataTestMethod("Fail TryParsing of invalid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(CueTimeTestData.FailTexts), typeof(CueTimeTestData))]
    public void TryParseTestSpanFail(string item, Type _)
    {
        Assert.IsFalse(CueTime.TryParse(item.AsSpan(), out CueTime ct), $"Text {item} parsed successfully ({ct})");
    }
    [DataTestMethod("Fail parsing of invalid strings")]
    [DynamicData(nameof(CueTimeTestData.FailTexts), typeof(CueTimeTestData))]
    public void ParseTestStringFail(string item, Type expected)
    {
        Assert.AreEqual(expected, ThrownType(() => CueTime.Parse(item)), item);
    }
    [DataTestMethod("Fail parsing of invalid strings as ReadOnlySpan<char>")]
    [DynamicData(nameof(CueTimeTestData.FailTexts), typeof(CueTimeTestData))]
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

    [TestMethod]
    public void ParseKeepsSignOfZeroMinutes()
    {
        // bug: CueTime.Parse takes the sign from the minutes number, and -00 is 0
        Assert.AreEqual(new CueTime(-750), CueTime.Parse("-00:10:00"));
    }

    [DataTestMethod]
    [DataRow("10:-10:10", DisplayName = "mixed signs")]
    [DataRow("1:2:3:junk", DisplayName = "extra part")]
    [DataRow("1:junk:3", DisplayName = "corrupted part")]
    public void ParseRejectsMalformed(string input)
    {
        // bug: CueTime.Parse ignores signs past the first part and stops after three parts
        Assert.IsFalse(CueTime.TryParse(input, out _));
        bool threw = false;
        try
        {
            CueTime.Parse(input);
        }
        catch (Exception)
        {
            threw = true;
        }
        Assert.IsTrue(threw, "Parse accepted malformed input");
    }

    [DataTestMethod]
    [DataRow("10")]
    [DataRow("10:10")]
    [DataRow("10:10:10")]
    [DataRow("01:01:01")]
    [DataRow("00:00:01")]
    [DataRow("00:-00:01")]
    [DataRow("001:001:001")]
    [DataRow("-10")]
    [DataRow("-10:10")]
    [DataRow("-10:10:10")]
    [DataRow("-00:00:01")]
    [DataRow("9999")]
    [DataRow("-9999")]
    public void ParseAndTryParseAgree(string input)
    {
        // bug: Parse requires 3 parts, TryParse accepts 1 or 2
        bool tryResult = CueTime.TryParse(input, out var tried);
        CueTime? parsed = null;
        try
        {
            parsed = CueTime.Parse(input);
        }
        catch (Exception)
        {
        }
        Assert.AreEqual(tryResult ? tried : null, parsed);
    }
}
