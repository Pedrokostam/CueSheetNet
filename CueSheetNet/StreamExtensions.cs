using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;
internal static class StreamExtensions
{
#if !NETCOREAPP2_1_OR_GREATER || !NETSTANDARD2_1_OR_GREATER // Stream.Read(Span) was introduced in NET Core 2.1 and NETStandard2.1
    public static int Read(this Stream stream, Span<byte> span)
    {
        int i;
        for (i = 0; i < span.Length; i++)
        {
            int readByte =stream.ReadByte();
            if(readByte >= 0)
            {
                span[i]=(byte)readByte;
            }
            else
            {
                break;
            }
        }
        return i;
    }
#endif
}
