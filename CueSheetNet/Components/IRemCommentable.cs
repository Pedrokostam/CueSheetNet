using CueSheetNet.Collections;

namespace CueSheetNet
{
    /// <summary>
    /// Provides methods to modify comments and remarks of the parent item.
    /// </summary>
    internal interface IRemCommentable
    {
        public RemarkCollection Remarks { get; }
        public CommentCollection Comments { get; }
    }
}