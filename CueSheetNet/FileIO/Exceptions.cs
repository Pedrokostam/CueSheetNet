namespace CueSheetNET.FileIO
{
    class UnsupportedFileFormatException : Exception
    {
        public UnsupportedFileFormatException(string msg) : base(msg)
        { }
    }
    class FileFormatRecognitionException : Exception
    {
        public FileFormatRecognitionException(string msg) : base(msg)
        { }
    }
    class InvalidFileFormatException : Exception
    {
        public InvalidFileFormatException(string msg) : base(msg)
        { }
    }
}
