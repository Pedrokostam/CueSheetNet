using System.Text;
using System.Text.RegularExpressions;

namespace CueSheetNet.Tests;
[TestClass]
public partial class EncodingDetectionTests
{
    [TestInitialize]
    public void Init()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    [GeneratedRegex(@"UTF-(?<BIT>\d+)")]
    public static partial Regex UtfBitFinder();

    private static bool IsBigEndian(string input) => input.Contains(" BE");
    private static bool HasBom(string input) => input.Contains(" BOM");
    private static Encoding ParseEncodingFromName(string name)
    {
        name = Path.GetFileNameWithoutExtension(name);
        var utf = UtfBitFinder().Match(name);
        if (utf.Success)
        {
            string bitString = utf.Groups["BIT"].Value;
            Encoding en = bitString switch
            {
                "8" => new UTF8Encoding(HasBom(name), false),
                "16" => new UnicodeEncoding(IsBigEndian(name), HasBom(name), false),
                "32" => new UTF32Encoding(IsBigEndian(name), HasBom(name), false),
                _ => throw new NotImplementedException(bitString),
            };
            return en;

        }
        else
        {
            return Encoding.GetEncoding(int.Parse(name));
        }
    }
    [TestMethod]
    [DynamicData(nameof(Samples.EncodingRows), typeof(Samples))]
    public void TestEncodingDetection(Sample sample)
    {
        string filepath = sample.Path;
        Encoding target = ParseEncodingFromName(filepath);
        var cue = CueReader.CreateFromFile(filepath).Parse();
        Encoding cueEnc = cue.SourceEncoding!;
        bool match = target.GetPreamble().SequenceEqual(cueEnc.GetPreamble())
            && target.EncodingName == cueEnc.EncodingName;
        var a = cueEnc.GetPreamble();
        Assert.IsTrue(match, $"Target: {target.EncodingName}, actual: {cueEnc.EncodingName}, actual preamble: {string.Join(", ", a)}-{Path.GetFileNameWithoutExtension(filepath)}");
    }
}
