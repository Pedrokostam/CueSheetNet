using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;
internal static class StreamExtensions
{
#if !NETCOREAPP2_1_OR_GREATER
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
