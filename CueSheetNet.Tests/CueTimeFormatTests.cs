namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeFormatTests
{
    [DataTestMethod]
    [DataRow(0, 0, 0, "00:00:00")]
    [DataRow(1, 2, 3, "01:02:03")]
    [DataRow(-1, -2, -3, "-01:02:03")]
    [DataRow(100, 0, 0, "100:00:00")]
    public void DefaultToString(int m, int s, int f, string expected)
    {
        Assert.AreEqual(expected, new CueTime(m, s, f).ToString());
    }

    [TestMethod]
    public void ExtremesFormatWithoutThrowing()
    {
        Assert.IsFalse(string.IsNullOrEmpty(CueTime.MaxValue.ToString()));
        Assert.IsTrue(CueTime.MinValue.ToString().StartsWith('-'));
    }

    [TestMethod]
    public void CustomFormatPadsOnTheLeft()
    {
        Assert.AreEqual("00:05:03", new CueTime(0, 5, 3).ToString("mm:ss:ff"));
    }

    [TestMethod]
    public void EscapedCharacterIsLiteral()
    {
        // bug: CueTime.AppendEscaped skips the backslash and formats the next character
        Assert.AreEqual("m", new CueTime(7, 0, 0).ToString(@"\m"));
    }

    [TestMethod]
    public void TrailingBackslashDoesNotThrowIndexOutOfRange()
    {
        // bug: CueTime.AppendEscaped reads past the end of the format
        try
        {
            new CueTime(1, 0, 0).ToString(@"mm\");
        }
        catch (FormatException)
        {
        }
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

    [TestMethod]
    public void ModuloByZeroThrows()
    {
        Assert.ThrowsException<DivideByZeroException>(() => new CueTime(1, 0, 0) % CueTime.Zero);
    }

    [TestMethod]
    public void DivideByFractionOverflowThrows()
    {
        Assert.ThrowsException<OverflowException>(() => CueTime.MaxValue / 0.5);
    }

    [TestMethod]
    public void TryFormatToSmallDestinationReturnsFalse()
    {
        var time = new CueTime(1, 2, 3);
        Assert.IsFalse(time.TryFormat(new char[3], out int chars, default, null));
        Assert.AreEqual(0, chars);
        Assert.IsFalse(time.TryFormat(new byte[3], out int bytes, default, null));
        Assert.AreEqual(0, bytes);
    }
}
