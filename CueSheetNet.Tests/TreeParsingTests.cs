using CueSheetNet.NameParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;
[TestClass]
public class TreeParsingTests
{
    [TestMethod("Test if all parse token are valid")]
    public void TestAllAvailable()
    {
        var t = CueTreeFormatter.AvailableProperties;
    }
}
