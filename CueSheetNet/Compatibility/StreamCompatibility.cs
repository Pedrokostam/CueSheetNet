#if !(NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER) // Stream.Read(Span) was introduced in NET Core 2.1 and NETStandard2.1
namespace CueSheetNet;
internal static class StreamCompatibility
{
    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
    /// <para>Introduced in NET Core 2.1 and NETStandard 2.1 as instance method.</para>
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="span">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current source.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the size of the buffer if that many bytes are not currently available, or zero (0) if the buffer's length is zero or the end of the stream has been reached.</returns>
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
}
#endif
