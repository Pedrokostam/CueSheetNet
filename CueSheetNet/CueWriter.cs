using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet
{
    internal class CueWriter
    {
        public Encoding Encoding { get; set; } =new UTF8Encoding(false);
        public int IndentationDepth { get; set; } = 2;
        /// <summary>
        /// If true, Byte order mark will not be included in the text file, even if encoding specifies it.
        /// </summary>
        public bool SkipBOM { get; set; } = false;

        public CueWriter()
        {
        }
        public void SaveCueSheet(CueSheet sheet)
        {
            //sheet.FileInfo
        }
    }
}
