namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeFormattingTests
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
        Assert.AreEqual("01\\",new CueTime(1, 0, 0).ToString(@"mm\"));
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
