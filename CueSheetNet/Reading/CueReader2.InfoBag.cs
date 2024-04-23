﻿namespace CueSheetNet;

public partial class CueReader2
{
    private class InfoBag()
    {
        public string? Performer { get; set; }
        public string? Title { get; set; }
        public string? CdTextFile { get; set; }
        public string? Catalog { get; set; }
        public string? OriginalContent { get; set; }
        public Line CurrentLine { get; set; } = new Line(-1, "");
        public Chain<File> Files=[];
        public List<CueRemark> Remarks { get; } = [];

    }

}