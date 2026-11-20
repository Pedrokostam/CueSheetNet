namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeConversionTests
{
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
    [DynamicData(nameof(CueTimeTestData.TestTickToFrameConversion), typeof(CueTimeTestData))]
    public void TestConversion(long ticks, int targetFrames)
    {
        Assert.AreEqual(targetFrames, CueTime.TicksToFrames(ticks), 0);
        Assert.AreEqual(-targetFrames, CueTime.TicksToFrames(-ticks), 0);
    }
}
