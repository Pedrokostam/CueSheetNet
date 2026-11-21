using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.TextParser;

public static class Encodings
{
    public static Encoding Utf32BE { get; } = Encoding.GetEncoding("utf-32BE",
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);
    public static Encoding Utf32 { get; } = Encoding.UTF32;
    public static Encoding Utf8Bom { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);
    public static Encoding Utf8NoBom { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    public static Encoding LastChance { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: false);
}
