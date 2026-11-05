namespace CueSheetNet;

public interface ISourcelessCueReader
{
    CueReaderSettings Settings { get; }
    /// <summary>
    /// Sets the settings to be used to parse the sheet.
    /// </summary>
    /// <remarks>
    /// To learn more check the more fine-grained methods:
    /// <list type="table">
    /// <item><see cref="WithQuotation(char)"/></item>
    /// <item><see cref="WithEncoding(Encoding?)"/></item>
    /// <item><see cref="WithQualifiedPaths(bool)"/></item>
    /// </list>
    /// </remarks>
    /// <param name="encoding">The settings to set.</param>
    /// <returns>The same instance used to call it, with changed settings.</returns>
    public ISourcelessCueReader WithSettings(CueReaderSettings settings);
    /// <summary>
    /// Sets the quotation char to be used when parsing the sheet.
    /// </summary>
    /// <remarks>
    /// The CDRWin standard only allows the double quotes, but if your sheet for some reason uses a different character this may prevent parsing errors.
    /// </remarks>
    /// <param name="quote">Single character used instead double quotes.</param>
    /// <returns>The same instance used to call it, with changed quotation.</returns>
    public ISourcelessCueReader WithQuotation(char quote);
    /// <summary>
    /// Sets the encoding to be used to decode most source types, except <see cref="CueReader2.FromString(string)">string</see>.
    /// </summary>
    /// <remarks>
    /// If set to null, the reader will attempt to detect the file encoding.
    /// <br/>
    /// If set to an actual encoding, no detection will be done.
    /// <br/>
    /// If the reader encounters invalid bytes, the behavior will depend on the encoding's parameters.
    /// </remarks>
    /// <param name="encoding">The encoding, or lack thereof, to set.</param>
    /// <returns>The same instance used to call it, with changed encoding.</returns>
    public ISourcelessCueReader WithEncoding(Encoding? encoding);
    /// <summary>
    /// Changes how file paths are handled when parsing and editing the sheet.
    /// </summary>
    /// <remarks>
    /// By default all absolute (qualified) file paths (FILE, CDTEXT, associated files) will be converted to be relative to the sheet's directory, if possible.
    /// <br>
    /// Setting this to <see langword="true"/> will do the opposite - all paths will be converted to qualified paths, if possible.
    /// </br>
    /// </remarks>
    /// <param name="qualified">Whether the paths should be converted to qualified paths whenever possible.</param>
    /// <returns>The same instance used to call it, with the given qualified paths behavior.</returns>
    public ISourcelessCueReader WithQualifiedPaths(bool qualified);
    /// <summary>
    /// Resets the source, and any data originating from the parsing process.
    /// </summary>
    /// <returns>The same instance used to call it, reset.</returns>
    public ISourcelessCueReader Reset();

    /// <summary>
    /// Sets the source of the cuesheet to the string content.
    /// </summary>
    /// <remarks>
    /// If the source has already been set, it is changed to the one and the reader is reset.
    /// </remarks>
    /// <param name="content">String to be parsed directly</param>
    /// <returns>An instance of CueReader with the source set.</returns>
    CueReader2 FromString(string content);

    /// <summary>
    /// Sets the source of the cuesheet to the existing file.
    /// </summary>
    /// <remarks>
    /// If the source has already been set, it is changed to the one and the reader is reset.
    /// </remarks>
    /// <param name="filepath">Path of the file to be read and parsed.</param>
    /// <returns>An instance of CueReader with the source set.</returns>
    CueReader2 FromFile(string filepath);

    /// <summary>
    /// Sets the source of the cuesheet to the existing file.
    /// </summary>
    /// <remarks>
    /// If the source has already been set, it is changed to the one and the reader is reset.
    /// </remarks>
    /// <param name="fileInfo">A FileInfo instance pointing to the file to be parsed.</param>
    /// <returns>An instance of CueReader with the source set.</returns>
    CueReader2 FromFile(FileInfo fileInfo);

    /// <summary>
    /// Sets the source of the cuesheet to the string content. Will attempt detect encoding if not specified.
    /// </summary>
    /// <remarks>
    /// If the source has already been set, it is changed to the one and the reader is reset.
    /// </remarks>
    /// <param name="bytes">Byte source to be decoded to text and parsed</param>
    /// <returns>An instance of CueReader with the source set.</returns>
    CueReader2 FromBytes(IList<byte> bytes);
    /// <summary>
    /// Sets the source of the cuesheet to the stream. The stream will be read fully before parsing.
    /// </summary>
    /// <remarks>
    /// If the source has already been set, it is changed to the one and the reader is reset.
    /// </remarks>
    /// <param name="bytes">Byte source to be decoded to text and parsed</param>
    /// <returns>An instance of CueReader with the source set.</returns>
    CueReader2 FromStream(Stream stream);
}
