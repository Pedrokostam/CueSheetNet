namespace CueSheetNet.Tests;

[TestClass]
public class CueTimeArithmeticTests
{
    [DataTestMethod]
    [DynamicData(nameof(CueTimeTestData.TestObjectComparisons), typeof(CueTimeTestData))]
    public void ComparisonTest(CueTime time,(TimeSpan ts, int res)[] timespans)
    {
        foreach (var (ts, res) in timespans)
        {
            Assert.AreEqual(res, time.CompareTo(ts),$"{time} - {ts.Ticks}");
        }
    }
    [DataTestMethod]
    [DynamicData(nameof(CueTimeTestData.TestObjectComparisonsThrow), typeof(CueTimeTestData))]
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
    [DynamicData(nameof(CueTimeTestData.OverflowingOperations), typeof(CueTimeTestData))]
    public void CheckedOverflowDetectedTest2(string name, Func<CueTime> operation)
    {
        Assert.ThrowsException<OverflowException>(() => operation(), name);
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
}
