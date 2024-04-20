namespace CueSheetNet;

/// <summary>
/// File types mentioned in the CUE sheet specification.
/// </summary>
[Flags]
public enum FileType
{
    /// <summary>
    /// Not detected yet, or out-of-specification.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Uncompressed audio data in a RIFF container (common on Windows). Also a catch-all for other audio formats not mentioned in specification, like FLAC.
    /// </summary>
    WAVE = 1 << 0,

    /// <summary>
    /// Uncompressed audio data in an IFF container (common on Apple).
    /// </summary>
    AIFF = 1 << 1,

    /// <summary>
    /// Compressed lossy audio data.
    /// </summary>
    MP3 = 1 << 2,

    /// <summary>
    /// Little-Endian binary.
    /// </summary>
    BINARY = 1 << 3,

    /// <summary>
    /// Big-Endian binary.
    /// </summary>
    MOTOROLA = 1 << 4,
}
