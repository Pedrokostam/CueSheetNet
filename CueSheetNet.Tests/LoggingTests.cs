using CueSheetNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;
[TestClass]
public class LoggingTests
{
    [TestMethod]
    public void LogsWork()
    {
        ArrayLogger arlog = new(LogLevel.All);
        Logger.Register(arlog);
        Logger.LogDebug("DEBUG");
        Logger.LogWarning("WARN");
        Logger.LogInformation("INFO");
        Assert.IsTrue(arlog.LogEntries[0].Message.Contains("Registered"));
        Assert.AreEqual(arlog.LogEntries[1].Message, "DEBUG");
        Assert.AreEqual(arlog.LogEntries[2].Message, "WARN");
        Assert.AreEqual(arlog.LogEntries[3].Message, "INFO");
    }
}
